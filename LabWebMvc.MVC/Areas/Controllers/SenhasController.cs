using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ExpressionCombiner;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Areas.Validations;
using LabWebMvc.MVC.Interfaces.Criptografias;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using static BLL.UtilBLL;
using static ExtensionsMethods.Genericos.Enumeradores;
using static LabWebMvc.MVC.Areas.Utils.Utils;
using DateTime = System.DateTime;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class SenhasController : BaseController
    {
        private readonly IPathHelper _pathHelper;
        private readonly IValidacoesDeSenhas _validacoesDeSenhas;

        public SenhasController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IPathHelper pathHelper,
            IValidacoesDeSenhas validacoesDeSenhas)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService)
        {
            _pathHelper = pathHelper;
            _validacoesDeSenhas = validacoesDeSenhas;
        }

        private static readonly HttpClient client = new();

        private void MontaControllers(string action, string controller, string parametros = "")
        {
            PartialFiltro.Action = action;
            PartialFiltro.Controller = controller;
            PartialFiltro.ActionButton = action + parametros;
            PartialFiltro.ControllerButton = controller;
            PartialFiltro.Esconde = false;
            ViewBag.TextoMenu = action.MensagemStartUp();
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Senhas")]
        public async Task<IActionResult> Index(string? Conteudo, int registros = 50)
        {
            MontaControllers("Index", "Senhas");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            ICollection<dynamic> listaGrid = [];
            List<Senhas> dados = [];    //tem que usar "List" por causa do "AddRange"

            int totalTabela = 0;
            int totalRegistros = 0;
            if (string.IsNullOrEmpty(Conteudo)) registros = 100; //quando não tem dados para filtrar

            totalTabela = _db.Senhas.AsNoTracking().AsEnumerable().Count();

            if (!string.IsNullOrEmpty(Conteudo))
            {
                dados = await _db.Senhas.AsNoTracking()
                          .FiltrarPorConteudo(Conteudo, x => x.NomeUsuario, x => x.NomeCompleto, x => x.Email, x => x.Id.ToString())
                          .OrderByDescending(x => x.Id)
                          .ToListAsync();

                if (Conteudo.Split('/').Count() == 3 || Conteudo.Split('-').Count() == 3) //está buscando alguma data
                {
                    DateTime dataBusca = Conteudo.Trim().FormataData("dd/MM/yyyy", true);
                    ICollection<Senhas> dadosQuery = await _db.Senhas.AsNoTracking()
                                                       .Where(l => (l.DataCadastro.Day > 0 &&
                                                                    l.DataCadastro.Year == dataBusca.Year &&
                                                                    l.DataCadastro.Month == dataBusca.Month &&
                                                                    l.DataCadastro.Day == dataBusca.Day) ||
                                                                    (l.DataExpira.HasValue &&
                                                                    l.DataExpira.Value.Year == dataBusca.Year &&
                                                                    l.DataExpira.Value.Month == dataBusca.Month &&
                                                                    l.DataExpira.Value.Day == dataBusca.Day)
                                                                   )
                                                       .ToListAsync();
                    if (dadosQuery.Count > 0)
                        dados.AddRange(dadosQuery);
                }
            }
            else
                dados = await _db.Senhas.AsNoTracking().OrderByDescending(o => o.Id).Take(registros).ToListAsync();

            foreach (Senhas item in dados)
            {
                totalRegistros++;
                vmSenhas resultado = new()
                {
                    Id = item.Id,
                    Email = item.Email,
                    NomeUsuario = item.NomeUsuario,
                    NomeCompleto = item.NomeCompleto,
                    DataCadastro = item.DataCadastro,
                    DataExpiraStr = item.DataExpira.RetornaTextoQuandoNullVazio("Nunca"),
                    UsarAssinaturaStr = item.UsarAssinatura.RetornaSN(),
                    BloqueadoStr = item.Bloqueado.RetornaSN(),
                    Administrador = item.Administrador,
                    EmailConfirmado = item.EmailConfirmado
                };
                listaGrid.Add(resultado);
            }

            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = listaGrid;

            //Finalização da View
            return _geralController.Validacao("Index,Senhas", "Cadastro de Usuários", totalRegistros, totalTabela, listaGrid);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirUsuarios")]
        public IActionResult IncluirUsuarios()
        {
            return _geralController.Validacao("IncluirUsuarios", "Cadastro de Usuários");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirUsuarios")]
        public async Task<IActionResult> SalvarUsuario(vmSenhas obj)
        {
            string redirecionaUrl = "Usuarios".MontaUrl(base.HttpContext.Request);

            // Valida campos obrigatórios
            if (string.IsNullOrEmpty(obj.NomeUsuario) || string.IsNullOrEmpty(obj.NomeCompleto) || string.IsNullOrEmpty(obj.Email) || string.IsNullOrEmpty(obj.CPF))
            {
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios" });
            }

            // Chama o método CriaUsuarioSenha de forma assíncrona
            string mensagem = await _validacoesDeSenhas.CriaUsuarioSenha(obj, 0);

            if (mensagem == "Usuário foi salvo")
            {
                return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = mensagem, action = "", sucesso = true });
            }
            else
            {
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = mensagem, action = "", sucesso = false });
            }
        }

        /* REFERENTE A MANUTENÇÃO DE SENHAS DOS USUÁRIOS PELO ADMINISTRADOR */

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarSenha")]
        public IActionResult AlterarSenha(string? Email)
        {
            Senhas dados = new();
            vmSenhas vm = new();

            dados = _db.Senhas.Where(c => c.Email == Email).AsNoTracking().First();   //espera-se um único registro, pois Email é campo chave único!

            if (dados != null)
            {
                vm.Email = dados.Email;
                vm.Id = dados.Id;
                vm.LoginUsuario = dados.LoginUsuario;
                vm.Email = dados.Email;
                vm.NomeCompleto = dados.NomeCompleto;
                vm.DataExpira = dados.DataExpira;
                vm.BloqueadoStr = dados.Bloqueado.RetornaSN();
                vm.Administrador = dados.Administrador;
                vm.SenhaUsuario = string.Empty;    //TODO ::: criptografar a senha
                vm.SenhaRepete = string.Empty;     //TODO ::: criptografar a senha
                vm.BoxEnviarEmail = true;          //TODO ::: tratar de enviar o email com a senha
            }

            //Finalização da View
            return _geralController.Validacao("AlterarSenha,Senhas", "Altera Senha do Usuário", vm);   //ViewBag.Senhas (passa tudo como parâmetro para a ViewBag do outro lado)
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirUsuario")]
        public async Task<JsonResult> ExcluirUsuario(string? Email) //OK
        {
            Senhas dados = new();

            bool deletou = false;

            dados = await _db.Senhas.Where(c => c.Email == Email).AsNoTracking().FirstAsync();   //espera-se um único registro, pois Email é campo chave único!

            if (dados != null)
            {
                if (dados.Administrador == 1)
                    return Json(new { titulo = "Atenção!", mensagem = "Usuário proprietário não pode ser excluído do Sistema", sucesso = deletou });

                //TODO ::: Deletar tudo do perfil deste usuário
                //TODO ::: Deletar usuario
                deletou = true;
            }

            if (deletou) //Retorna o Json para a mesma função javascript que chamou esta action "ExcluirUsuario".
                return Json(new { titulo = "Ok", mensagem = "Usuário excluído com sucesso", sucesso = deletou });
            else
                return Json(new { titulo = "Atenção!", mensagem = "Usuário não foi excluído", sucesso = deletou });
        }

        /* REFERENTE A MANUTENÇÃO DE SENHAS DOS LOGINS DOS USUÁRIOS PELO ADMINISTRADOR
         * aqui neste HttPost não resolve com vm (View Model),
         * por isso poderíamos usar IFormCollection para capturarmos os campos do formulário.
         */

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]  //se for Post aqui, tem que ser Post também na função de chamada do json javascript, ou não entrará neste método (o mesmo para Get)!
        [Route("UsuarioSalvarSenha")]
        public async Task<JsonResult> UsuarioSalvarSenha(List<string> dados, List<string> dadosForm)    //, IFormCollection fc)
        {
            //Se fosse usar apenas o IFormCollection fc:
            //string loginUsuario = "Email".RetornaValorDoFormulario(fc);
            //string senhaUsuario = "SenhaUsuario".RetornaValorDoFormulario(fc);
            //string senhaRepete = "SenhaRepete".RetornaValorDoFormulario(fc);

            //array com chaves, pode ser tradado por "RetornaValorDoFormulario"
            string loginUsuario = "Email".RetornaValorDoFormulario(dados);
            string nomeCompleto = "NomeCompleto".RetornaValorDoFormulario(dados);

            //modelo Json com colchetes, vindo apenas uma string separada por vírgula pode ser tratado por "RetornaValorDoFormulario"
            string senhaUsuario = "SenhaUsuario".RetornaValorDoFormulario(dadosForm);
            string senhaRepete = "SenhaRepete".RetornaValorDoFormulario(dadosForm);
            bool boxGerarSenhaAutomatica = "BoxGerarSenhaAutomatica".RetornaValorDoFormulario(dadosForm).ConvertStringToBool();
            bool boxEnviarEmail = "BoxEnviarEmail".RetornaValorDoFormulario(dadosForm).ConvertStringToBool();

            string[] mensagem = new string[] { "Seja bem-vindo: " + loginUsuario }; //para poder cumprir o status da tela principal com o login do administrador

            string redirecionaUrl = "Senhas".MontaUrl(base.HttpContext.Request);

            if (senhaUsuario != senhaRepete)
            {
                LoggerFile.Write("ERRO: Alteração pelo Administrador falhou porque as senhas digitadas para o usuário não eram iguais: " + loginUsuario);
                return Json(new { titulo = "Atenção", mensagem = "Alteração falhou porque as senhas digitadas não são iguais", action = "", sucesso = false });
            }
            else if ((senhaUsuario == senhaRepete) && (string.IsNullOrEmpty(loginUsuario) || string.IsNullOrEmpty(senhaUsuario) || string.IsNullOrEmpty(senhaRepete)))
            {
                LoggerFile.Write("ERRO: A senha alterada pelo administrador não foi salva para o usuário: " + loginUsuario);
                return Json(new { titulo = "Ops", mensagem = "A senha alterada não foi salva para o usuário", action = redirecionaUrl, sucesso = false });
            }
            else
            {
                //O administrador vai alterar a senha do usuário cadastrado no sistema...
                Senhas senhas = await _db.Senhas.Where(s => s.LoginUsuario == loginUsuario).SingleAsync();
                if (senhas.Bloqueado == 1)
                {
                    LoggerFile.Write("ERRO: A senha alterada pelo administrador não foi salva para o usuário que está bloqueado: " + loginUsuario);
                    return Json(new { titulo = "Ops", mensagem = "Este usuário <b>" + loginUsuario + "</b> está bloqueado, não posso alterar a senha", action = redirecionaUrl, sucesso = false });
                }
                if (senhas.DataExpira.HasValue && senhas.DataExpira <= _geralController.ObterDataHoraServidor().ToFormataData()) //se a data de expiração for menor ou igual a data atual
                {
                    LoggerFile.Write("ERRO: A senha do usuário não foi alterada pelo administrador porque tem data expirada: " + loginUsuario);
                    return Json(new { titulo = "Ops", mensagem = "Este usuário <b>" + loginUsuario + "</b> está com data expirada, não posso alterar sua senha", action = redirecionaUrl, sucesso = false });
                }
                using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
                {
                    //Pega a nova senha definida pelo usuário e criptografa antes de salvar na tabela
                    senhas.SenhaUsuario = CriptoDecripto.Criptografa_StringToString(senhaUsuario);
                    if (_db.SaveChanges() < 1)
                    {
                        LoggerFile.Write("ERRO: A senha alterada não foi salva para o usuário: " + loginUsuario);
                    }
                    trans.Complete();
                }
                return Json(new { titulo = "Ok", mensagem = "Senha foi alterada para o usuário <b>" + loginUsuario + "</b><br />" + nomeCompleto, action = redirecionaUrl, sucesso = true });
            }
        }

        //[TypeFilter(typeof(SessionFilter))]
        //[HttpGet]
        //[Route("Senhas")]
        //public IActionResult Senhas()
        //{
        //    object[] colecao = { "Controle de Acesso" + Getbolinha + "Criar Usuário", true };
        //    ViewBag.TextoMenu = colecao;

        //    /* INICIO: Declarar botões para o Formulário
        //     *         type.......value.......id.......evento
        //     */
        //    Tuple<string, string, string, string>[] formBotoes =
        //                  {
        //                  Tuple.Create("submit", "Incluir", "1", "onclick=alert(this.value)"),
        //                  Tuple.Create("button", "Excluir", "2", "onclick=alert(this.value)"),
        //                  Tuple.Create("button", "Folhear", "3", "onclick=alert(this.value)"),
        //                  Tuple.Create("button", "Gerar novo token", "4", "onclick=alert(this.value)"),
        //                  Tuple.Create("button", "Enviar senha por Email", "5", "onclick=alert(this.value)")
        //                };
        //    var Botoes = UtilBLL.ConstroiBotoesFormulario(formBotoes, 2);
        //    ViewBag.ViewBotoes = Botoes;
        //    /* FIM: Declarar botões para o Formulário */

        //    var result = new GeralController();
        //    return result.Validacao("Senhas,Senhas", "Cadastro de Senhas");

        //    //return View();
        //}

        /* Código que adiciona usuários */

        [Authorize]
        public ActionResult Adiciona(object usu)
        {
            return RedirectToAction("Index", "Senhas");   /* "action", "controller" retornando ao "Index" de Senhas */
        }

        [Authorize]
        public IActionResult FormularioSenhas()
        {  /* EXEMPLO */
            string[] campos = {
                                "Nick do usuário",
                                "Nome completo",
                                "Setor"
                              };
            var resultado = new { ret = campos };
            return View(resultado);
        }

        [Authorize]
        public JsonResult RetornaRegistrosSenhas()
        {  /* EXEMPLO retorno de um Json */
            var resultado = new
            {
                Nick = "apelido",
                Nome = "nome completo"
            };
            return Json(resultado);
        }

        [Authorize]
        public JsonResult RetornaRegistrosSenhas2()
        {  /* EXEMPLO retorno de um Json */
            string Nick = "apelido";
            string Nome = "nome completo";

            var resultado = new
            {
                Nick,
                Nome
            };
            return Json(resultado);
        }

        /*  */

        public IActionResult ConfirmarAlterarSenha(vmSenhas objLogin)
        {
            if (string.IsNullOrEmpty(objLogin.LoginUsuario) || string.IsNullOrEmpty(objLogin.SenhaUsuario)) return View("Login");
            vmSenhas? validaLogin = _validacoesDeSenhas.RetornaLogin(objLogin.LoginUsuario, objLogin.SenhaUsuario);
            //Se validou o Login sem qualquer tipo de restrição (ou seja, nenhum bloqueio e o usuário está normalmente ativo)
            if (validaLogin != null && validaLogin.SituacaoLogin == (int)TipoSituacaoLogin.SemRestricao)
            {
                //Vamos alterar a senha do usuário com um hash
                Senhas senhas = _db.Senhas.Where(s => s.LoginUsuario == objLogin.LoginUsuario).Single();
                using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
                {
                    //Pega a nova senha definida pelo usuário e criptografa antes de salvar na tabela
                    senhas.SenhaUsuario = CriptoDecripto.Criptografa_StringToString(objLogin.SenhaUsuario);
                    if (_db.SaveChanges() < 1)
                    {
                        LoggerFile.Write("ERRO: O usuário não conseguiu alterar sua senha : " + objLogin.LoginUsuario);
                    }
                    trans.Complete();
                }
            }

            //Implementar o Envio do Email com a senha alterada...
            //var link = HttpContext.Request.Host.Value;
            //Email objEmail = new Email(_config);
            //objEmail.CliCodigo = objRed.Id; //Aqui poderia mudar a nomenclatura, já que não será mais codigo do cliente, e também já poderia passar criptografado.

            //objEmail.CliEmail = objLogin.CliEmail;
            //objEmail.link = link;
            //objEmail.EnviarEmail();
            return View("Login");
        }

        /* REFERENTE A LOGIN:
         * Usuário esqueceu a senha.
         * A senha é recriada e enviada por Email que consta na tabela de cadastro do usuário web) */

        public IActionResult ResetarSenha(vmSenhas objLogin)
        {
            ViewBag.TextoReset = "Foi enviado um Email para você com uma senha temporária.<br />" +
                                 "Verifique sua caixa de Emails e siga as instruções contidas nela.";

            /* Vamos verificar o Email do Reset de senha */
            if (!string.IsNullOrEmpty(objLogin.LoginUsuario) && !string.IsNullOrEmpty(objLogin.CPF))
            {
                vmSenhas? validaLogin = _validacoesDeSenhas.RetornaLogin(objLogin.LoginUsuario, objLogin.CPF.CPFSemFormatacao(), objLogin.DataNascimento);
                if (validaLogin?.RecuperacaoDeSenha == true &&
                    validaLogin.SituacaoLogin == (int)TipoSituacaoLogin.SemVerificacao &&
                    !string.IsNullOrEmpty(validaLogin.CPF) &&
                    !string.IsNullOrEmpty(validaLogin.LoginUsuario))
                {
                    Senhas senhas = _db.Senhas.Where(s => s.LoginUsuario == objLogin.LoginUsuario).Single();
                    using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
                    {
                        //Gera uma senha aleatória para o usuário, salva na tabela e envia por Email para ser validada
                        string senhaAleatoria = Criptografias.GeraSenhaAleatoria();
                        //senhas.SenhaUsuario = CriptoDecripto.Criptografa_StringToString(senhaAleatoria);
                        senhas.SenhaUsuario = senhaAleatoria;
                        senhas.EmailConfirmado = (int)TipoSituacaoLogin.SemVerificacao;
                        if (_db.SaveChanges() < 1)
                        {
                            LoggerFile.Write("RECUPERACAO DE LOGIN/ERRO: O usuário não conseguiu recuperar/alterar sua senha : " + objLogin.LoginUsuario);
                        }
                        else
                        {
                            LoggerFile.Write("RECUPERACAO DE LOGIN: usuário resetou a senha para recuperação do Login: " + objLogin.LoginUsuario);
                        }
                        trans.Complete();
                    }
                }
                else
                {
                    if (validaLogin?.Senhas != null && validaLogin.Senhas.EmailConfirmado == (int)TipoEmailConfirmado.Sim && validaLogin.Senhas.LoginUsuario == objLogin.LoginUsuario)
                        ViewBag.TextoReset = "Já foi enviado um Email para validação de login/senha. <br />" +
                                             "Por favor, confira sua caixa de emails (verifique lixeira e caixa de spams). <br />" +
                                             "Obrigado.";
                    else
                        ViewBag.TextoReset = "Sua senha não foi recuperada, porque os dados que você informou não conferem com o cadastro.";
                }
                ViewBag.TextoMenu = "Recuperação de senha".MensagemStartUp();
            }
            return View();
        }

        /* REFERENTE A LOGIN */

        public IActionResult SenhaEsquecida(vmLogin login)
        {
            ViewBag.TextoMenu = "Recuperação de senha".MensagemStartUp();

            return View();
        }

        /*
         * Salva uma imagem em bytes[] já pronta para ser exibida em outro momento
         */

        /// <summary>
        /// Salva uma imagem como array de bytes para exibição futura.
        /// </summary>
        private void GetImagemAssinatura(vmSenhas vm)
        {
            // Garante um caminho inicial válido
            if (NaoExistePath(vm.CaminhoImagemAssinatura))
                vm.CaminhoImagemAssinatura = GetLocalPathImagens();

            // Resolve o caminho real
            vm.CaminhoImagemAssinatura = _pathHelper.GetPathTrue(vm.CaminhoImagemAssinatura, vm.NomeAssinatura);

            // Verifica se os dados são válidos
            if (string.IsNullOrWhiteSpace(vm.CaminhoImagemAssinatura) || string.IsNullOrWhiteSpace(vm.NomeAssinatura))
                return;

            string caminhoCompleto = Path.Combine(vm.CaminhoImagemAssinatura, vm.NomeAssinatura);

            if (!System.IO.File.Exists(caminhoCompleto))
                return;

            // Lê o arquivo diretamente para array de bytes com using (garante fechamento)
            vm.Assinatura = System.IO.File.ReadAllBytes(caminhoCompleto);
        }
    } /* fim do controller */
}