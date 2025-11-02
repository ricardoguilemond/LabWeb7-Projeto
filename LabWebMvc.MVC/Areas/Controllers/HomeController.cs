using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Areas.Validations;
using LabWebMvc.MVC.Interfaces;
using LabWebMvc.MVC.Interfaces.Criptografias;   //decalaração para modificação de parâmetros do reCaptcha
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Claims;
using static BLL.UtilBLL;
using static ExtensionsMethods.Genericos.Enumeradores;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class HomeController : BaseController
    {
        private readonly GoogleReCaptchaSettings _captchaSettings;
        private readonly IValidacaoGoogleReCaptcha _captchaValidator;
        private readonly CreateAssessmentSample _captchaService;
        private readonly IValidacoesDeSenhas _validacoesDeSenhas;
        private readonly IConnectionService _connectionService;
        private readonly ReCaptchaService _reCaptchaService;

        public HomeController(IOptions<GoogleReCaptchaSettings> captchaSettings,
                              IValidacaoGoogleReCaptcha captchaValidator,
                              CreateAssessmentSample captchaService,
                              IValidacoesDeSenhas validacoesDeSenhas,
                              IDbFactory dbFactory,
                              IValidadorDeSessao validador,
                              GeralController geralController,
                              IEventLogHelper eventLogHelper,
                              Imagem imagem,
                              IConnectionService connectionService,
                              ReCaptchaService reCaptchaService,
                              ExclusaoService exclusaoService) 
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService)
        {
            _captchaSettings = captchaSettings.Value;
            _captchaValidator = captchaValidator;
            _captchaService = captchaService;
            _validacoesDeSenhas = validacoesDeSenhas;
            _connectionService = connectionService;
            _reCaptchaService = reCaptchaService;
        }

        [HttpGet]
        [Route("Home")]
        public async Task<IActionResult> Index(string[] mensagem)
        {
            //após o Login, pega o nome que foi feito Login e os dados registrados na Session
            string? loginNome = HttpContext.Session.GetString("SessionNome");
            string? loginToken = HttpContext.Session.GetString("SessionToken");
            string? loginEmail = HttpContext.Session.GetString("SessionEmail");

            mensagem[0] = "Seja bem-vindo: " + loginNome;

            //Responses static que ficarão disponíveis por toda a aplicação:
            TextoMenuPrincipalResponse.TextoMenuPrincipal = new object[] { mensagem[0], false };  //ficará disponível para toda a aplicação via BLL
            try
            {
                MensagemHtmlResponse.MensagemHtml = new object[] { await @"Views\Mensagem\MensagemTela.cshtml".RetornaTextoDeArquivoHtmlAsync() };
            }
            catch (Exception e)
            {
                string error = e.Message;
                _eventLogHelper.LogEventViewer("[Home] Erro ao tentar carregar o arquivo HTML: " + mensagem + ", exception: " + error, "wError");
                MensagemHtmlResponse.MensagemHtml = new object[] { "Nenhum arquivo HTML foi encontrado para acionar mensagens.", error };
            }
            finally { }
            //Fim dos Responses static

            if (!string.IsNullOrEmpty(loginNome) && !string.IsNullOrEmpty(loginToken) && !string.IsNullOrEmpty(loginEmail))
            {
                //Conseguiu fazer um Login válido, então passa por aqui
                ViewBag.TextoMenu = new object[] { mensagem[0], bool.Parse(mensagem[1].ToLower()) };
            }
            else
            {
                mensagem[0] = "Por favor, faça seu Login para ter acesso!";
                ViewBag.TextoMenu = new object[] { mensagem[0], false };
                return RedirectToAction("AcessoNegado", "Mensagem", new { mensagem = MensagensError_pt_BR.ErroLogin });
            }
            return View();
        }

        /*
         * PRIMEIRO MÉTODO DE LOGIN
         */

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(vmLogin vm)
        {
            var conexaoGenerica = _connectionService.GetConnectionString();

            //Esse primeiro método é acionado quando carrega a tela do login
            string baseUrl = _captchaSettings.SiteKey;
            string secretKey = _captchaSettings.SecretKey;
            ViewBag.KeySecretPublic = secretKey;   //passa a mesma chave para o HTML
            ViewBag.UrlGoogleAPI = $"{baseUrl}{secretKey}";

            ViewBag.TextoMenu = "Tentativa de acesso".MensagemStartUp();
            ViewBag.TitleLogin = "Login";
            ViewBag.Entrar = "Entrar";
            ViewBag.EsqueceuSuaSenha = "Esqueceu sua senha?";
            ViewBag.OlhoEmail = Utils.Utils.ImagemOlho;

            // Recupera a mensagem de erro de Login, se houver...
            if (TempData["MensagemErroLogin"] != null)
            {
                ViewBag.MensagemErroLogin = TempData["MensagemErroLogin"];
            }
            return View();
        }

        /*
         * SEGUNDO MÉTODO DE LOGIN (Post ou pós login), somente aqui o "Token do Google" (segurança de página) aparece disponível.
         *
         * Só iremos validar o Login da conta do usuário neste Sistema, se o Google autorizou via ReCaptcha/Tokens.
         * */

        [HttpPost]
        [AllowAnonymous]
        [Route("Login")]
        public async Task<IActionResult> Login(vmLogin? vm, bool? modo = true)   /* o parâmetro "modo" aqui nem é usado, é apenas para tornar o método sobrescrito !! */
        {
            if (vm == null)
            {
                _eventLogHelper.LogEventViewer("[Home] Tentativa de Login sem parâmetros", "wError");
                return RedirectToAction("Error", "Mensagem", new { mensagem = "Tentativa de Login sem parâmetros e reCaptcha não foi acionado!" });
            }
            bool validaReCaptcha = _captchaValidator.IsCaptchaValid(vm!);  //retorna se o Google ReCaptcha está sendo validado ou não pela página.

            if (!validaReCaptcha)
            {
                _eventLogHelper.LogEventViewer("[Home] Não consegui validar o login porque retornou erro reCaptcha da página - tente de novo", "wError");
                return RedirectToAction("Error", "Mensagem", new { mensagem = @"[Home] Não consegui validar o login porque retornou erro reCaptcha da página - tente de novo" });
            }

            //Avaliação do Google ReCaptcha antes de continuar o Login
            return await AvaliarReCaptchaLimiteAntesDoLogin(vm);
        }

        [NonAction]
        public async Task<IActionResult> AvaliarReCaptchaLimiteAntesDoLogin(vmLogin vm)
        {
            if (vm != null && !string.IsNullOrEmpty(vm.GoogleCaptchaToken))
            {
                ICollection<string> validacaoReCaptcha = await _captchaService.CreateAssessment(vm.GoogleCaptchaToken, _captchaSettings.ProjectID, "login");

                bool sucesso = validacaoReCaptcha.Any(m => m.Contains("segura para este acesso"));
                bool precisaConfirmacao = validacaoReCaptcha.Any(m => m.Contains("limite gratuito de 10.000 requisições"));

                if (!sucesso && precisaConfirmacao)
                {
                    TempData["vmLogin"] = JsonConvert.SerializeObject(vm);

                    return RedirectToAction("MensagemConfirma", "Mensagem", new
                    {
                        tituloModal = "Confirmação ReCaptcha",
                        mensagemModal = "Você atingiu o limite gratuito de 10.000 requisições ReCaptcha.",
                        perguntaModal = "Deseja continuar? Isso pode gerar custos.",
                        returnAction = "ExecutaRotinaFinal",
                        returnController = "Home",
                        textoRodape = "Atenção: Se desejar prosseguir terá que pagar $1 dollar, a partir de agora, por cada mil acessos!"
                    });
                }
            }

            TempData["vmLogin"] = JsonConvert.SerializeObject(vm);
            return RedirectToAction("ExecutaRotinaFinal", "Home");
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExecutaRotinaFinal()
        {
            if (!TempData.TryGetValue("vmLogin", out object? tempLogin) || string.IsNullOrEmpty(tempLogin?.ToString()))
                return RedirectToAction("Error", "Mensagem", new { mensagem = "Sessão de login expirada ou inválida." });

            vmLogin? vm = JsonConvert.DeserializeObject<vmLogin>(tempLogin.ToString()!);

            ICollection<string> validacaoReCaptcha = await _captchaService.CreateAssessment(vm?.GoogleCaptchaToken ?? "");

            if (validacaoReCaptcha == null || validacaoReCaptcha.Count == 0)
            {
                _eventLogHelper.LogEventViewer("[Home] Não foi possível validar o ReCaptcha do Google, retornou vazio ou nulo.", "wError");
                return RedirectToAction("Error", "Mensagem", new { mensagem = "Não foi possível validar o ReCaptcha do Google, retornou vazio ou nulo." });
            }
            //Lista de erros e avaliação do ReCaptcha
            ICollection<string> listaErros = [];
            ICollection<string> listaAvaliacao = [];   //avaliação sem erros/apenas warnings ou pontos positivos.

            //Se não retornou nada, então não está validando o ReCaptcha do Google.
            if (validacaoReCaptcha.Count == 0)
            {
                listaAvaliacao.Add("Não está validando o ReCaptcha do Google! ReCaptcha pode estar desativado no código ou no cadastro do Google!");
                listaAvaliacao.Add(@"Consultar a conta no Google Cloud: https://cloud.google.com/sdk/docs/install?hl=pt-br");
                listaAvaliacao.Add(@"Verificar se está correta a instalação local do Google Cloud CLI Core (GoogleCloudSDKInstaller.exe)");
                listaAvaliacao.Add(@"Depois de entrar no Google Cloud, consulte Console: https://console.cloud.google.com/apis/dashboard?hl=pt-br&project=labwebmvc");
                listaAvaliacao.Add(@"Consulte no Google Cloud páginas das métricas: https://console.cloud.google.com/apis/api/recaptchaenterprise.googleapis.com/metrics?project=labwebmvc&hl=pt-br");
            }
            else
            {
                //Se retornou alguma linha de ERRO de validacaoReCaptcha, vamos devolver e abortar o Login do usuário:
                foreach (string item in validacaoReCaptcha)
                {
                    if (item.Contains("ERRO:"))
                    {
                        listaErros.Add(item);
                        listaErros.Add(@"Consultar a conta no Google Cloud: https://cloud.google.com/sdk/docs/install?hl=pt-br");
                    }
                    else
                    {
                        listaAvaliacao.Add(item);
                    }
                }

                //Pega de volta o conteúdo completo que havia sido armazenado do response no primeiro método de Login.
                HttpResponseMessage? conteudoCompleto = ExtensionsMethods.Genericos.GenericValidations.ConteudoCompletoReponseCaptchaGoogle;

                //Vamos checar o conteúdo do response do ReCaptcha e validar alguns dados que consideramos importantes.
                listaErros = _captchaValidator.ValidaReCaptchaResponse(conteudoCompleto, ref listaErros);
                if (listaErros.Count > 0)
                {
                    string mensagem = "\nERRO(s) RETORNADO(s) APÓS VALIDAR RECAPTCHA:\n" + string.Join("\n", listaErros);
                    _eventLogHelper.LogEventViewer($"{mensagem}, tentativa no login: {vm?.LoginUsuario} em {DateTime.UtcNow}", "wWarning");
                }
            }
            if (listaAvaliacao.Count > 0)
            {
                string mensagem = "\nRELATÓRIO DE AVALIAÇÃO DO RECAPTCHA RETORNADO PELO GOOGLE CLOUD: ";
                foreach (string item in listaAvaliacao)
                {
                    mensagem = string.Format("{0}\n{1}", mensagem, item);
                }
                _eventLogHelper.LogEventViewer(mensagem + "\n* * *, avaliação do login: " + vm?.LoginUsuario + " em " + DateTime.UtcNow.ToString(), "wInfo");
            }

            // Se não retornou o Token do Google ReCaptcha, não prossegue com o Login
            if (vm is { GoogleCaptchaToken: null or "" })
            {
                _eventLogHelper.LogEventViewer("Falhou tentativa de realizar Login porque o Google ReCaptcha não retornou o token: " + vm.LoginUsuario, "wError");
                return RedirectToAction("Error", "Mensagem", new { mensagem = "Falhou tentativa de realizar Login porque o Google ReCaptcha não retornou o Token!" });
            }
            return await ContinuarLogin(vm);
        }

        [NonAction]
        private async Task<IActionResult> ContinuarLogin(vmLogin? vm)
        {
            /*
             * Entra aqui para localizar o usuário...
             */
            if (vm != null)
            {
                vmSenhas? validaLogin = await _validacoesDeSenhas.RetornaValidacaoLogin(vm)!;

                if (vm != null && validaLogin != null && validaLogin.SituacaoLogin == (int)TipoSituacaoLogin.SemRestricao)
                {
                    //Descobre a base certa a ser utilizada pelo e-mail do login que foi validado acima!
                    var conexaoGenerica = _connectionService.GetConnectionString();

                    //Atualiza a connection string global
                    _connectionService.SetConnectionString(validaLogin.StringDeConexao);

                    //Salvando as sessions para uso GLOBAL no sistema
                    //Grava as variáveis do Usuário de Sessão/Session incluindo o token gerado pelo Google reCaptcha
                    HttpContext.Session.SetString("SessionLogin", validaLogin.LoginUsuario ?? "");
                    HttpContext.Session.SetString("SessionEmail", validaLogin.Email ?? "");
                    HttpContext.Session.SetString("SessionNome", validaLogin.NomeCompleto ?? "");
                    HttpContext.Session.SetString("SessionCNPJEmpresa", validaLogin.CNPJEmpresa ?? "");
                    HttpContext.Session.SetString("SessionNomeEmpresa", validaLogin.NomeEmpresa ?? "");

                    //Retorna e guarda o token que foi gerado pelo Google reCaptcha, se houver.
                    if (!string.IsNullOrEmpty(vm?.GoogleCaptchaToken))
                        HttpContext.Session.SetString("SessionToken", vm.GoogleCaptchaToken);

                    //Continua salvando as sessions para uso GLOBAL no sistema
                    string UF = _db.Empresa.FirstOrDefault()?.UF ?? "RJ";
                    HttpContext.Session.SetString("SessionUF", UF);

                    //Para controlar o Menu esconder e mostrar somente quando o Login for realizado com sucesso!
                    //USA as bibliotecas:
                    //using System.Security.Claims;
                    //using Microsoft.AspNetCore.Authentication;
                    //using Microsoft.AspNetCore.Authentication.Cookies;
                    List<Claim> claims =
                              [
                                   new Claim(ClaimTypes.Name, validaLogin.LoginUsuario ?? ""),
                                   new Claim(ClaimTypes.Email, validaLogin.Email ?? ""),
                                   new Claim("NomeCompleto", validaLogin.NomeCompleto ?? ""),
                                   new Claim("Empresa", validaLogin.NomeEmpresa ?? "")
                                   // Você pode adicionar mais Claims se necessário
                              ];
                    ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ClaimsPrincipal principal = new(identity);

                    // Faz login com cookie
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    //..

                    //Retorna OK, após Login validado!
                    return RedirectToAction("Index", "Home", new { mensagem = vm?.LoginUsuario != null ? vm.LoginUsuario.MensagemStartUp(false) : "" });
                }
                else if (validaLogin != null && (validaLogin.SituacaoLogin == (int)TipoSituacaoLogin.ComRestricao))
                {
                    _eventLogHelper.LogEventViewer("Falhou tentativa de realizar Login: " + MascararEmail(vm?.LoginUsuario), "wError");
                    TempData["MensagemErroLogin"] = "Login inválido - reveja suas credenciais";
                    return Redirect("/Home/Login");  //retorna pro Login
                }
                else if (validaLogin != null && (validaLogin.SituacaoLogin == (int)TipoSituacaoLogin.SemVerificacao))
                {
                    if (vm == null)
                    {
                        _eventLogHelper.LogEventViewer("[Home] Usuário ainda não fez a verificação de Email via caixa-postal para liberação de Login: " + MascararEmail(vm?.LoginUsuario), "wWarning");
                        return RedirectToAction("Error", "Mensagem", new { mensagem = "Ainda não foi feita a validação de email via caixa-postal para liberação de login" });
                    }
                    _eventLogHelper.LogEventViewer("Login inválido - reveja suas credenciais: " + MascararEmail(vm?.LoginUsuario), "wWarning");
                    TempData["MensagemErroLogin"] = "Login inválido - reveja suas credenciais";
                    return Redirect("/Home/Login");  //retorna pro Login
                }
                else
                {
                    _eventLogHelper.LogEventViewer("Procedimento de login falhou para evento desconhecido: " + MascararEmail(vm?.LoginUsuario), "wError");
                    return RedirectToAction("Error", "Mensagem", new { mensagem = "Procedimento de login falhou para evento desconhecido" });
                }
            }
            else
            {
                _eventLogHelper.LogEventViewer("Erro interno: dados do login ausentes ou não foram carregados.", "wError");
                return RedirectToAction("Error", "Mensagem", new { mensagem = "Erro interno: dados do login ausentes ou não foram carregados." });
            }
        }

        [HttpGet]
        [Route("SenhaEsquecida")]
        public IActionResult SenhaEsquecida(vmLogin login)
        {
            if (!ModelState.IsValid)
            {
                //LoggerFile.Write("ACESSO: Solicitação de recuperação de Senha do Login: " + login.Email);
                _eventLogHelper.LogEventViewer("ACESSO: Solicitação de recuperação de Senha do Login: " + login.Email);
                return RedirectToAction("SenhaEsquecida", "Senhas", new { mensagem = login.Email != null ? login.Email.MensagemStartUp(false) : "" });
            }
            return View();
        }

        [HttpGet]
        [Route("Privacy")]
        public IActionResult Privacy()         /* Privacy em Home, livre de login */
        {
            object[] colecao = { "Sobre" + Getbolinha + "Privacidade", false };
            ViewBag.TextoMenu = colecao;
            return View();
        }

        [HttpGet]
        [Route("NossoSistema")]
        public IActionResult NossoSistema()    /* NossoSistema em Home, livre de login */
        {
            object[] colecao = { "Sobre" + Getbolinha + "Nosso Sistema", false };
            ViewBag.TextoMenu = colecao;
            return View();
        }

        [HttpGet]
        [Route("Logout")]
        public IActionResult Logout()
        {
            // Remove autenticação
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Limpa a sessão (opcional, mas recomendado)
            LimparSessao();

            _eventLogHelper.LogEventViewer("[Home] A sessão foi encerrada via logout", "wInfo");

            ViewBag.TextoMenu = "Logout".MensagemStartUp();
            return View();
        }

        private void LimparSessao()
        {
            //Atenção: Apenas limpar as Sessions, porque se usar remove() ou clear(), elas não serão mais preenchidas nos login subsequentes!
            HttpContext.Session.SetString("SessionToken", "");
            HttpContext.Session.SetString("SessionLogin", "");
            HttpContext.Session.SetString("SessionEmail", "");
            HttpContext.Session.SetString("SessionNome", "");
            HttpContext.Session.SetString("SessionCNPJEmpresa", "");
            HttpContext.Session.SetString("SessionNomeEmpresa", "");
            //Remove o cookie associado à sessão
            Response.Cookies.Delete(".LabWeb7.Session");
        }
    } //Fim
}