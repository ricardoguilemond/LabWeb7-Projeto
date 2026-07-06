using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Controllers;
using LabWebMvc.MVC.Areas.Impressoras;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Validations;
using LabWebMvc.MVC.Integracoes.Exportacao;
using LabWebMvc.MVC.Integracoes.Importacao;
using LabWebMvc.MVC.Interfaces;
using LabWebMvc.MVC.Interfaces.Criptografias;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text.Json;

namespace LabWebMvc.MVC
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Registro do serviço que conecta e aciona o Db com a base correta de dados.
            //Lembrando que o Db não será injetado diretamente, mas sim via Repositórios genéricos.
            services.AddScoped<IConnectionService, ConnectionService>();
            services.AddScoped<IDbFactory, DbFactory>(); //Fábrica para criar instâncias do DbContext para troca de banco de dados dinamicamente.

            services.AddScoped<Db>(sp =>
            {
                var connectionService = sp.GetRequiredService<IConnectionService>();
                var eventLogHelper = sp.GetRequiredService<IEventLogHelper>();

                var optionsBuilder = new DbContextOptionsBuilder<Db>()
                    .UseNpgsql(connectionService.GetConnectionString());

                return new Db(optionsBuilder.Options, connectionService, eventLogHelper);
            });

            services.AddScoped<ExportacaoFactory>();
            services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
            //..

            //Definindo o local correto de construção das Views neste projeto...
            services.Configure<RazorViewEngineOptions>(options =>
            {
                //options.ViewLocationFormats.Clear();
                //options.ViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
                //options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
            });
            //..

            //Serviços para injeção de dependência para o BLL
            // Fonte de data/hora: PostgreSQL (SELECT NOW() → UTC/timestamptz)
            // Fallback automático para DateTime.UtcNow se banco inacessível
            services.AddScoped<ITempoServidorService, TempoServidorPostgreSQL>();
            services.AddScoped<GeralController>();  //para injeção de dependência do serviço de métodos gerais de controller
            services.AddScoped<IValidacoesDeSenhas, ValidacoesDeSenhas>();  //para injeção de dependência do serviço de validações de senhas
            services.AddSingleton<IEventLogHelper, EventLogHelper>();   //para injeção de uma única instância do Log Helper por toda a aplicação.
            services.AddScoped<ExclusaoService>(); //para injeção de dependência do serviço de exclusões com concorrência
            //..

            //Para injetar serviço de impressoras
            if (OperatingSystem.IsWindows())
            {
                services.AddScoped<IImpressoraCupom, ImpressoraWindows>();
            }
            else if (OperatingSystem.IsLinux())
            {
                services.AddScoped<IImpressoraCupom, ImpressoraLinux>();
            }
            //para injetar o serviço de concorrências de exclusões
            services.AddScoped<IConcorrenciaService, ConcorrenciaService>();
            //..
            //para injetar o serviço de imagens
            services.AddScoped<Imagem>();
            //..
            //injeção de dependência para PathHelper
            services.AddScoped<IPathHelper, PathHelper>();
            //..
            //DI para ReCaptcha
            services.AddScoped<ReCaptchaService>();
            //.
            //para registrar a injeção de dependência e tornar o "HttpContext" disponível entre os controladores...
            //com tempo de vida útil igual ao tempo da aplicação!
            services.AddScoped<IValidadorDeSessao, ValidadorDeSessao>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //Para o modelo de importação de movimentações (carga de dados) 
            services.AddScoped<MovimentacaoImportacao>(sp =>
            {
                var connectionService = sp.GetRequiredService<IConnectionService>();
                var config = sp.GetRequiredService<IConfiguration>();
                var eventLogHelper = sp.GetRequiredService<IEventLogHelper>();

                var optionsBuilder = new DbContextOptionsBuilder<Db>()
                    .UseNpgsql(connectionService.GetConnectionString());

                var db = new Db(optionsBuilder.Options, connectionService, eventLogHelper);

                return new MovimentacaoImportacao(db, config);
            });
            //Para o modelo de exportação de pacientes
            services.AddScoped<ServicoExportacaoPacientes>();
            //
            //Feito pelo Kiro em 11/07/2025
            //Cache em memória para referências de exames (migração Delphi)
            services.AddMemoryCache();
            services.AddScoped<IExameReferenciaCache, ExameReferenciaCache>();
            //..Kiro
            //
            //services.AddControllersWithViews();
            //..
            //Evita que um campo string de View Model (formulário) dê erro de validação quando for nulo!
            services.AddControllersWithViews(options =>
            {
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options =>
            {
                // Serialização JSON: DateTime UTC em formato ISO 8601 com Z
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
            //..

            //Criação de Cookies e Sessions
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                //Após o tempo estipulado aqui, o usuário será obrigado a efetuar Login de novo.
                options.Cookie.Name = ".LabWeb7.Session";
                options.Cookie.HttpOnly = true;                     //essencial: definimos o cookie para ser acessado por scripts do lado do cliente.
                options.Cookie.IsEssential = true;                  //importante para funcionar mesmo com LGPD em produção.
                options.Cookie.SameSite = SameSiteMode.Lax;         // ou Strict
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.IdleTimeout = TimeSpan.FromMinutes(30);     //por exemplo: tempo em MINUTOS (7 minutos = 420s) que a atividade permanecerá após inatividade do usuário (isso afeta o HttpContext, nas sessions)
            });
            //..

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.Cookie.Name = ".LabWeb7.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/Login";
                options.LogoutPath = "/Logout";
                options.AccessDeniedPath = "/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            //..
            //services.AddMvc();

            //Para injeção de dependência do serviço do Google ReCaptcha
            services.Configure<GoogleReCaptchaSettings>(Configuration.GetSection("GoogleReCaptcha"));  //pega o array de configurações do Google ReCaptcha do appsettings.json
            services.AddScoped<CreateAssessmentSample>();
            services.AddScoped<IValidacaoGoogleReCaptcha, ValidacaoGoogleReCaptcha>();
            //..
            //Renderizando Razor Pages
            services.AddRazorPages();
            //..
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();    /* este instalou como padrão */
                app.UseStatusCodePages();           /* coloquei */
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios,
                // see https://aka.ms/aspnetcore-hsts.
                // app.UseHsts();
            }

            //Usado em ServicosDatabase/HttpContextHelper e ValidacoesDeSenhas
            HttpContextHelper.Configure(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
            Areas.Utils.Utils.SetHttpContextAccessor(app.ApplicationServices.GetRequiredService<IHttpContextAccessor>());
            //..
            app.UseHttpsRedirection();
            app.UseStaticFiles();      /* coloquei */
            app.UseCookiePolicy();     /* coloquei */
            app.UseRouting();          /* obrigatório vir antes de UseSession */
            /* Para dar suporte as datas brasileiras como padrão em toda a aplicação
             * Parece que tem que ficar aqui entre o app.UseRouting() e app.UseAuthentication()
             */
            CultureInfo[] supportedCultures = new[] { new CultureInfo(name: "pt-BR") };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture: "pt-BR", uiCulture: "pt-BR"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });
            //..
            //combinado com services.AddSession (lá em cima em services) acionando a session. Deve vir depois de "UseRouting()" e antes de "UseAuthentication()"
            app.UseSession();          /* obrigatório vir antes dos endpoints, e antes de UseAuthentication e UseAuthorization */

            if (env.IsDevelopment())
            {
                //AUXILIAR DEBUG: usamos um middleware personalizado para capturar exceções e escrever diretamente na resposta HTTP, principalmente quando não for possível mensagem tratada em tela.
                //app.UseMiddleware<SessionDebugMiddleware>();             //Exibe dados sensíveis da sessão no log de depuração, somente em ambiente de desenvolvimento.
                //app.UseMiddleware<SessionCookieDiagnosticMiddleware>();  //Exibe dados sensíveis da sessão no log de depuração, somente em ambiente de desenvolvimento.
                //app.UseMiddleware<ResponseCookieLoggerMiddleware>();     //Exibe dados sensíveis da sessão no log de depuração, somente em ambiente de desenvolvimento.
            }

            app.UseAuthentication();
            app.UseAuthorization();
            //...

