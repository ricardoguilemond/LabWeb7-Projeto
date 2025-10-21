using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using static BLL.UtilBLL;
using static ExtensionsMethods.Genericos.Enumeradores;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class PlanoExamesController : BaseController
    {
        public PlanoExamesController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem)
        {
        }

        private void MontaControllers(string action, string controller, string parametros = "")
        {
            PartialFiltro.Action = action;
            PartialFiltro.Controller = controller;
            PartialFiltro.ActionButton = action + parametros;
            PartialFiltro.ControllerButton = controller;
            PartialFiltro.Esconde = false;
            ViewBag.TextoMenu = action.MensagemStartUp();
        }

        [HttpGet]
        [Route("FiltraFolhaExame")]
        public IActionResult FiltraFolhaExame(int numeroItemFolha)
        {
            ViewBag.ItensExamePrincipal = numeroItemFolha;
            return PartialView("Partials/_PartialPlanoConta");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("PlanoExames")]
        public async Task<IActionResult> Index(vmPlanoExames vm, int numeroItemFolha = 1, bool partial = false, string? Conteudo = "", int registros = 100)
        {
            MontaControllers("IncluirPlanoExames", "PlanoExames");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            ICollection<PlanoExames> dados = [];
            int totalTabela = 0;
            string? descricaoFolha = string.Empty;

            /* 0000000 = não serão mostrados aqueles que são o header a Folha de Exames  */
            totalTabela = _db.PlanoExames.Where(s => s.ContaExame != null && !s.ContaExame.EndsWith("0000000")).AsNoTracking().Count();

            descricaoFolha = _db.ClasseExames.Where(s => s.Id == numeroItemFolha).Single().RefExame;
            descricaoFolha = !string.IsNullOrEmpty(descricaoFolha) ? descricaoFolha : string.Empty;

            dados = await _db.PlanoExames.Where(s => !s.ContaExame.EndsWith("0000000") && 
                                                s.TabelaExamesId == (int)IdPadrao.SUS && 
                                                s.ExameId == numeroItemFolha).OrderByDescending(o => o.Id).Take(registros).ToListAsync();

            int totalRegistros = dados.Count();




            var folhas = _db.ClasseExames.OrderBy(o => o.RefExame).ToList();


            //preenche com a parte obrigatória e ÚNICA/ESPECÍFICA DA INCLUSÃO da vm, com os valores de filtro para aparecer na IncluirPlanoExames.cshtml
            vm = new vmPlanoExames()
            {
                FolhaIdList = folhas.Select(l => new SelectListItem
                {
                    Text = l.Id.ToString(),
                    Value = l.Id.ToString()
                }).ToList(),

                FolhaNomeList = folhas.Select(l => new SelectListItem
                {
                    Text = l.RefExame,
                    Value = l.Id.ToString()
                }).ToList(),



                ExameId = numeroItemFolha,     //número da folha selecionada
                RefExame = descricaoFolha,     //descrição da folha selecionada
                ContaExame = totalRegistros == 0 ? Utils.Utils.RetornaCodigoFolhaExame(_db, numeroItemFolha) : dados.First().ContaExame  //conta exame da folha selecionada
            };

            TempData.Clear();
            TempData["Descricao"] = descricaoFolha;
            TempData["NumeroFolha"] = numeroItemFolha.ToString();
            TempData.Keep();

            //Finalização da View
            if (partial || (totalRegistros == 0 && string.IsNullOrEmpty(vm.Descricao)))
            {
                var vmResposta = new vmListaValidacao<dynamic>
                {   //quando ainda não houver dados da Folha no Plano de Exames ou for uma partialView
                    RetornoDeRota = "Index",
                    Titulo = "Tabela de Plano de Exames",
                    TotalRegistros = totalRegistros,
                    TotalTabela = totalTabela,
                    ListaDados = dados.Cast<dynamic>().ToList(),
                    PartialView = "Partials/_PartialPlanoConta"
                };
                return _geralController.ValidacaoGenerica(vmResposta);
            }
            else
            {   //quando monta o grid pela primeira vez ou reconstrói tudo!
                var vmResposta = new vmListaValidacao<dynamic>
                {
                    RetornoDeRota = "Index",
                    Titulo = "Tabela de Plano de Exames",
                    TotalRegistros = totalRegistros,
                    TotalTabela = totalTabela,
                    ListaDados = dados.Cast<dynamic>().ToList()
                };
                return _geralController.ValidacaoGenerica(vmResposta);
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirPlanoExames")]
        public IActionResult IncluirPlanoExames()
        {
            int itemFolha = Convert.ToInt32(TempData["NumeroFolha"]);

            var refItem = _db.PlanoExames
                .Where(l => !l.ContaExame.EndsWith("0000000") &&
                            l.ContaExame.EndsWith("0000") &&
                            l.ExameId == itemFolha &&
                            l.TabelaExamesId == (int)IdPadrao.SUS)
                .OrderBy(o => o.RefExame)
                .ToList();

            var vm = new vmPlanoExames
            {
                Item1 = refItem.Select(l => new SelectListItem { Text = l.Id.ToString(), Value = l.Id.ToString() }).ToList(),
                Item2 = refItem.Select(l => new SelectListItem { Text = l.ContaExame, Value = l.Id.ToString() }).ToList(),
                Item3 = refItem.Select(l => new SelectListItem { Text = l.Descricao, Value = l.Id.ToString() }).ToList(),
                // outros campos...
            };

            //Finalização da View
            return _geralController.Validacao("IncluirPlanoExames", "Cadastro de Plano de Exames");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirPlanoExames")]
        public async Task<IActionResult> SalvarPlanoExames(vmPlanoExames vm, int registroID)
        {
            string redirecionaUrl = "PlanoExames".MontaUrl(base.HttpContext.Request);

            string[] contaExame = new string[] { };

            //Importante: corrigindo possível falha de lançamento, caso o usuário tente lançar uma conta principal com controle de conta item (javascript pode ter sido burlado)
            if (vm.TipoContaExame == (int)TipoContaExame.Item && (registroID == vm.ExameId)) registroID = 0;

            /*
             * Bloco de preparação dos dados antes da gravação
             */
            PlanoExames? planoExamesConta = await _db.PlanoExames.Where(x => x.Id == registroID).FirstOrDefaultAsync();

            if (vm.TipoContaExame == (int)TipoContaExame.Principal && registroID == 0 && planoExamesConta == null)  //Está chegando então conta principal para ser incluída
            {
                //última conta exame existente no plano na mesma folha (ExameId = Folha)
                planoExamesConta = await _db.PlanoExames.Where(x => x.ExameId == vm.ExameId && x.ContaExame.EndsWith("0000")).OrderByDescending(o => o.ContaExame).FirstOrDefaultAsync();
            }

            if (planoExamesConta == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "O sistema não conseguiu identificar internamente a conta necessária" });

            vm.ExameId = planoExamesConta.ExameId;
            vm.ContaExame = planoExamesConta.ContaExame.Substring(0, 7) + "0000";

            contaExame = (vm.TipoContaExame == (int)TipoContaExame.Principal) ? Utils.Utils.SequenciadorContaPrincipal(_db, vm.ExameId) : Utils.Utils.SequenciadorContaItem(_db, vm.ExameId, vm.ContaExame.ToULong());
            if (contaExame[0] == "ERRO")
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "O sistema não conseguiu gerar o código da conta principal" });

            if (vm.TipoContaExame == (int)TipoContaExame.Principal)
            {
                vm.ContaExame = contaExame[0];    //conta principal completa!
                vm.RefExame = contaExame[1];
                vm.RefItem = contaExame[2];
                vm.Descricao = vm.Descricao.ToUpper();
            }
            else
            {   //conta item
                ///string[] ret = Utils.RetornaDescricaoConta(vm.ExameId, vm.ContaExame.Substring(4, 3).ToInt32());
                vm.ContaExame = contaExame[0];    //conta item completa!
                vm.RefExame = contaExame[1];      // ret[1];
                vm.RefItem = contaExame[2];       // vm.RefExame;
                vm.Descricao = vm.Descricao;      //fica do jeito que foi digitado pelo usuário (nem upper nem lower).
            }

            /*
             * Bloco da gravação dos dados do registro
             */

            PlanoExames? PlanoExames = await _db.PlanoExames.Where(s => s.ContaExame == vm.ContaExame && s.TabelaExamesId == (int)IdPadrao.SUS).SingleOrDefaultAsync();
            if (PlanoExames != null)
            {
                if (PlanoExames.ContaExame == vm.ContaExame)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = string.Format("{0}{1}", "Esta conta já existe no Plano de Exames: ", vm.ContaExame), action = "", sucesso = false });
            }
            //Cria a conta igual para todas as instituições existentes, como modelo do SUS.
            try
            {
                List<TabelaExames> tabelaExames = await _db.TabelaExames.OrderBy(o => o.Id).ToListAsync();  //Todos os nomes das tabelas das instituições existentes (tabelas de plano).
                                                                                                            //Cadastrar o plano de exame para cada uma das instituições de plano, sendo o SUS um padrão (SUS é uma conta espelho)!
                using (TransactionScope trans = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
                {
                    foreach (TabelaExames? tabela in tabelaExames)
                    {
                        await _db.PlanoExames.AddAsync(new PlanoExames()
                        {
                            //Colunas NÃO nulas:
                            ExameId = vm.ExameId,  //relativo a Folha
                            CitoInstituicao = vm.CitoInstituicao,  //tem default 0 (não nulo)  na tabela
                            CitoTituloExame = vm.CitoTituloExame,  //tem default 0 (não nulo)  na tabela
                            RefExame = vm.RefExame,
                            RefItem = vm.RefItem,
                            Descricao = vm.Descricao,
                            TabelaExamesId = tabela.Id,  //relativo a instituição do plano, sendo 1 = SUS = padrão = espelho.
                            ContaExame = vm.ContaExame,
                            QCH = string.IsNullOrEmpty(vm.QCH.ToString()) ? 0 : vm.QCH,
                            Etiqueta = string.IsNullOrEmpty(vm.Etiqueta.ToString()) ? 0 : vm.Etiqueta,
                            Etiquetas = string.IsNullOrEmpty(vm.Etiquetas.ToString()) ? 0 : vm.Etiquetas,
                            AlinhaLaudo = string.IsNullOrEmpty(vm.AlinhaLaudo.ToString()) ? 0 : vm.AlinhaLaudo,
                            Seleciona = string.IsNullOrEmpty(vm.Seleciona.ToString()) ? 0 : vm.Seleciona,
                            NaoMostrar = string.IsNullOrEmpty(vm.NaoMostrar.ToString()) ? 0 : vm.NaoMostrar,

                            //Aceitam nulo
                            CitoTituloFolha = vm.CitoTituloFolha,
                            CitoDescricao = vm.CitoDescricao,
                            CitoParteDescricao = vm.CitoParteDescricao,
                            TABELACH = vm.TABELACH,
                            ICH = vm.ICH,
                            UnidadeMedida = vm.UnidadeMedida,
                            Referencia = vm.Referencia,
                            Laudo = vm.Laudo,
                            MapaHorizontal = string.IsNullOrEmpty(vm.MapaHorizontal) ? string.Empty : vm.MapaHorizontal.ToUpper(),    //Sinonímia SEMPRE maiúscula
                            ResultadoMinimo = vm.ResultadoMinimo,
                            ResultadoMaximo = vm.ResultadoMaximo,
                            LaboratorioExterno = vm.LaboratorioExterno,
                            PrazoResultadoDias = string.IsNullOrEmpty(vm.PrazoResultadoDias.ToString()) ? 15 : vm.PrazoResultadoDias   //prazo de 15 dias para segurança
                        });
                    }
                    if (_db.SaveChanges() <= 0)
                    {
                        LoggerFile.Write("ERRO: Plano de Exames não foi salvo: " + vm.ContaExame.ToString());
                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = MensagensError_pt_BR.ErroSemDadoAtualizado, action = "", sucesso = false });
                    }
                    trans.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                //LoggerFile.Write("TransactionAbortedException Message: {0}", ex.Message);
                _eventLogHelper.LogEventViewer("[PlanoExames] Salvar - TransactionAbortedException Message: " + ex.Message, "wError");
            }
            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Plano de Exames foi salvo", action = "", sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModeloPlanoExames")]
        public async Task<IActionResult> ModeloPlanoExames(int registroID, vmPlanoExames vm)
        {
            if (registroID > 0)
            {
                try
                {
                    PlanoExames? modelo = await _db.PlanoExames.Where(x => x.Id == registroID).FirstOrDefaultAsync();
                    if (modelo != null)
                    {
                        string folhaModelo = modelo.ContaExame.Substring(0, 4) + "0000000";   //para pegar também o registro referente a FOLHA.
                        string contaModelo = modelo.ContaExame.Substring(0, 7);
                        List<PlanoExames> lista = await _db.PlanoExames.Where(x => (x.ContaExame.StartsWith(contaModelo) || x.ContaExame.StartsWith(folhaModelo)) && x.TabelaExamesId == (int)IdPadrao.SUS).AsNoTracking().OrderBy(o => o.ContaExame).ToListAsync();

                        //Vamos primeiro, colocar a lista com os campos que queremos na memória (Stream) como um TEXTO.
                        MemoryStream stream = new MemoryStream();
                        StreamWriter writer = new StreamWriter(stream);

                        /* formata o texto html da view */
                        writer.Write("<style>p { ");
                        writer.Write("          display: block;");
                        writer.Write("          -webkit-margin-before: 1em;");
                        writer.Write("          -webkit-margin-after: 1em;");
                        writer.Write("          -webkit-margin-start: 0px;");
                        writer.Write("          -webkit-margin-end: 0px;");
                        writer.Write("         } ");
                        writer.Write("       p { margin: 0; margin-bottom: 0.8em; width: 500px; }");
                        writer.Write("</style>");

                        writer.Write("<div style='font: normal 12px arial, sans-serif; line-height: 0.9;'>");

                        /* monta o texto html da view */
                        foreach (PlanoExames? item in lista)
                        {
                            if (item.ContaExame.Substring(4, 7) == "0000000")
                            {
                                if (item.Id == registroID) writer.Write("<strong>");
                                writer.Write("<p style='margin-left: 0px;'>" + item.ContaExame.FormatarContaExameSem11() + "&nbsp;&nbsp;" + item.Descricao + " <small style='color: gray;'>(nome da Folha de Exames)</small>" + "</p>");
                                if (item.Id == registroID) writer.Write("</strong>");
                            }
                            else if ((item.ContaExame.Substring(7, 4) == "0000") && (Convert.ToInt32(item.ContaExame.Substring(4, 3)) > 0))
                            {
                                if (item.Id == registroID) writer.Write("<strong style='color: blue;'>");
                                writer.Write("<p style='margin-left: 30px;'>" + item.ContaExame.FormatarContaExameSem11() + "&nbsp;&nbsp;" + item.Descricao + " <small style='color: gray;'>(conta principal)</small>" + "</p>");
                                if (item.Id == registroID) writer.Write("</strong>");
                            }
                            else
                            {
                                if (item.Id == registroID) writer.Write("<strong style='color: blue;'>");
                                writer.Write("<p style='margin-left: 60px;'>" + item.ContaExame.FormatarContaExameSem11() + "&nbsp;&nbsp;" + item.Descricao + "</p>");
                                if (item.Id == registroID) writer.Write("</strong>");
                            }
                        }

                        writer.Write("</div>");

                        writer.Flush();

                        // convert stream to string
                        stream.Position = 0;
                        StreamReader reader = new StreamReader(stream);
                        string myText = reader.ReadToEnd();  //meu texto pronto com os delimitadores

                        ViewBag.Modelo = myText;  //leva o html montado para a view "ModeloPlanoExames.cshtml"
                    }
                }
                catch (Exception ex)
                {
                    //LoggerFile.Write("Erro ao gerar PDF do Plano - Message: {0}", ex.Message);
                    _eventLogHelper.LogEventViewer("[PlanoExames] Erro ao gerar PDF do Plano - Message: " + ex.Message, "wError");
                }
                finally { }
            }

            //Finalização para a View
            ViewBag.TextoMenu = new object[] { "Modelo Formatado do Plano de Exames", false };
            _geralController.Validacao("ModeloPlanoExames,PlanoExames", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarPlanoExames")]
        public async Task<ActionResult> ConsultarPlanoExames(vmPlanoExames vm, int id)
        {
            PlanoExames dados = await _db.PlanoExames.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.ExameId = dados.ExameId;
                vm.ContaExame = dados.ContaExame.FormatarContaExameSem11();
                vm.TabelaExamesId = dados.TabelaExamesId;
                vm.RefExame = dados.RefExame;
                vm.RefItem = dados.RefItem;
                vm.Descricao = dados.Descricao;
                vm.UnidadeMedida = dados.UnidadeMedida;

                ///TODO/// PRECISA COMPLEMENTAR COM OS DADOS DOS PREÇOS QUANDO TIVER PRONTA A TABELA
                ///
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Consulta Conta no Plano de Exames", false };
            //Finalização para a View
            _geralController.Validacao("ConsultarPlanoExames,PlanoExames", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarPlanoExames")]
        public async Task<IActionResult> AlterarPlanoExames(vmPlanoExames vm, int id)
        {
            /*
             * Carrega o registro a ser alterado
             */
            PlanoExames? planoExames = await _db.PlanoExames.Where(x => x.Id == id).AsNoTracking().FirstOrDefaultAsync();   //É uma lista que só vai trazer um único registro por enquanto.
            if (planoExames == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "O sistema não conseguiu identificar o registro do plano para a alteração" });

            try
            {
                //Não aceitam nulos
                vm.CitoInstituicao = planoExames.CitoInstituicao;  //tem default 0 (não nulo)  na tabela
                vm.CitoTituloExame = planoExames.CitoTituloExame;  //tem default 0 (não nulo)  na tabela
                                                                   //planoExames.RefExame = planoExames.RefExame.ToUpper();    //nome da folha não vamos correr o risco de alterar
                vm.RefItem = planoExames.RefItem;                  //principal
                vm.Descricao = planoExames.Descricao;              //item
                vm.QCH = planoExames.QCH;
                vm.Etiqueta = planoExames.Etiqueta;
                vm.Etiquetas = planoExames.Etiquetas;
                vm.AlinhaLaudo = planoExames.AlinhaLaudo;
                vm.Seleciona = planoExames.Seleciona;
                vm.NaoMostrar = planoExames.NaoMostrar;

                //Aceitam nulo
                vm.CitoTituloFolha = planoExames.CitoTituloFolha;
                vm.CitoDescricao = planoExames.CitoDescricao;
                vm.CitoParteDescricao = planoExames.CitoParteDescricao;
                vm.TABELACH = planoExames.TABELACH;
                vm.ICH = planoExames.ICH;
                vm.UnidadeMedida = planoExames.UnidadeMedida;
                vm.Referencia = planoExames.Referencia;
                vm.Laudo = planoExames.Laudo;
                vm.MapaHorizontal = planoExames.MapaHorizontal;    //Sinonímia SEMPRE maiúscula
                vm.ResultadoMinimo = planoExames.ResultadoMinimo;
                vm.ResultadoMaximo = planoExames.ResultadoMaximo;
                vm.LaboratorioExterno = planoExames.LaboratorioExterno;
                vm.PrazoResultadoDias = planoExames.PrazoResultadoDias; 

                ViewBag.TipoContaExame = planoExames.ContaExame.Substring(7, 4) == "0000" ? TipoContaExame.Principal : TipoContaExame.Item;
            }
            catch (TransactionAbortedException ex)
            {
                //LoggerFile.Write("TransactionAbortedException Message: {0}", ex.Message);
                _eventLogHelper.LogEventViewer("[PlanoExames] Alterar - TransactionAbortedException Message: " + ex.Message, "wError");
            }

            TempData.Clear();
            TempData["Descricao"] = planoExames.Descricao;
            TempData["NumeroFolha"] = planoExames.ExameId.ToString();
            TempData.Keep();

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Alterar Cadastro do Plano de Exames", false };
            //Finalização da View
            _geralController.Validacao("AlterarPlanoExames", ViewBag.TextoMenu[0]);
            return View(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarPlanoExames")]
        public async Task<IActionResult> SalvarAlteracaoPlanoExames(vmPlanoExames vm, int id)
        {
            string redirecionaUrl = "PlanoExames".MontaUrl(base.HttpContext.Request);

            /*
             * Bloco de preparação dos dados antes da gravação
             * OBS: o número da conta não pode ser alterado, somente excluído quando não estiver ainda sendo utilizado em exames
             */
            ICollection<PlanoExames> planoExames = await _db.PlanoExames.Where(x => x.Id == id).AsNoTracking().ToListAsync(); //É uma lista que só vai trazer um único registro por enquanto.
            if (planoExames == null || planoExames.Count == 0)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "O sistema não conseguiu identificar o registro do plano para a alteração" });

            /*
             * Bloco da gravação dos dados do registro
             */
            string contaExame = planoExames.First().ContaExame;  //conta exame a ser alterada em todos os planos das instituições!
                                                                 //Refaz a lista agora pelo ContaExame
            planoExames = await _db.PlanoExames.Where(s => s.ContaExame == contaExame).ToListAsync();

            //Altera os registros igualmente para todas as instituições existentes, pelo modelo que veio alterado!
            try
            {
                List<TabelaExames> tabelaExames = await _db.TabelaExames.OrderBy(o => o.Id).ToListAsync();  //Todos os nomes das tabelas das instituições existentes (tabelas de plano).
                                                                                                            //Alterar a conta em todas as instituições (inclusive o próprio SUS)
                using (TransactionScope trans = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
                {
                    foreach (TabelaExames? tabela in tabelaExames)
                    {
                        PlanoExames? plano = planoExames.Where(s => s.TabelaExamesId == tabela.Id).First();

                        //Não aceitam nulos
                        plano.CitoInstituicao = string.IsNullOrEmpty(vm.CitoInstituicao.ToString()) ? 0 : vm.CitoInstituicao;  //tem default 0 (não nulo)  na tabela
                        plano.CitoTituloExame = string.IsNullOrEmpty(vm.CitoTituloExame.ToString()) ? 0 : vm.CitoTituloExame;  //tem default 0 (não nulo)  na tabela
                        //plano.RefExame = vm.RefExame.ToUpper();
                        //plano.RefItem = vm.RefItem;

                        plano.Descricao = contaExame.Substring(7, 4) == "0000" ? vm.Descricao.ToUpper() : vm.Descricao;

                        //plano.QCH = string.IsNullOrEmpty(vm.QCH.ToString()) ? 0 : vm.QCH;
                        plano.Etiqueta = string.IsNullOrEmpty(vm.Etiqueta.ToString()) ? 0 : vm.Etiqueta;
                        plano.Etiquetas = string.IsNullOrEmpty(vm.Etiquetas.ToString()) ? 0 : vm.Etiquetas;
                        plano.AlinhaLaudo = string.IsNullOrEmpty(vm.AlinhaLaudo.ToString()) ? 0 : vm.AlinhaLaudo;
                        plano.Seleciona = string.IsNullOrEmpty(vm.Seleciona.ToString()) ? 0 : vm.Seleciona;
                        plano.NaoMostrar = string.IsNullOrEmpty(vm.NaoMostrar.ToString()) ? 0 : vm.NaoMostrar;
                        plano.PrazoResultadoDias = string.IsNullOrEmpty(vm.PrazoResultadoDias.ToString()) ? 15 : vm.PrazoResultadoDias;   //prazo de 15 dias para segurança

                        //Aceitam nulo
                        plano.CitoTituloFolha = vm.CitoTituloFolha;
                        plano.CitoDescricao = vm.CitoDescricao;
                        //plano.CitoParteDescricao = vm.CitoParteDescricao;
                        //plano.TABELACH = vm.TABELACH;
                        //plano.ICH = vm.ICH;
                        //plano.UnidadeMedida = vm.UnidadeMedida;
                        //plano.Referencia = vm.Referencia;
                        //plano.Laudo = vm.Laudo;
                        //plano.MapaHorizontal = string.IsNullOrEmpty(vm.MapaHorizontal) ? string.Empty : vm.MapaHorizontal.ToUpper();    //Sinonímia SEMPRE maiúscula
                        //plano.ResultadoMinimo = vm.ResultadoMinimo;
                        //plano.ResultadoMaximo = vm.ResultadoMaximo;
                        //plano.LaboratorioExterno = vm.LaboratorioExterno;
                    }
                    if (_db.SaveChanges() <= 0)
                    {
                        _eventLogHelper.LogEventViewer("[PlanoExames] SalvarAlteracao - Plano de Exames não foi salvo: " + vm.ContaExame.ToString(), "wError"); 
                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = MensagensError_pt_BR.ErroSemDadoAtualizado, action = "", sucesso = false });
                    }
                    trans.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                //LoggerFile.Write("TransactionAbortedException Message: {0}", ex.Message);
                _eventLogHelper.LogEventViewer("[PlanoExames] Alteracao - TransactionAbortedException Message: " + ex.Message, "wError");
            }
            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Plano de Exames foi atualizado", action = "", sucesso = true });
        }

        /* Atenção: a excusão com "ExecuteDeleteAsync" não pode ter um TransactionScope, porque ela fica executando async mas o método é imediatamente liberado  */

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirPlanoExames")]
        public async Task<IActionResult> ExcluirPlanoExames(int id)
        {
            bool erro = false;
            int exclusao = 0;  //change delete multiple records

            ///TODO/// PRECISA BLOQUEAR A DELEÇÃO QUANDO A CONTA JÁ ESTIVER SENDO UTILIZADA EM ALGUM PACIENTE.

            try
            {
                PlanoExames? registro = await _db.PlanoExames.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (registro != null && registro.Id == id)
                {
                    string contaExame = registro.ContaExame;

                    if (contaExame.Substring(7, 4) == "0000")
                    {
                        //Significa que é uma conta principal, vai ter que excluir ela e todos os seus itens, para que não fiquem órfãos!
                        contaExame = contaExame.Substring(0, 7);
                        var registros = await _db.PlanoExames
                            .Where(d => d.ContaExame.StartsWith(contaExame.Substring(0, 7)) && !d.ContaExame.Substring(5, 3).Equals("000"))
                            .ToListAsync();

                        _db.PlanoExames.RemoveRange(registros);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        //método deleção em massa a partir do Core 7 = ExecuteDelete()
                        //exclusao = await _db.PlanoExames.Where(d => d.ContaExame == contaExame).ExecuteDeleteAsync();
                        var registros = await _db.PlanoExames.Where(d => d.ContaExame == contaExame).ToListAsync();

                        _db.PlanoExames.RemoveRange(registros);
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch
            {
                erro = true;
            }
            finally
            { }
            if (erro || exclusao < 1)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi excluído", action = "", sucesso = false });

            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Registro foi excluído", action = "", sucesso = true });
        }
    }
}