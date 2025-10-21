using BLL;
using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.Controllers;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Interfaces.Criptografias;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static ExtensionsMethods.Genericos.Enumeradores;

namespace LabWebMvc.MVC.Areas.Validations
{
    public class ValidacoesDeSenhas : IValidacoesDeSenhas
    {
        private Db _db;

        private readonly IEventLogHelper _eventLog;
        private readonly GeralController _geralController;
        private readonly ITempoServidorService _tempoService;
        private readonly IConnectionService _connectionService;
        private readonly IDbFactory _dbFactory;

        public ValidacoesDeSenhas(IEventLogHelper eventLog, GeralController geralController, 
                                  ITempoServidorService tempoService, IConnectionService connectionService, 
                                  IDbFactory dbFactory)
        {
            _eventLog = eventLog;
            _geralController = geralController;
            _tempoService = tempoService;
            _connectionService = connectionService;
            _dbFactory = dbFactory;

            // Inicializa _db com a conexão padrão, pois não pode ser injetado porque fará troca de bases dinâmicas.
            _db = _dbFactory.Create();
        }

        /* Retorna Login SOMENTE para recuperação de senha */
        public void RecuperaLogin(ref vmSenhas objLogin)
        {
            objLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemVerificacao;
            string loginUsuario = string.IsNullOrEmpty(objLogin.LoginUsuario) ? "0" : objLogin.LoginUsuario;
            DateTime dataNascimento = objLogin.DataNascimento;

            //Primeiro verifica se o usuário está no cadastro de Usuários da Web, e valida CPF e Data de Nascimento
            string cpf = objLogin.CPF != null ? objLogin.CPF.CPFSemFormatacao() : string.Empty;
            if (string.IsNullOrEmpty(cpf))
            {
                objLogin = new vmSenhas();
                //LoggerFile.Write("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/FALHOU: este usuário está sem CPF no cadastro de senhas: " + objLogin.LoginUsuario);
                _eventLog.LogEventViewer("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/FALHOU: este usuário está sem CPF no cadastro de senhas: " + objLogin.LoginUsuario, "wWarning");
                return;
            }
            try
            {
                UsuariosWeb usuWeb = _db.UsuariosWeb.Single(u => u.CPFUsuario == cpf && u.DataNascimentoUsuario == dataNascimento);
                if (usuWeb != null)
                {
                    //Agora verifica se o usuário possui um registro de senha
                    Senhas? login = _db.Senhas.FirstOrDefault(l => l.LoginUsuario == loginUsuario && l.Id == usuWeb.SenhaId);

                    if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Nao && login.EmailConfirmado == (int)TipoEmailConfirmado.Sim && (login.DataExpira == null || login.DataExpira >= DateTime.Now))
                    {//SEM RESTRIÇÃO pode recuperar, mas tem que retornar com os dados de login
                        objLogin.RecuperacaoDeSenha = true;
                        objLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemRestricao;
                        objLogin.NomeCompleto = $"Sr(a). " + login.NomeCompleto;
                        objLogin.Email = login.Email;
                    }
                    else if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Nao && login.EmailConfirmado == (int)TipoEmailConfirmado.Nao)
                    {//JÁ ESTÁ EM VERIFICAÇÃO pode recuperar, mas tem que retornar com os dados de login
                        objLogin.RecuperacaoDeSenha = true;  //true, apesar de o usuário JÁ ESTAR em verificação, ele será avisado disso!
                        objLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemVerificacao;
                        objLogin.NomeCompleto = $"Sr(a). " + login.NomeCompleto;
                        objLogin.SenhaUsuario = login.SenhaUsuario;  //neste caso, ele já possui a senha alterada, vamos reenviá-la
                        objLogin.Email = login.Email;
                        _eventLog.LogEventViewer("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/SUSPENSO: Usuário ainda não confirmou seu Email de Login anterior, e está tentando recuperar senha: " + objLogin.LoginUsuario, "wWarning");
                    }
                    else if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Sim)
                    {
                        objLogin.SituacaoLogin = (int)TipoSituacaoLogin.ComRestricao;
                        _eventLog.LogEventViewer("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/BLOQUEIO: Login bloqueado para o usuário, e está tentando recuperar senha: " + objLogin.LoginUsuario, "wWarning");
                    }
                    else if (login == null) //INCRÍVEL, o cara ainda não possui um Login, mas está em UsuariosWeb?!
                    {
                        objLogin = new vmSenhas();
                        _eventLog.LogEventViewer("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/FALHOU/ESTRANHO: este usuário ainda não possui um Login, mas está em 'UsuariosWeb' " + objLogin.LoginUsuario, "wError");
                    }
                }
            }
            catch
            {
                objLogin = new vmSenhas();
                _eventLog.LogEventViewer("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/FALHOU: tentativa de recuperação de login para " + objLogin.LoginUsuario, "wWarning");
            }
            finally
            { }
        }

        public async Task<string> CriaUsuarioSenha(vmSenhas obj, int adm = 0)
        {
            void SalvaEmailEmpresaCliente(string emailUsuario)
            {
                string cnpjEmpresa = Areas.Utils.Utils.LoginCNPJEmpresaLogado() ?? obj.CNPJEmpresa;   // _httpContext.Session.GetString("SessionCNPJEmpresa");

                //Repositório de dados da Empresa-Cliente
                EmpresaClienteRepository repo = new();
                string SQLEmpresa = repo.RetornaSelectEmpresaCliente(cnpjEmpresa, "CNPJ");  //retorna Select da Empresa em LABWEB7Empresas
                string admStringConexao = Areas.Utils.Utils.GetValorSetupDoServico("ConexaoPostgreSQL", "PNpgsqlConnection StringEmpresas") ?? "";
                admStringConexao = admStringConexao.ReformaTexto("usubanco", BasePadrao.UserId).ReformaTexto("ususenha", BasePadrao.Password);

                using (var conexao = new NpgsqlConnection(admStringConexao))    //cria conexão Empresas
                {
                    conexao.Open();

                    Emails emailLocalizado = new();

                    //Lendo o registro da Empresa-Cliente
                    using (var comando = new NpgsqlCommand(SQLEmpresa, conexao))
                    {
                        using (NpgsqlDataReader reader = comando.ExecuteReader())
                        {
                            if (reader.Read()) // retornando dados da EmpresaCliente
                            {
                                emailLocalizado.EmpresaClienteId = Convert.ToInt32(reader["Id"].ToString());
                            }
                        }
                    }
                    //Inserindo o Email o novo do usuário no controle geral de Empresas
                    string linkSQL = @"INSERT INTO ""Emails"" (Email, EmpresaClienteId) VALUES (@Email, @EmpresaClienteId)";
                    using (var comando = new NpgsqlCommand(linkSQL, conexao))
                    {
                        //Adiciona os parâmetros para evitar SQL Injection
                        comando.Parameters.AddWithValue("@Email", emailUsuario);
                        comando.Parameters.AddWithValue("@EmpresaClienteId", emailLocalizado.EmpresaClienteId);

                        //Executa a inserção
                        comando.ExecuteNonQueryAsync();
                    }
                }
            }//Fim sub método

            string mensagem = string.Empty;

            Senhas? Senhas = await _db.Senhas.Where(s => s.Email == obj.Email ||
                                     (s.NomeUsuario == obj.NomeUsuario && (s.Email == obj.Email)) ||
                                     (s.Email == obj.Email) ||
                                     (s.NomeCompleto == obj.NomeCompleto)).SingleOrDefaultAsync();
            if (Senhas != null)
            {
                if (Senhas.Email == obj.Email)
                    return "Já existe Usuário cadastrado com este e-mail";
                else if (Senhas.NomeUsuario == obj.NomeUsuario.ToUpper() && Senhas.Email == obj.Email)
                    return "Usuario já cadastrado com este Email";
                else
                    return "Já existe Usuário cadastrado com algum destes dados (Nome ou Email)";
            }
            //
            //OBSERVAR QUE NESTE MÉTODO A PARTIR DAQUI NÃO COLOQUEI TUDO "await", PORQUE OS PASSOS ABAIXO PRECISAM
            //SALVAR OS DADOS ANTES DE CHAMAR QUALQUER OUTRA ROTINA ASSÍNCRONA (não podemos ter paralelismo aqui)!
            //
            using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = _db.Database.BeginTransaction();    //Nativo do Core bem melhor!!!!
            try
            {
                Senhas senha = new()
                {
                    LoginUsuario = obj.Email.ToLower(),
                    NomeUsuario = obj.NomeUsuario.ToUpper(),
                    NomeCompleto = obj.NomeCompleto.ToUpper(),
                    Email = obj.Email.ToLower(),
                    SenhaUsuario = "BAHImD+dYlY+zWRFNMimXw==",  //12345 (por enquanto) Criptografias.GeraSenhaAleatoria(),
                    DataCadastro = _tempoService.ObterDataHoraServidor().ToFormataData(),
                    DataExpira = obj.DataExpira ?? null,
                    UsarAssinatura = obj.UsarAssinatura ?? 0,
                    Assinatura = obj.Assinatura,
                    NomeAssinatura = obj.NomeAssinatura,
                    Administrador = obj.Administrador ?? adm,
                    EmailConfirmado = adm,     //se for administrador então o Email consideramos confirmado! Caso contrário, aconpanha o que vier.
                    CNPJEmpresa = Areas.Utils.Utils.LoginCNPJEmpresaLogado() ?? obj.CNPJEmpresa
                };

                _db.Senhas.Add(senha);
                _db.SaveChanges();    // Salva as alterações para gerar o ID

                UsuariosWeb usuarioWeb = new()
                {
                    SenhaId = senha.Id,
                    CPFUsuario = obj.CPF ?? "11111111111",
                    DataNascimentoUsuario = obj.DataNascimento > Convert.ToDateTime("01/01/1000") ? obj.DataNascimento : Convert.ToDateTime("01/01/1900"),
                    CNPJEmpresa = senha.CNPJEmpresa,
                    DataCadastro = senha.DataCadastro
                };

                _db.UsuariosWeb.Add(usuarioWeb);

                // Salva as alterações para o registro na tabela "UsuariosWeb"
                if (_db.SaveChanges() <= 0)
                {
                    _eventLog.LogEventViewer("ERRO: Usuário não foi salvo: " + obj.NomeUsuario, "wWarning");
                    return "Usuário NÃO foi salvo";
                }

                // Chama o método e captura exceções na lógica do try
                SalvaEmailEmpresaCliente(senha.Email);  //obrigatório para conseguir validar usuário por e-mail de qualquer empresa-cliente

                transaction.Commit();
            }
            catch (DbUpdateException ex)
            {
                transaction.Rollback();
                _eventLog.LogEventViewer("[Inclusão de Usuários] Erro ao salvar no banco: " + ex.InnerException?.Message ?? ex.Message, "wErro");
                return "Usuário NÃO foi salvo no Banco de Dados";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _eventLog.LogEventViewer("[Inclusão de Usuários] Erro inesperado: " + ex.Message, "wError");
                return "Usuário NÃO foi salvo por um erro inesperado";
            }

            return "Usuário foi salvo";
        }

        /* Retorna Login para fazer acesso efetivo ao Sistema com o email do usuário na empresa do cliente correto */
        public async Task<vmSenhas>? RetornaValidacaoLogin(vmLogin? vm)
        {
            static async Task<EmpresaCliente> LocalizaEmpresaAsync(GeralController geralController, IEventLogHelper eventLog, string admStringConexao, string? loginEmail, string empresaId = "0")
            {
                var repo = new EmpresaClienteRepository(admStringConexao, geralController, eventLog);
                string SQLEmpresa = !string.IsNullOrEmpty(loginEmail) && empresaId.ToInt32() == 0
                       ? repo.RetornaSelectEmpresaCliente(loginEmail, "Email")
                       : repo.RetornaSelectEmpresaCliente(empresaId, "Id");

                var cliente = new EmpresaCliente();
                using (var conexao = new NpgsqlConnection(admStringConexao))
                {
                    await conexao.OpenAsync();
                    using (NpgsqlCommand comando = new(SQLEmpresa, conexao))
                    {
                        using (NpgsqlDataReader reader = await comando.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                cliente = new EmpresaCliente
                                {
                                    Id = reader["Id"].ToString() ?? "0",
                                    CNPJ = reader["CNPJ"].ToString() ?? "",
                                    Email = reader["Email"].ToString() ?? "",
                                    StringConexao = reader["StringConexao"].ToString() ?? "erro",
                                    LimiteUsuarios = reader["LimiteUsuarios"].ToString() ?? "0",
                                    DataExpira = Convert.ToDateTime(reader["DataExpira"].ToString())
                                };
                            }
                        }
                    }
                }
                return cliente;
            }
            //Submétodo:
            static async Task<string> CriaCacheEmailAsync(string admStringConexao, string? loginEmail, EmpresaCliente cliente)
            {
                var eventLog2 = new ExtensionsMethods.EventViewerHelper.EventLogHelper();

                if (string.IsNullOrEmpty(cliente.Id) || string.IsNullOrEmpty(cliente.CNPJ) || string.IsNullOrEmpty(cliente.Email))
                {
                    //Este usuário não existe cadastrado no Sistema, portanto, login inválido, aborta imediatamente!
                    eventLog2.LogEventViewer("[ValidacoesDeSenhas] IMPEDIMENTO IMEDIATO: Houve tentativa de acesso com login não cadastrado no Sistema: " + loginEmail, "wErro");
                    admStringConexao = "erro"; //no async vai retornar sempre o valor dó parâmetro que foi modificado: admStringConexao.
                    return admStringConexao;
                }

                try
                {
                    using (var conexao = new NpgsqlConnection(admStringConexao))
                    {
                        await conexao.OpenAsync();

                        // Inicia a transação como DbTransaction
                        using (System.Data.Common.DbTransaction transacao = await conexao.BeginTransactionAsync())
                        {
                            if (transacao is SqlTransaction sqlTransacao)
                            {
                                try
                                {
                                    // Inserção na tabela Emails
                                    string linkSQL = $@"INSERT INTO ""Emails"" (Email, EmpresaClienteId) VALUES (@Email, @EmpresaClienteId)";
                                    using (NpgsqlCommand comando = new(linkSQL, conexao, (NpgsqlTransaction)transacao))
                                    {
                                        comando.Parameters.AddWithValue("@Email", cliente.Email);
                                        comando.Parameters.AddWithValue("@EmpresaClienteId", cliente.Id);
                                        await comando.ExecuteNonQueryAsync();
                                    }

                                    // Inserção na tabela EmpresaLogin
                                    linkSQL = $@"INSERT INTO ""EmpresaLogin{cliente.CNPJ}"" (Email, Perfil) VALUES (@Email, @Perfil)";

                                    using (NpgsqlCommand comando = new(linkSQL, conexao, (NpgsqlTransaction)transacao))
                                    {
                                        comando.Parameters.AddWithValue("@Email", cliente.Email);
                                        comando.Parameters.AddWithValue("@Perfil", 1); // Perfil Administrador
                                        await comando.ExecuteNonQueryAsync();
                                    }

                                    // Confirma a transação
                                    await transacao.CommitAsync();

                                    // Atualiza a string de conexão
                                    admStringConexao = cliente.StringConexao;
                                }
                                catch (Exception ex)
                                {
                                    // Reverte a transação em caso de erro
                                    await transacao.RollbackAsync();
                                    eventLog2.LogEventViewer("[ValidacoesDeSenhas] Erro durante a transação: " + ex.Message, "wErro");

                                    // Define a conexão como erro
                                    admStringConexao = "erro";
                                }
                            }
                            else
                            {
                                eventLog2.LogEventViewer("[ValidacoesDeSenhas] A transação não é do tipo SqlTransaction no método: CriaCacheEmailAsync.", "wErro");
                                admStringConexao = "erro";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    eventLog2.LogEventViewer($"[ValidacaoDeSenhas] Erro ao criar o cache de email: {ex.Message}", "wErro");
                    admStringConexao = "erro";
                }

                return admStringConexao;
            }//Fim do CriaCacheEmailAsync (submétodo)

            vmSenhas? senhasLogin = new();
            if (vm == null)
            {
                _eventLog.LogEventViewer("[ValidacoesDeSenhas] IMPEDIMENTO: falhou para carregar o login", "wWarning");
                return senhasLogin;
            }

            //Vamos validar a existência do Login/Email do usuário
            //Se ainda não existir, e for ADMINISTRADOR-Cliente e for a primeira vez que está se logando no Sistema, vai criar o registro de Login em EmpresaLogin[CNPJ].
            //Mas se o registro dele já existir no EmpresaLogin[CNPJ] então segue normalmente
            string loginEmail = vm.LoginUsuario ?? "";
            string loginCNPJ = string.Empty;

            string admStringConexao = Areas.Utils.Utils.GetValorSetupDoServico("ConexaoPostgreSQL", "PSQLConnectionStringEmpresas") ?? "";
            admStringConexao = admStringConexao.ReformaTexto("usubanco", BasePadrao.UserId).ReformaTexto("ususenha", BasePadrao.Password);

            //Valida se tem loginEmail na tabela de Emails (LABWEB7Empresas)
            //Atualiza com a string de conexão dinâmica
            _connectionService.SetConnectionString(admStringConexao);

            //Está acessando o banco de dados LABWEB7Empresas para validar o Email na tabela de Emails
            EmpresaClienteRepository repo = new(admStringConexao, _geralController, _eventLog);
            string SQL = repo.RetornaSelectEmails(loginEmail);

            Emails emailLocalizado = new();

            using (var conexao = new NpgsqlConnection(admStringConexao))
            {
                await conexao.OpenAsync();

                using (var comando = new NpgsqlCommand(SQL, conexao))
                {
                    using (NpgsqlDataReader reader = await comando.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            emailLocalizado.Email = reader["Email"].ToString() ?? "";
                            emailLocalizado.EmpresaClienteId = Convert.ToInt32(reader["EmpresaClienteId"].ToString());
                        }
                    }
                }
            }
            if (emailLocalizado == null || string.IsNullOrEmpty(emailLocalizado.Email) || emailLocalizado.Email != loginEmail)
            {
                //Significa que é o primeiro acesso do ADMINISTRADOR Cliente do Sistema
                EmpresaCliente cliente = await LocalizaEmpresaAsync(_geralController, _eventLog, admStringConexao, loginEmail, "0");
                admStringConexao = await CriaCacheEmailAsync(admStringConexao, loginEmail, cliente);

                vmSenhas vmS = new()
                {
                    Email = loginEmail.ToLower(),
                    LoginUsuario = loginEmail.ToLower(),
                    NomeUsuario = "ADMINISTRADOR",
                    NomeCompleto = "ADMINISTRADOR DO SISTEMA",
                    SenhaUsuario = "BAHImD+dYlY+zWRFNMimXw==",  //12345 (por enquanto) //TODO Criptografias.GeraSenhaAleatoria(),
                    DataCadastro = _tempoService.ObterDataHoraServidor().ToFormataData(),
                    Administrador = 1,
                    CNPJEmpresa = cliente.CNPJ,
                    StringDeConexao = cliente.StringConexao
                };

                if (admStringConexao != "erro") //significa que foi digitado um e-mail inválido para login.
                {
                    //Atualiza com a string de conexão dinâmica
                    _connectionService.SetConnectionString(admStringConexao);

                    await CriaUsuarioSenha(vmS, 1);
                }
            }
            else //Prossegue com a validação de Login identificando a Empresa e pegando o script de conexão
            {
                //Localiza dados na Empresa/Cliente
                EmpresaCliente cliente = await LocalizaEmpresaAsync(_geralController, _eventLog, admStringConexao, emailLocalizado.Email, emailLocalizado.EmpresaClienteId.ToString());
                admStringConexao = cliente.StringConexao;  //pega o script de conexão do cliente
            }
            if (admStringConexao == "erro")
            {
                //LoggerFile.Write("[ValidacoesDeSenhas] IMPEDIMENTO: Login impedido para o usuário não cadastrado no Sistema: " + loginEmail);
                _eventLog.LogEventViewer("[ValidacoesDeSenhas] IMPEDIMENTO: Login impedido para o usuário não cadastrado no Sistema: " + loginEmail, "wErro");
                senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.ComRestricao;
                return senhasLogin;  //retorna imediatamente para refazer o Login "ComRestricao"
            }
            else if (string.IsNullOrEmpty(admStringConexao))
            {
                //LoggerFile.Write("[ValidacoesDeSenhas] IMPEDIMENTO: Login do usuário está cadastrado no Sistema, mas não consegue entrar na Empresa: " + loginEmail);
                _eventLog.LogEventViewer("[ValidacoesDeSenhas] IMPEDIMENTO: Login do usuário está cadastrado no Sistema, mas não consegue entrar na Empresa: " + loginEmail, "wErro");
                senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.ComRestricao;
                return senhasLogin;  //retorna imediatamente para refazer o Login "ComRestricao"
            }
            // 1. Atualiza com a string de conexão dinâmica do cliente correta e não troca mais durante o uso do sistema!
            _connectionService.SetConnectionString(admStringConexao);

            // 2. Atualiza o _db com a nova conexão dinâmica para a Base do Cliente
            var optionsBuilder = new DbContextOptionsBuilder<Db>().UseNpgsql(_connectionService.GetConnectionString());
            _db = new Db(optionsBuilder.Options, _connectionService, _eventLog);

            //Segue fluxo normal, recebendo a senha normalmente do usuário na sua própria base, ou seja, na base do Cliente.
            //O usuário faz login com a senha normal, então precisa criptografar em memória para achá-la coretamente no banco de dados...
            string senhaDecripto = vm.SenhaUsuario != null ? CriptoDecripto.Criptografa_StringToString(vm.SenhaUsuario) : "SenhaErrada@@#$&NãoPodeSerVazia";

            Senhas? login = _db.Senhas.Where(l => l.LoginUsuario == vm.LoginUsuario && l.SenhaUsuario == senhaDecripto).Include(l => l.UsuariosWeb).SingleOrDefault();
            if (login != null && vm.SenhaUsuario != null)
            {
                senhasLogin.Senhas = login;
                senhasLogin.NomeCompleto = login.NomeCompleto;
                senhasLogin.Email = login.Email;
                senhasLogin.CPF = login.UsuariosWeb?.CPFUsuario;
                senhasLogin.NomeEmpresa = _db.Empresa.Single().NomeFantasia ?? "";
                senhasLogin.CNPJEmpresa = _db.Empresa.Single().CNPJ ?? "";
            }
            //Validações importantes do Login
            if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Nao && login.EmailConfirmado == (int)TipoEmailConfirmado.Sim && (login.DataExpira == null || login.DataExpira >= DateTime.Now))
            {
                senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemRestricao;
                return senhasLogin;
            }
            else if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Nao && login.EmailConfirmado == (int)TipoSituacaoLogin.SemVerificacao)
            {
                _eventLog.LogEventViewer("[ValidacoesDeSenhas] SUSPENSO: Usuário ainda não confirmou seu Email de Login: " + login.LoginUsuario, "wWarning");
                senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemVerificacao;
            }
            else if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Sim)
            {
                _eventLog.LogEventViewer("[ValidacoesDeSenhas] BLOQUEIO: Login bloqueado para o usuário: " + login.LoginUsuario, "wWarning");
                senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.ComRestricao;
            }

            return senhasLogin;  //retorna para refazer o Login "ComRestricao"
        }

        /* Retorna Login para fazer acesso ao Sistema */
        public vmSenhas? RetornaLogin(string loginUsuario, string senhaUsuario)
        {
            vmSenhas senhasLogin = new()
            {
                SituacaoLogin = (int)TipoSituacaoLogin.ComRestricao
            };
            //Criptografa a senha que o usuário havia digitado no box de login
            if (!senhaUsuario.StartsWith("@") && !string.Equals(loginUsuario, "adm@adm", StringComparison.OrdinalIgnoreCase))
            {
                senhaUsuario = CriptoDecripto.CriptografaSenha(senhaUsuario);    //TODO
            }
            if (string.Equals(loginUsuario, "adm@adm", StringComparison.OrdinalIgnoreCase) && new[] { "@master105", "master105" }
                      .Any(s => string.Equals(senhaUsuario, s, StringComparison.Ordinal)))  // se veio com arroba, então ainda não criptografou
            {
                senhaUsuario = CriptoDecripto.CriptografaSenha(senhaUsuario);    //TODO
            }
            Senhas? login = _db.Senhas.FirstOrDefault(l => l.LoginUsuario == loginUsuario && l.SenhaUsuario == senhaUsuario);
            if (login != null)
                senhasLogin.Senhas = login;

            //Validações importantes do Login
            if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Nao && login.EmailConfirmado == (int)TipoEmailConfirmado.Sim && (login.DataExpira == null || login.DataExpira >= DateTime.Now))
            {
                senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemRestricao;
                senhasLogin.LoginUsuario = login.LoginUsuario;
                senhasLogin.Funcao = "";   //TODO
                return senhasLogin;
            }
            else if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Nao && login.EmailConfirmado == (int)TipoSituacaoLogin.SemVerificacao)
            {
                senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemVerificacao;
                senhasLogin.LoginUsuario = login.LoginUsuario;
                senhasLogin.Funcao = "";  //TODO
                _eventLog.LogEventViewer("SUSPENSO: Usuário ainda não confirmou seu Email de Login: " + login.LoginUsuario, "wWarning");
            }
            else if (login == null || login.Bloqueado == (int)TipoContaBloqueado.Sim)
            {
                senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.ComRestricao;
                senhasLogin.LoginUsuario = login != null ? login.LoginUsuario : loginUsuario;
                senhasLogin.Funcao = "";  //TODO
                _eventLog.LogEventViewer("BLOQUEIO: Login bloqueado para o usuário: " + loginUsuario, "wWarning");
            }

            return senhasLogin;
        }

        /* Retorna Login SOMENTE para recuperação de senha */
        public vmSenhas? RetornaLogin(string loginUsuario, string cpf, DateTime nascimento)
        {
            vmSenhas senhasLogin = new()
            {
                SituacaoLogin = (int)TipoSituacaoLogin.SemVerificacao
            };
            try
            {
                //Primeiro verifica se o usuário possui um Login
                Senhas? login = _db.Senhas.Where(l => l.LoginUsuario == loginUsuario).SingleOrDefault();
                if (login != null)
                {
                    senhasLogin.Senhas = login;

                    //Depois verifica se o usuário está no cadastro de Usuários da Web, e valida CPF e Data de Nascimento
                    UsuariosWeb? usuWeb = _db.UsuariosWeb.Where(u => u.SenhaId == login.Id && u.CPFUsuario == cpf && u.DataNascimentoUsuario == nascimento).SingleOrDefault();
                    if (usuWeb != null)
                    {
                        if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Nao && login.EmailConfirmado == (int)TipoEmailConfirmado.Sim && (login.DataExpira == null || login.DataExpira >= DateTime.Now))
                        {//SEM RESTRIÇÃO pode recuperar
                            senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemVerificacao;
                            senhasLogin.RecuperacaoDeSenha = true;
                            return senhasLogin;
                        }
                        else if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Nao && login.EmailConfirmado == (int)TipoSituacaoLogin.SemVerificacao)
                        {
                            _eventLog.LogEventViewer("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/SUSPENSO: Usuário ainda não confirmou seu Email de Login anterior, e está tentando recuperar senha: " + login.LoginUsuario, "wWarning");
                            senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.SemVerificacao;
                        }
                        else if (login != null && login.Bloqueado == (int)TipoContaBloqueado.Sim)
                        {
                            _eventLog.LogEventViewer("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/BLOQUEIO: Login bloqueado para o usuário, e está tentando recuperar senha: " + login.LoginUsuario, "wWarning");
                            senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.ComRestricao;
                        }
                    }
                }
                else
                {
                    _eventLog.LogEventViewer("[ValidacoesDeSenhas] TENTATIVA DE LOGIN SEM CADASTRO: Usuário pode estar tentando fazer Login sem ter cadastro!", "wError");
                    senhasLogin.SituacaoLogin = (int)TipoSituacaoLogin.ComRestricao;
                }
            }
            catch
            {
                senhasLogin = new vmSenhas();
                //LoggerFile.Write("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/FALHOU: tentativa de recuperação de login para " + loginUsuario);
                _eventLog.LogEventViewer("[ValidacoesDeSenhas] RECUPERACAO DE LOGIN/FALHOU: tentativa de recuperação de login para " + loginUsuario, "wError");
            }
            finally
            { }
            return senhasLogin;
        }

        public class Emails
        {
            public string Email { get; set; } = null!;
            public int EmpresaClienteId { get; set; }
            public DateTime DataCadastro { get; set; }
        }

        public class EmpresaLogin
        {
            public int Id { get; set; }
            public string Email { set; get; } = null!;
            public int Perfil { get; set; }
        }
    }
}