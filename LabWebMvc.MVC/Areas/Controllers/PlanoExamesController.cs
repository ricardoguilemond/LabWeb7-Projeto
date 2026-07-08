using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static BLL.UtilBLL;
using static ExtensionsMethods.Genericos.Enumeradores;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class PlanoExamesController : Controller
    {
        private readonly Db _db;
        private readonly IValidadorDeSessao _validador;
        private readonly GeralController _geralController;
        private readonly IEventLogHelper _eventLogHelper;

        public PlanoExamesController(Db db, IValidadorDeSessao validador, GeralController geralController, IEventLogHelper eventLogHelper)
        {
            _db = db;
            _validador = validador;
            _geralController = geralController;
            _eventLogHelper = eventLogHelper;
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
            var vm = new vmPlanoExames { ItensExamePrincipal = numeroItemFolha };
            return PartialView("Partials/_PartialPlanoConta", vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("PlanoExames")]
        public async Task<IActionResult> Index(vmPlanoExames vm, int numeroItemFolha = 1, bool partial = false, string? Conteudo = "", int registros = 100)
        {
            MontaControllers("IncluirPlanoExames", "PlanoExames");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            //Troca dinâmica de folhas de exames
            var folhas = _db.ClasseExames.OrderBy(o => o.RefExame).ToList();
            vm.FolhaIdList = folhas.Select(l => new SelectListItem { Text = l.Id.ToString(), Value = l.Id.ToString() }).ToList();
            vm.FolhaNomeList = folhas.Select(l => new SelectListItem { Text = l.RefExame, Value = l.Id.ToString() }).ToList();
            //..

            /* 0000000 = não serão mostrados aqueles que são o header a Folha de Exames  */
            int totalTabela = _db.PlanoExames.Where(s => s.ContaExame != null && !s.ContaExame.EndsWith("0000000")).Count();
            string? descricaoFolha = _db.ClasseExames.Where(s => s.Id == numeroItemFolha).Single().RefExame ?? string.Empty;

            ICollection<PlanoExames> dados = await _db.PlanoExames
                .Where(s => !s.ContaExame.EndsWith("0000000") && s.TabelaExamesId == (int)IdPadrao.SUS && s.ExameId == numeroItemFolha)
                .OrderByDescending(o => o.Id)
                .Take(registros)
                .ToListAsync();

            int totalRegistros = dados.Count();

            //preenche com a parte obrigatória e ÚNICA/ESPECÍFICA DA INCLUSÃO da vm, com os valores de filtro para aparecer na IncluirPlanoExames.cshtml
            vm = new vmPlanoExames()
            {
                ExameId = numeroItemFolha,   //número da folha selecionada
                RefExame = descricaoFolha,    //descrição da folha selecionada
                ContaExame = totalRegistros == 0 ? Utils.Utils.RetornaCodigoFolhaExame(_db, numeroItemFolha) : dados.First().ContaExame,  //conta exame da folha selecionada
                FolhaIdList = _db.ClasseExames.Select(l => new SelectListItem { Text = l.Id.ToString(), Value = l.Id.ToString() }).ToList(),
                FolhaNomeList = _db.ClasseExames.Select(l => new SelectListItem { Text = l.RefExame, Value = l.Id.ToString() }).ToList()
            };

            TempData.Clear();
            TempData["Descricao"] = descricaoFolha;
            TempData["NumeroFolha"] = numeroItemFolha.ToString();
            TempData.Keep();

            //Feito pelo Kiro em 20/04/2026
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
                    PlanoExames = vm,
                    PartialView = "Partials/_PartialPlanoConta"
                };
                //Dados auxiliares em ViewBag para o GeralController
                ViewBag.TextoMenu = new object[] { "Tabela de Plano de Exames", false };
                ViewBag.TotalRegistros = totalRegistros.ToString();
                ViewBag.TotalTabela = totalTabela.ToString();
                ViewBag.ListaDados = dados;
                return PartialView("Partials/_PartialPlanoConta", vmResposta);
            }
            else
            {   //quando monta o grid pela primeira vez ou reconstrói tudo!
                var vmResposta = new vmListaValidacao<dynamic>
                {
                    RetornoDeRota = "Index",
                    Titulo = "Tabela de Plano de Exames",
                    TotalRegistros = totalRegistros,
                    TotalTabela = totalTabela,
                    ListaDados = dados.Cast<dynamic>().ToList(),
                    PlanoExames = vm
                };
                //Dados auxiliares em ViewBag para o _Layout
                ViewBag.TextoMenu = new object[] { "Tabela de Plano de Exames", false };
                ViewBag.TotalRegistros = totalRegistros.ToString();
                ViewBag.TotalTabela = totalTabela.ToString();
                ViewBag.ListaDados = dados;
                return View(vmResposta);
            }
            //..Kiro
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirPlanoExames")]
        //Feito pelo Kiro em 20/04/2026
        public IActionResult IncluirPlanoExames()
        {
            // Recupera o número da folha do TempData (definido no Index)
            int numeroFolha = 1;
            if (TempData.ContainsKey("NumeroFolha"))
            {
                int.TryParse(TempData["NumeroFolha"]?.ToString(), out numeroFolha);
                TempData.Keep();
            }

            // Carrega as contas principais da folha para os dropdowns
            var contasPrincipais = _db.PlanoExames
                .Where(p => p.ExameId == numeroFolha
                         && p.ContaExame.Substring(7, 4) == "0000"
                         && p.ContaExame.Substring(4, 3) != "000"
                         && p.TabelaExamesId == (int)IdPadrao.SUS)
                .OrderBy(o => o.ContaExame)
                .ToList();

            var vm = new vmPlanoExames
            {
                ExameId = numeroFolha,
                Item1 = contasPrincipais.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = c.Id.ToString(),
                    Value = c.Id.ToString()
                }).ToList(),
                Item2 = contasPrincipais.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = c.ContaExame,
                    Value = c.Id.ToString()
                }).ToList(),
                Item3 = contasPrincipais.Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = c.Descricao,
                    Value = c.Id.ToString()
                }).ToList()
            };

            ViewBag.TextoMenu = new object[] { "Cadastro de Plano de Exames", false };
            return View(vm);
        }
        //..Kiro

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
                List<TabelaExames> tabelaExames = await _db.TabelaExames.OrderBy(o => o.Id).ToListAsync();

                //Feito pelo Kiro em 20/04/2026
                Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            foreach (TabelaExames? tabela in tabelaExames)
                            {
                                await _db.PlanoExames.AddAsync(new PlanoExames()
                                {
                                    //Colunas NÃO nulas:
                                    ExameId = vm.ExameId,
                                    CitoInstituicao = vm.CitoInstituicao,
                                    CitoTituloExame = vm.CitoTituloExame,
                                    RefExame = vm.RefExame,
                                    RefItem = vm.RefItem,
                                    Descricao = vm.Descricao,
                                    TabelaExamesId = tabela.Id,
                                    ContaExame = vm.ContaExame,
                                    QCH = string.IsNullOrEmpty(vm.QCH.ToString()) ? 0 : vm.QCH,
                                    Etiqueta = string.IsNullOrEmpty(vm.Etiqueta.ToString()) ? 0 : vm.Etiqueta,
                                    Etiquetas = string.IsNullOrEmpty(vm.Etiquetas.ToString()) ? 0 : vm.Etiquetas,
                                    GraficoNoItem = DefinirFlagGrafico(vm),
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
                                    MapaHorizontal = string.IsNullOrEmpty(vm.MapaHorizontal) ? string.Empty : vm.MapaHorizontal.ToUpper(),
                                    ResultadoMinimo = vm.ResultadoMinimo,
                                    ResultadoMaximo = vm.ResultadoMaximo,
                                    LaboratorioExterno = vm.LaboratorioExterno,
                                    PrazoResultadoDias = string.IsNullOrEmpty(vm.PrazoResultadoDias.ToString()) ? 15 : vm.PrazoResultadoDias
                                });
                            }

                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Plano de Exames foi salvo", action = "", sucesso = true });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _eventLogHelper.LogEventViewer("[PlanoExames] Salvar - Erro: " + ex.Message, "wError");
                            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Plano de Exames NÃO foi salvo", action = "", sucesso = false });
                        }
                    }
                });
                //..Kiro
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[PlanoExames] Salvar - Erro geral: " + ex.Message, "wError");
            }
            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Plano de Exames NÃO foi salvo", action = "", sucesso = false });
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

                        vm.ModeloHtml = myText;  //leva o html montado para a view "ModeloPlanoExames.cshtml"
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
                vm.GraficoNoItem = planoExames.GraficoNoItem;
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
                vm.MapaHorizontal = planoExames.MapaHorizontal;    //Sinonímia SEMPRE maiúscula
                vm.ResultadoMinimo = planoExames.ResultadoMinimo;
                vm.ResultadoMaximo = planoExames.ResultadoMaximo;
                vm.LaboratorioExterno = planoExames.LaboratorioExterno;
                vm.PrazoResultadoDias = planoExames.PrazoResultadoDias; 

                ViewBag.TipoContaExame = planoExames.ContaExame.Substring(7, 4) == "0000" ? TipoContaExame.Principal : TipoContaExame.Item;
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[PlanoExames] Alterar - Erro: " + ex.Message, "wError");
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
                List<TabelaExames> tabelaExames = await _db.TabelaExames.OrderBy(o => o.Id).ToListAsync();

                //Feito pelo Kiro em 20/04/2026
                Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            foreach (TabelaExames? tabela in tabelaExames)
                            {
                                PlanoExames? plano = planoExames.Where(s => s.TabelaExamesId == tabela.Id).First();

                                plano.CitoInstituicao = string.IsNullOrEmpty(vm.CitoInstituicao.ToString()) ? 0 : vm.CitoInstituicao;
                                plano.CitoTituloExame = string.IsNullOrEmpty(vm.CitoTituloExame.ToString()) ? 0 : vm.CitoTituloExame;
                                plano.Descricao = contaExame.Substring(7, 4) == "0000" ? vm.Descricao.ToUpper() : vm.Descricao;
                                plano.Etiqueta = string.IsNullOrEmpty(vm.Etiqueta.ToString()) ? 0 : vm.Etiqueta;
                                plano.Etiquetas = string.IsNullOrEmpty(vm.Etiquetas.ToString()) ? 0 : vm.Etiquetas;
                                plano.GraficoNoItem = DefinirFlagGrafico(vm);
                                plano.Seleciona = string.IsNullOrEmpty(vm.Seleciona.ToString()) ? 0 : vm.Seleciona;
                                plano.NaoMostrar = string.IsNullOrEmpty(vm.NaoMostrar.ToString()) ? 0 : vm.NaoMostrar;
                                plano.PrazoResultadoDias = string.IsNullOrEmpty(vm.PrazoResultadoDias.ToString()) ? 15 : vm.PrazoResultadoDias;
                                plano.CitoTituloFolha = vm.CitoTituloFolha;
                                plano.CitoDescricao = vm.CitoDescricao;
                            }

                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Plano de Exames foi atualizado", action = "", sucesso = true });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _eventLogHelper.LogEventViewer("[PlanoExames] Alteração - Erro: " + ex.Message, "wError");
                            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Plano de Exames NÃO foi atualizado", action = "", sucesso = false });
                        }
                    }
                });
                //..Kiro
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[PlanoExames] Alteração - Erro geral: " + ex.Message, "wError");
            }
            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Plano de Exames NÃO foi atualizado", action = "", sucesso = false });
        }

        /* Atenção: a excusão com "ExecuteDeleteAsync" não pode ter um TransactionScope, porque ela fica executando async mas o método é imediatamente liberado  */

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirPlanoExames")]
        public async Task<IActionResult> ExcluirPlanoExames(int id)
        {
            // Busca o registro para identificar a ContaExame e TabelaExamesId
            PlanoExames? registro = await _db.PlanoExames.Where(x => x.Id == id).AsNoTracking().FirstOrDefaultAsync();
            if (registro == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi encontrado", action = "", sucesso = false });

            string contaExame = registro.ContaExame;

            //Feito pelo Kiro em 20/04/2026
            // Verifica se a ContaExame está sendo utilizada em exames realizados, AM ou requisições
            bool possuiVinculos = await _db.ItensExamesRealizados.AnyAsync(i => i.ContaExame == contaExame)
                               || await _db.ItensExamesRealizadosAM.AnyAsync(i => i.ContaExame == contaExame)
                               || await _db.Requisitar.AnyAsync(r => r.ContaExame == contaExame);

            // Se for conta principal (termina em 0000), verificar também os itens filhos
            if (!possuiVinculos && contaExame.Substring(7, 4) == "0000")
            {
                string prefixoConta = contaExame.Substring(0, 7);
                possuiVinculos = await _db.ItensExamesRealizados.AnyAsync(i => i.ContaExame.StartsWith(prefixoConta))
                              || await _db.ItensExamesRealizadosAM.AnyAsync(i => i.ContaExame.StartsWith(prefixoConta))
                              || await _db.Requisitar.AnyAsync(r => r.ContaExame.StartsWith(prefixoConta));
            }

            if (possuiVinculos)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Esta conta do Plano de Exames possui itens de exames realizados ou requisições vinculadas e não pode ser excluída", action = "", sucesso = false });
            //..Kiro

            // Exclusão
            bool erro = false;
            int exclusao = 0;

            try
            {
                if (contaExame.Substring(7, 4) == "0000")
                {
                    // Conta principal: exclui ela e todos os seus itens
                    string prefixoConta = contaExame.Substring(0, 7);
                    exclusao = await _db.PlanoExames.Where(d => d.ContaExame.StartsWith(prefixoConta) && d.ContaExame.Substring(5, 3) != "000").ExecuteDeleteAsync();
                }
                else
                {
                    // Item: exclui em todas as tabelas de exames
                    exclusao = await _db.PlanoExames.Where(d => d.ContaExame == contaExame).ExecuteDeleteAsync();
                }
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[PlanoExames] Excluir - Erro: " + ex.Message, "wError");
                erro = true;
            }

            if (erro || exclusao < 1)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi excluído", action = "", sucesso = false });

            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Registro foi excluído", action = "", sucesso = true });
        }

        private static int? DefinirFlagGrafico(vmPlanoExames vm)
        {
            return vm.GraficoNoItem == 1 ? 1 : null;
        }
    }
}