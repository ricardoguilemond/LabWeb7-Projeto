using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    //Feito pelo Kiro em 17/05/2026
    public class ConsultarExamesController : BaseController
    {
        public ConsultarExamesController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        { }

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarExames")]
        public async Task<IActionResult> Index(
            string? dataExameDe,
            string? dataExamePara,
            string? nomePaciente,
            int? codigoExame,
            string? siglaInstituicao,
            string? nomeInstituicao,
            string? siglaPosto,
            string? nomePosto)
        {
            ICollection<dynamic> listaGrid = [];

            var query = _db.ExamesRealizados
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.Postos)
                .Include(e => e.Pacientes)
                .Include(e => e.TabelaExames)
                .AsQueryable();

            // Defaults: DE = 3 dias atras, PARA = ontem (hoje nao aparece por padrao)
            DateTime hojeLocal = DateTime.Now.Date;
            DateTime dataDe = hojeLocal.AddDays(-3);
            DateTime dataPara = hojeLocal.AddDays(-1);

            // Parse se fornecido pelo usuario
            if (!string.IsNullOrEmpty(dataExameDe))
                dataDe = dataExameDe.Trim().FormataData("dd/MM/yyyy", true);
            if (!string.IsNullOrEmpty(dataExamePara))
                dataPara = dataExamePara.Trim().FormataData("dd/MM/yyyy", true);

            // Converte para range UTC (necessario para timestamptz no Npgsql 8.x)
            var (inicioDeUtc, _) = _geralController.ConverterDataLocalParaRangeUtc(dataDe);
            var (_, fimParaUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataPara);
            query = query.Where(e => e.DataIni >= inicioDeUtc && e.DataIni <= fimParaUtc);

            // Passa as datas efetivas para a view
            ViewBag.DataExameDe = dataDe.ToString("dd/MM/yyyy");
            ViewBag.DataExamePara = dataPara.ToString("dd/MM/yyyy");

            bool temOutroFiltro = !string.IsNullOrEmpty(nomePaciente)
                          || codigoExame.HasValue
                          || !string.IsNullOrEmpty(siglaInstituicao)
                          || !string.IsNullOrEmpty(nomeInstituicao)
                          || !string.IsNullOrEmpty(siglaPosto)
                          || !string.IsNullOrEmpty(nomePosto);

            if (!string.IsNullOrEmpty(nomePaciente))
                query = query.Where(e => e.Pacientes.NomePaciente
                    .ToLower().Contains(nomePaciente.Trim().ToLower()));

            if (codigoExame.HasValue)
                query = query.Where(e => e.Id == codigoExame.Value);

            if (!string.IsNullOrEmpty(siglaInstituicao))
                query = query.Where(e => e.Instituicao.Sigla
                    .ToLower().Contains(siglaInstituicao.Trim().ToLower()));

            if (!string.IsNullOrEmpty(nomeInstituicao))
                query = query.Where(e => e.Instituicao.Nome
                    .ToLower().Contains(nomeInstituicao.Trim().ToLower()));

            if (!string.IsNullOrEmpty(nomePosto))
                query = query.Where(e => e.Postos != null && e.Postos.NomePosto
                    .ToLower().Contains(nomePosto.Trim().ToLower()));

            //Feito pelo Qoder em 21/04/2026 - filtro por Sigla Posto (D8)
            if (!string.IsNullOrEmpty(siglaPosto))
                query = query.Where(e => e.Postos != null && e.Postos.SiglaPosto
                    .ToLower().Contains(siglaPosto.Trim().ToLower()));
            //..Qoder

            // Sem outros filtros alem da data: limitar a 100 registros
            if (!temOutroFiltro)
                query = query.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id).Take(100);
            else
                query = query.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id);

            var dados = await query.ToListAsync();

            int totalRegistros = 0;
            int totalTabela = _db.ExamesRealizados.AsNoTracking().Count();

            foreach (var item in dados)
            {
                totalRegistros++;
                listaGrid.Add(new
                {
                    Id = item.Id,
                    SiglaTabela = item.TabelaExames?.SiglaTabela ?? "",
                    SiglaInstituicao = item.Instituicao.Sigla,
                    NomeInstituicao = item.Instituicao.Nome,
                    SiglaPosto = item.Postos?.SiglaPosto ?? "",
                    NomePosto = item.Postos?.NomePosto ?? "",
                    NomePaciente = item.Pacientes.NomePaciente,
                    Nascimento = item.Pacientes.Nascimento,
                    Sequencial = item.Sequencial,
                    DataIni = item.DataIni
                });
            }

            ViewBag.TextoMenu = new object[] { "Consultar Exames", false };
            var vmIndex = new vmConsultarExames { ListaDados = listaGrid };
            return View(vmIndex);
        }
        //..Kiro

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarExames/ObterItensExame")]
        public async Task<IActionResult> ObterItensExame(int exameRealizadoId)
        {
            var itensRaw = await _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ExameRealizadoId == exameRealizadoId)
                .OrderBy(i => i.OrdemItem)
                .Select(i => new
                {
                    i.Id,
                    i.ClasseExamesNome,
                    i.RefExame,
                    i.RefItem,
                    i.ContaExame,
                    i.Descricao,
                    i.ValorItem,
                    i.Etiquetas
                })
                .ToListAsync();

            var itens = itensRaw.Select(i => new
            {
                i.Id,
                i.ClasseExamesNome,
                i.RefExame,
                i.RefItem,
                ContaExame = i.ContaExame.FormatarContaExameSem11(),
                i.Descricao,
                ValorItem = i.ValorItem.HasValue
                    ? i.ValorItem.Value.ToString("N2")
                    : "-",
                i.Etiquetas
            }).ToList();

            return Json(new { sucesso = true, itens });
        }
        //..Kiro

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarExames/ExcluirExame")]
        public async Task<IActionResult> ExcluirExame(int id)
        {
            var exame = await _db.ExamesRealizados
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exame == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Exame não encontrado", action = "", sucesso = false });

            var itens = await _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ExameRealizadoId == id)
                .ToListAsync();

            bool possuiResultado = itens.Any(i => !string.IsNullOrEmpty(i.Resultado));

            if (possuiResultado)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Exame possui resultados lançados e não pode ser excluído", action = "", sucesso = false });

            //Feito pelo Qoder em 04/06/2026
            // Verificar se existem fichas vinculadas ao exame antes de permitir a exclusão
            bool possuiFichasInternas = await _db.FichasInternas.AnyAsync(f => f.ExamesRealizadosId == id);
            bool possuiFichasLotes = await _db.FichasLotes.AnyAsync(f => f.ExamesRealizadosId == id);
            bool possuiFichasPlanilhas = await _db.FichasPlanilhas.AnyAsync(f => f.ExamesRealizadosId == id);

            if (possuiFichasInternas || possuiFichasLotes || possuiFichasPlanilhas)
            {
                string mensagem = "Exame possui fichas vinculadas e não pode ser excluído. Verifique:";
                if (possuiFichasInternas) mensagem += " Fichas Internas;";
                if (possuiFichasLotes) mensagem += " Fichas de Lotes;";
                if (possuiFichasPlanilhas) mensagem += " Fichas de Planilhas;";
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = mensagem.TrimEnd(';'), action = "", sucesso = false });
            }
            //..Qoder

            var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var itensParaExcluir = await _db.ItensExamesRealizados
                    .Where(i => i.ExameRealizadoId == id)
                    .ToListAsync();

                _db.ItensExamesRealizados.RemoveRange(itensParaExcluir);

                var requisitarVinculados = await _db.Requisitar
                    .Where(r => r.ExameRealizadoId == id)
                    .ToListAsync();

                _db.Requisitar.RemoveRange(requisitarVinculados);

                var exameParaExcluir = await _db.ExamesRealizados
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (exameParaExcluir != null)
                    _db.ExamesRealizados.Remove(exameParaExcluir);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Exame foi excluído com sucesso", action = "", sucesso = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _eventLogHelper.LogEventViewer("[ConsultarExames] ExcluirExame - Erro: " + ex.Message, "wError");
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Erro ao excluir exame", action = "", sucesso = false });
            }
        }
        //..Kiro

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarExames/ExcluirItemExame")]
        public async Task<IActionResult> ExcluirItemExame(int itemId)
        {
            var item = await _db.ItensExamesRealizados
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Item de exame não encontrado", action = "", sucesso = false });

            if (!string.IsNullOrEmpty(item.Resultado))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Este item possui resultado lançado e não pode ser excluído", action = "", sucesso = false });

            // Verificar se o item correspondente existe em Requisitar
            var requisitarItem = await _db.Requisitar
                .FirstOrDefaultAsync(r => r.ExameRealizadoId == item.ExameRealizadoId
                                       && r.ContaExame == item.ContaExame);

            if (requisitarItem == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Item correspondente não encontrado na tabela Requisitar. Exclusão não permitida.", action = "", sucesso = false });

            var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Excluir o item de ItensExamesRealizados
                var itemParaExcluir = await _db.ItensExamesRealizados
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (itemParaExcluir != null)
                    _db.ItensExamesRealizados.Remove(itemParaExcluir);

                // Excluir o item correspondente em Requisitar
                _db.Requisitar.Remove(requisitarItem);

                await _db.SaveChangesAsync();

                // Reordenar os itens restantes do exame
                var itensRestantes = await _db.ItensExamesRealizados
                    .Where(i => i.ExameRealizadoId == item.ExameRealizadoId)
                    .OrderBy(i => i.OrdemItem)
                    .ToListAsync();

                int ordem = 1;
                foreach (var it in itensRestantes)
                {
                    it.OrdemItem = ordem++;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _eventLogHelper.LogEventViewer(
                    $"[ConsultarExames] ExcluirItemExame - Item Id={itemId}, ContaExame={item.ContaExame}, ExameRealizadoId={item.ExameRealizadoId} excluído com sucesso.",
                    "wInformation");

                return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Item de exame excluído com sucesso", action = "", sucesso = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _eventLogHelper.LogEventViewer("[ConsultarExames] ExcluirItemExame - Erro: " + ex.Message, "wError");
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Erro ao excluir item de exame", action = "", sucesso = false });
            }
        }
        //..Kiro
    }
    //..Kiro
}
