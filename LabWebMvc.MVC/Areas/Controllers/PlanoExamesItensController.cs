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
    public class PlanoExamesItensController : Controller
    {
        private readonly Db _db;
        private readonly IValidadorDeSessao _validador;
        private readonly GeralController _geralController;
        private readonly IEventLogHelper _eventLog;

        public PlanoExamesItensController(Db db, IValidadorDeSessao validador, GeralController geralController, IEventLogHelper eventLogHelper)
        {
            _db = db;
            _validador = validador;
            _geralController = geralController;
            _eventLog = eventLogHelper;
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

        private void CalculaLucroVariante(ICollection<PlanoExames> dados)
        {
            decimal totalCustoVariante = dados.Sum(s => s.ValorCusto).GetValueOrDefault();
            decimal totalItemVariante = dados.Sum(s => s.ValorItem).GetValueOrDefault(); ;
            string totalLucroVariante = Convert.ToString(UtilsMath.CalcLucroVarianteDec(totalCustoVariante, totalItemVariante)) + " %";

            TempData.Clear();

            TempData["TotalCustoVariante"] = totalCustoVariante.ToString("N4");
            TempData["TotalItemVariante"] = totalItemVariante.ToString("N4");
            TempData["TotalLucroVariante"] = totalLucroVariante;

            TempData["Summary"] = "<tr class='summary'>" +
                                  "<th colspan='5' style='text-align:left'><i style='color: #646464;'>Sumário da margem de lucro >>> </i></th>" +
                                  "<th class='summary_item'>R$ " + totalCustoVariante.ToString("N4") + "</th>" +
                                  "<th class='summary_item'>R$ " + totalItemVariante.ToString("N4") + "</th>" +
                                  "<th class='summary_item'><i style='color: #646464;'>Lucro Variante Total:</i>&nbsp;" + totalLucroVariante +
                                  "<a href='#' title='(valor cobrar * 100 / valor custo) - 100'>&nbsp;&nbsp;[Ver Fórmula]</a></th>" +
                                  "<th></th>" +
                                  "</tr>";

            TempData.Keep();  //para manter o TempData ativo até o término da sessão
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("PlanoExamesItens")]
        public async Task<IActionResult> Index(vmPlanoExames vm, int numeroItemFolha = 1, int numeroTabela = 1, bool partial = false, string? Conteudo = "", int registros = 100)
        {
            MontaControllers("IncluirPlanoExamesItens", "PlanoExamesItens");
            Conteudo = Conteudo?.Trim() ?? string.Empty;

            // Carrega listas de folhas e tabelas
            var folhas = _db.ClasseExames.OrderBy(o => o.RefExame).AsNoTracking().ToList();
            var tabelas = _db.TabelaExames.OrderBy(o => o.NomeTabela).AsNoTracking().ToList();

            // Preenche o ViewModel base
            vm = new vmPlanoExames
            {
                ExameId = numeroItemFolha,
                TabelaExamesId = numeroTabela,
                FolhaIdList = folhas.Select(f => new SelectListItem { Text = f.Id.ToString(), Value = f.Id.ToString() }).ToList(),
                FolhaNomeList = folhas.Select(f => new SelectListItem { Text = f.RefExame, Value = f.Id.ToString() }).ToList(),
                TabelaIdList = tabelas.Select(t => new SelectListItem { Text = t.Id.ToString(), Value = t.Id.ToString() }).ToList(),
                TabelaNomeList = tabelas.Select(t => new SelectListItem { Text = t.NomeTabela, Value = t.Id.ToString() }).ToList()
            };

            // Carrega os dados da tabela
            var dados = await _db.PlanoExames
                .Where(s => !s.ContaExame.EndsWith("0000000") && s.ExameId == numeroItemFolha && s.TabelaExamesId == numeroTabela)
                .OrderByDescending(o => o.Id)
                .Take(registros)
                .AsNoTracking()
                .ToListAsync();

            int totalTabela = await _db.PlanoExames
                .Where(s => !s.ContaExame.EndsWith("0000000"))
                .AsNoTracking()
                .CountAsync();

            int totalRegistros = dados.Count;

            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = dados;

            // Calcula lucro variante
            CalculaLucroVariante(dados);

            //Feito pelo Kiro em 20/04/2026
            // Prepara resposta
            var vmResposta = new vmListaValidacao<dynamic>
            {
                RetornoDeRota = "Index",
                Titulo = "Tabela de Preços dos Itens do Plano de Exames",
                TotalRegistros = totalRegistros,
                TotalTabela = totalTabela,
                ListaDados = dados.Cast<dynamic>().ToList(),
                PlanoExames = vm,
                PartialView = partial || (totalRegistros == 0 && string.IsNullOrEmpty(vm.Descricao))
                    ? "Partials/_PartialPlanoContaItem"
                    : null
            };

            //Dados auxiliares em ViewBag para o _Layout
            ViewBag.TextoMenu = new object[] { "Tabela de Preços dos Itens do Plano de Exames", false };

            if (!string.IsNullOrEmpty(vmResposta.PartialView))
                return PartialView(vmResposta.PartialView, vmResposta);
            else
                return View(vmResposta);
            //..Kiro
        }


        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModeloPlanoExamesItens")]
        /*
         * Carrega uma view do modelo formatado do plano de exames
         * */
        public async Task<IActionResult> ModeloPlanoExamesItens(int registroID, vmPlanoExames vm)
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
                        MemoryStream stream = new();
                        StreamWriter writer = new(stream);

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
                            string? vCusto = item.ValorCusto?.ToString("F");
                            string? vItem = item.ValorItem?.ToString("F");

                            if (item.ContaExame.Substring(4, 7) == "0000000")
                            {
                                if (item.Id == registroID) writer.Write("<strong>");
                                writer.Write("<p style='margin-left: 0px;'>" +
                                    item.ContaExame.FormatarContaExameSem11() + "&nbsp;&nbsp;" +
                                    item.Descricao + " <small style='color: gray;'>(nome da Folha de Exames)</small>" + "&nbsp;&nbsp;&nbsp;&nbsp; { " +
                                    "C:" + vCusto + "&nbsp;&nbsp;, " +
                                    "P:" + vItem + "&nbsp;&nbsp;" +
                                    "}</p>");
                                if (item.Id == registroID) writer.Write("</strong>");
                            }
                            else if ((item.ContaExame.Substring(7, 4) == "0000") && (Convert.ToInt32(item.ContaExame.Substring(4, 3)) > 0))
                            {
                                if (item.Id == registroID) writer.Write("<strong style='color: blue;'>");
                                writer.Write("<p style='margin-left: 30px;'>" +
                                    item.ContaExame.FormatarContaExameSem11() + "&nbsp;&nbsp;" +
                                    item.Descricao + " <small style='color: gray;'>(conta principal)</small>" + "&nbsp;&nbsp;&nbsp;&nbsp; { " +
                                    "C:" + vCusto + "&nbsp;&nbsp;, " +
                                    "P:" + vItem + "&nbsp;&nbsp;" +
                                    "}</p>");
                                if (item.Id == registroID) writer.Write("</strong>");
                            }
                            else
                            {
                                if (item.Id == registroID) writer.Write("<strong style='color: blue;'>");
                                writer.Write("<p style='margin-left: 60px;'>" +
                                    item.ContaExame.FormatarContaExameSem11() + "&nbsp;&nbsp;" +
                                    item.Descricao + "&nbsp;&nbsp;&nbsp;&nbsp; { " +
                                    "C:" + vCusto + "&nbsp;&nbsp;, " +
                                    "P:" + vItem + "&nbsp;&nbsp;" +
                                    "}</p>");
                                if (item.Id == registroID) writer.Write("</strong>");
                            }
                        }

                        writer.Write("</div>");

                        writer.Flush();

                        // convert stream to string
                        stream.Position = 0;
                        StreamReader reader = new(stream);
                        string myText = reader.ReadToEnd();  //meu texto pronto com os delimitadores

                        vm.ModeloHtml = myText;  //leva o html montado para a view "ModeloPlanoExamesItens.cshtml"
                    }
                }
                catch (Exception ex)
                {
                    //LoggerFile.Write("Erro ao gerar PDF do Plano - Message: {0}", ex.Message);
                    _eventLog.LogEventViewer("[PlanoExamesItens] Erro ao gerar PDF do Plano - Message: " + ex.Message, "wError");
                }
                finally { }
            }

            //Finalização para a View
            ViewBag.TextoMenu = new object[] { "Modelo Formatado dos Itens do Plano de Exames", false };
            _geralController.Validacao("ModeloPlanoExamesItens,PlanoExamesItens", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarPlanoExamesItens")]
        public async Task<ActionResult> ConsultarPlanoExamesItens(vmPlanoExames vm, int id)
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
            ViewBag.TextoMenu = new object[] { "Consulta Item no Plano de Exames", false };
            //Finalização para a View
            _geralController.Validacao("ConsultarPlanoExames,PlanoExames", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarPlanoExamesItens")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> AlterarPlanoExamesItens(vmPlanoExames vm, int id, string? valorCusto = null, string? valorItem = null)
        {
            PlanoExames? planoExames = await _db.PlanoExames.Where(x => x.Id == id).AsNoTracking().FirstOrDefaultAsync();
            if (planoExames == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "O sistema não conseguiu identificar o registro do plano para a alteração" });

            try
            {
                vm.CitoInstituicao = planoExames.CitoInstituicao;
                vm.CitoTituloExame = planoExames.CitoTituloExame;
                vm.RefItem = planoExames.RefItem;
                vm.Descricao = planoExames.Descricao;
                vm.QCH = planoExames.QCH;
                vm.Etiqueta = planoExames.Etiqueta;
                vm.Etiquetas = planoExames.Etiquetas;
                vm.AlinhaLaudo = planoExames.AlinhaLaudo;
                vm.Seleciona = planoExames.Seleciona;
                vm.NaoMostrar = planoExames.NaoMostrar;
                vm.CitoTituloFolha = planoExames.CitoTituloFolha;
                vm.CitoDescricao = planoExames.CitoDescricao;
                vm.CitoParteDescricao = planoExames.CitoParteDescricao;
                vm.TABELACH = planoExames.TABELACH;
                vm.ICH = planoExames.ICH;
                vm.UnidadeMedida = planoExames.UnidadeMedida;
                vm.Referencia = planoExames.Referencia;
                vm.Laudo = planoExames.Laudo;
                vm.MapaHorizontal = planoExames.MapaHorizontal;
                vm.ResultadoMinimo = planoExames.ResultadoMinimo;
                vm.ResultadoMaximo = planoExames.ResultadoMaximo;
                vm.LaboratorioExterno = planoExames.LaboratorioExterno;

                // Prioriza valores digitados no grid sobre os do banco
                if (!string.IsNullOrEmpty(valorCusto))
                    vm.ValorCusto = valorCusto.Replace(".", "").Replace(",", ".").ToDecimalInvariant();
                else
                    vm.ValorCusto = planoExames.ValorCusto;

                if (!string.IsNullOrEmpty(valorItem))
                    vm.ValorItem = valorItem.Replace(".", "").Replace(",", ".").ToDecimalInvariant();
                else
                    vm.ValorItem = planoExames.ValorItem;

                vm.TipoContaExame = planoExames.ContaExame.Substring(7, 4) == "0000" ? (int)TipoContaExame.Principal : (int)TipoContaExame.Item;
            }
            catch (Exception ex)
            {
                _eventLog.LogEventViewer("[PlanoExamesItens] Alterar - Erro: " + ex.Message, "wError");
            }

            ViewBag.TextoMenu = new object[] { "Alterar Item do Plano de Exames", false };
            _geralController.Validacao("AlterarPlanoExamesItens", ViewBag.TextoMenu[0]);
            return View(vm);
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarPlanoExamesItens")]
        public async Task<IActionResult> SalvarAlteracaoPlanoExamesItens(vmPlanoExames vm, int id)
        {
            string redirecionaUrl = "PlanoExamesItens".MontaUrl(base.HttpContext.Request);

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

                                plano.Descricao = contaExame.Substring(7, 4) == "0000" ? vm.Descricao.ToUpper() : vm.Descricao;
                                plano.Etiqueta = string.IsNullOrEmpty(vm.Etiqueta.ToString()) ? 0 : vm.Etiqueta;
                                plano.Etiquetas = string.IsNullOrEmpty(vm.Etiquetas.ToString()) ? 0 : vm.Etiquetas;
                                plano.Seleciona = string.IsNullOrEmpty(vm.Seleciona.ToString()) ? 0 : vm.Seleciona;
                                plano.NaoMostrar = string.IsNullOrEmpty(vm.NaoMostrar.ToString()) ? 0 : vm.NaoMostrar;
                                plano.ValorCusto = vm.ValorCusto;
                                plano.ValorItem = vm.ValorItem;
                            }

                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Item do Plano de Exames foi atualizado", action = "", sucesso = true });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _eventLog.LogEventViewer("[PlanoExamesItens] Alteração - Erro: " + ex.Message, "wError");
                            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Item do Plano NÃO foi atualizado", action = "", sucesso = false });
                        }
                    }
                });
                //..Kiro
            }
            catch (Exception ex)
            {
                _eventLog.LogEventViewer("[PlanoExamesItens] Alteração - Erro geral: " + ex.Message, "wError");
            }
            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Item do Plano NÃO foi atualizado", action = "", sucesso = false });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("SalvarItemGrid")]
        public async Task<IActionResult> SalvarItemGrid(vmPlanoExames vm, string[] parameters)
        {
            int id = UtilBLL.RetornaValorFormulario(parameters, 0, 0, 'I');
            string valorCusto = UtilBLL.RetornaValorFormulario(parameters, 0, 1, 'D');
            string valorItem = UtilBLL.RetornaValorFormulario(parameters, 0, 2, 'D');
            int idTabela = UtilBLL.RetornaValorFormulario(parameters, 0, 3, 'I');
            int idFolha = UtilBLL.RetornaValorFormulario(parameters, 0, 4, 'I');

            decimal? valorCustoSalvar = string.IsNullOrEmpty(valorCusto) ? 0 : valorCusto.ToDecimalInvariant();
            decimal? valorItemSalvar = string.IsNullOrEmpty(valorItem) ? 0 : valorItem.ToDecimalInvariant();
            string linhaLucro = UtilsMath.CalcLucroVariante(valorCustoSalvar, valorItemSalvar, 4, "%");
            string linhaLucroVariante = linhaLucro;

            PlanoExames planoExames = await _db.PlanoExames.Where(x => x.Id == id).SingleAsync();
            if (planoExames == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Falhou a gravação na linha do registro" });

            /*
             * Bloco da gravação dos dados do registro
             */
            //Altera os registros igualmente para todas as instituições existentes, pelo modelo que veio alterado!
            try
            {
                //Feito pelo Kiro em 20/04/2026
                Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            planoExames.ValorCusto = valorCustoSalvar == 0 ? null : valorCustoSalvar;
                            planoExames.ValorItem = valorItemSalvar == 0 ? null : valorItemSalvar;

                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            //Para conseguir atualizar o sumário no Grid após salvar o item
                            ICollection<PlanoExames> dados = await _db.PlanoExames.Where(s => !s.ContaExame.EndsWith("0000000") && s.ExameId == idFolha && s.TabelaExamesId == idTabela).AsNoTracking().OrderByDescending(o => o.Id).ToListAsync();
                            CalculaLucroVariante(dados);

                            TempData["linhaLucroVariante"] = linhaLucroVariante;
                            TempData.Keep();

                            return Json(new { titulo = Mensagens_pt_BR.Salvou, mensagem = "A linha do registro foi salva", id = id, sumario = TempData["Summary"], linhaLucroVariante = linhaLucroVariante });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _eventLog.LogEventViewer("[PlanoExamesItens] Salvar Item Grid - Erro: " + ex.Message, "wError");
                            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Falhou a gravação na linha do registro", id = id, sumario = TempData["Summary"], linhaLucroVariante = TempData["linhaLucroVariante"] });
                        }
                    }
                });
                //..Kiro
            }
            catch (Exception ex)
            {
                _eventLog.LogEventViewer("[PlanoExamesItens] Salvar Item Grid - Erro geral: " + ex.Message, "wError");
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Falhou a gravação na linha do registro", id = id, sumario = TempData["Summary"], linhaLucroVariante = TempData["linhaLucroVariante"] });
            }
        }

        //[TypeFilter(typeof(SessionFilter))]
        //[HttpGet]
        //[Route("ListaTabelaExames")]
        //public async Task<IActionResult> ListaTabelaExames(vmPlanoExames vm)
        //{
        //    List<SelectListItem> id_tabela = new List<SelectListItem>();
        //    List<SelectListItem> nome_tabela = new List<SelectListItem>();

        //    var tabelas = _db.TabelaExames.Where(l => l.Bloqueado == 0).OrderBy(o => o.NomeTabela).ToList();
        //    if (tabelas != null && tabelas.Count > 0)
        //    {
        //        id_tabela = tabelas.Select(l => new SelectListItem() { Text = l.Id.ToString(), Value = l.Id.ToString() }).ToList();
        //        nome_tabela = tabelas.Select(l => new SelectListItem() { Text = l.SiglaTabela + " | " + l.NomeTabela, Value = l.Id.ToString() }).ToList();
        //    }

        //    return View();
        //}
    } //Fim
}