#if DEBUG
            // Em desenvolvimento: reseta para BCrypt qualquer senha que ainda não foi migrada
            DevSenhasReset(app);
#endif
            // Antes da autenticação: configure o helper
            //var eventLog2 = app.ApplicationServices.GetRequiredService<IEventLogHelper>();
            //eventLog2.ObterCNPJ = () => Areas.Utils.Utils.LoginCNPJEmpresaLogado();
            //eventLog2.ObterNomeEmpresa = () => Areas.Utils.Utils.LoginNomeEmpresaLogado();
            //..
            //Não uso o endpoints aqui, porque está tudo mapeado dentro da aplicação, pois usamos roteamento MVC.
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Login}/{id?}");
                endpoints.MapRazorPages(); //para termos rotas de uso direto Razor Pages, como a de Login.cshtml

                //endpoints.MapDefaultControllerRoute();   //não vamos usar mapeamento default, porque queremos controlar algumas rotas diferentes.

                //endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                ///
                //endpoints.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                //endpoints.MapControllerRoute("Home", "{controller=Home}/{action=Login}/{id?}");
                //endpoints.MapControllerRoute("Home", "{controller=Home}/{action=NossoSistema}/{id?}");
                //endpoints.MapControllerRoute("Home", "{controller=Home}/{action=Privacy}/{id?}");
                //endpoints.MapControllerRoute("Release", "{controller=Release}/{action=Release}/{id?}");
                //endpoints.MapControllerRoute("Senhas", "{controller=Senhas}/{action=Index}/{id?}");
                //
                //endpoints.MapControllerRoute("Pacientes", "{controller=Pacientes}/{action=Index}/{id?}");
                //endpoints.MapControllerRoute("Medicos", "{controller=Medicos}/{action=Index}/{id?}");
                //endpoints.MapControllerRoute("Postos", "{controller=Postos}/{action=Index}/{id?}");
                //
                //endpoints.MapControllerRoute("AcessoNegado", "{controller=Mensagem}/{action=AcessoNegado}/{id?}");
                //endpoints.MapControllerRoute("MensagemTela", "{controller=Mensagem}/{action=MensagemTela}/{id?}");
            });
        }

#if DEBUG
        /// <summary>
        /// Apenas em DEBUG: reseta todas as senhas não-BCrypt para a senha padrão configurada em LoginPadraoSistema:Senha.
        /// Itera por TODOS os bancos de clientes lidos de LABWEB7Empresas (StringConexao).
        /// Garante que o desenvolvedor consiga logar após migrações de segurança.
        /// </summary>
        private static void DevSenhasReset(IApplicationBuilder app)
        {
            try
            {
                using var scope = app.ApplicationServices.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var eventLog = scope.ServiceProvider.GetRequiredService<IEventLogHelper>();
                var connectionService = scope.ServiceProvider.GetRequiredService<IConnectionService>();

                string senhaPadrao = config["LoginPadraoSistema:Senha"] ?? "12345";
                string novoHash = CriptoDecripto.HashSenha(senhaPadrao);
                int totalMigradas = 0;

                // 1. Lê todos os StringConexao distintos da base LABWEB7Empresas
                string? connEmpresas = config["ConexaoPostgreSQL:PSQLConnectionStringEmpresas"];
                if (string.IsNullOrEmpty(connEmpresas))
                {
                    Console.WriteLine("[DEV] DevSenhasReset ignorado: PSQLConnectionStringEmpresas não configurada.");
                    return;
                }

                var stringsConexao = new List<string>();
                using (var conexao = new Npgsql.NpgsqlConnection(connEmpresas))
                {
                    conexao.Open();
                    using var cmd = new Npgsql.NpgsqlCommand(@"SELECT DISTINCT ""StringConexao"" FROM ""EmpresaCliente"" WHERE ""StringConexao"" IS NOT NULL AND ""StringConexao"" <> ''", conexao);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        stringsConexao.Add(reader.GetString(0));
                }

                if (!stringsConexao.Any())
                {
                    Console.WriteLine("[DEV] DevSenhasReset: nenhum banco de cliente encontrado em EmpresaCliente.");
                    return;
                }

                // 2. Para cada banco de cliente, migra senhas legadas
                foreach (var connStr in stringsConexao)
                {
                    try
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<Db>().UseNpgsql(connStr);
                        using var db = new Db(optionsBuilder.Options, connectionService, eventLog);

                        var senhasLegadas = db.Senhas
                            .Where(s => !s.SenhaUsuario.StartsWith("$2"))
                            .ToList();

                        if (senhasLegadas.Any())
                        {
                            foreach (var s in senhasLegadas)
                                s.SenhaUsuario = novoHash;

                            db.SaveChanges();
                            totalMigradas += senhasLegadas.Count;
                            Console.WriteLine($"[DEV] DevSenhasReset: {senhasLegadas.Count} senha(s) migradas em '{connStr.Split(';')[2]}'");
                        }
                    }
                    catch (Exception exDb)
                    {
                        Console.WriteLine($"[DEV] DevSenhasReset: banco ignorado ({exDb.Message})");
                    }
                }

                if (totalMigradas > 0)
                    eventLog.LogEventViewer(
                        $"[DEV] DevSenhasReset: {totalMigradas} senha(s) migradas para BCrypt com senha padrão '{senhaPadrao}' em {stringsConexao.Count} banco(s)",
                        "wInfo");
                else
                    Console.WriteLine("[DEV] DevSenhasReset: todas as senhas já estão em BCrypt.");
            }
            catch (Exception ex)
            {
                // Não bloqueia o startup se o banco não estiver disponível
                Console.WriteLine($"[DEV] DevSenhasReset ignorado: {ex.Message}");
            }
        }
#endif
    }
}