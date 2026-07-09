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
            string? nomePosto,
            string situacaoExame = "todos")
        {
            ICollection<dynamic> listaGrid = [];

            // Valida valores permitidos
            var opcoesValidas = new[] { "todos", "liberados", "naoLiberados", "pendentes", "baixados" };
            if (!opcoesValidas.Contains(situacaoExame))
                situacaoExame = "todos";

            // Defaults: DE = 15 dias atras, PARA = hoje
            DateTime hojeLocal = DateTime.Now.Date;
            DateTime dataDe = hojeLocal.AddDays(-15);
            DateTime dataPara = hojeLocal;

            // Parse se fornecido pelo usuario
            if (!string.IsNullOrEmpty(dataExameDe))
                dataDe = dataExameDe.Trim().FormataData("dd/MM/yyyy", true);
            if (!string.IsNullOrEmpty(dataExamePara))
                dataPara = dataExamePara.Trim().FormataData("dd/MM/yyyy", true);

            // Converte para range UTC (necessario para timestamptz no Npgsql 8.x)
            var (inicioDeUtc, _) = _geralController.ConverterDataLocalParaRangeUtc(dataDe);
            var (_, fimParaUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataPara);

            // Passa as datas efetivas para a view
            ViewBag.DataExameDe = dataDe.ToString("dd/MM/yyyy");
            ViewBag.DataExamePara = dataPara.ToString("dd/MM/yyyy");
            ViewBag.SituacaoExame = situacaoExame;

            bool temOutroFiltro = !string.IsNullOrEmpty(nomePaciente)
                          || codigoExame.HasValue
                          || !string.IsNullOrEmpty(siglaInstituicao)
                          || !string.IsNullOrEmpty(nomeInstituicao)
                          || !string.IsNullOrEmpty(siglaPosto)
                          || !string.IsNullOrEmpty(nomePosto);

            var pendentesIds = await _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ContaExame.Substring(i.ContaExame.Length - 4) != "0000")
                .Where(i => string.IsNullOrEmpty(i.Resultado))
                .Select(i => i.ExameRealizadoId)
                .Distinct()
                .ToListAsync();

            IQueryable<ExamesRealizados> CriarQueryExamesRealizados()
            {
                var q = _db.ExamesRealizados
                    .AsNoTracking()
                    .Include(e => e.Instituicao)
                    .Include(e => e.Postos)
                    .Include(e => e.Pacientes)
                    .Include(e => e.TabelaExames)
                    .Where(e => e.DataIni >= inicioDeUtc && e.DataIni <= fimParaUtc)
                    .AsQueryable();

                switch (situacaoExame)
                {
                    case "liberados":
                        q = q.Where(e => e.Liberacao == 1 && e.Baixado == 0);
                        break;
                    case "naoLiberados":
                        q = q.Where(e => e.Liberacao == 0 && e.Baixado == 0 && !pendentesIds.Contains(e.Id));
                        break;
                    case "pendentes":
                        q = q.Where(e => e.Liberacao == 0 && e.Baixado == 0 && pendentesIds.Contains(e.Id));
                        break;
                }

                return q;
            }

            IQueryable<ExamesRealizadosAM> CriarQueryExamesRealizadosAM()
            {
                return _db.ExamesRealizadosAM
                    .AsNoTracking()
                    .Include(e => e.Instituicao)
                    .Include(e => e.Postos)
                    .Include(e => e.Pacientes)
                    .Include(e => e.TabelaExames)
                    .Where(e => e.DataIni >= inicioDeUtc && e.DataIni <= fimParaUtc)
                    .AsQueryable();
            }

            IQueryable<ExamesRealizados> AplicarFiltros(IQueryable<ExamesRealizados> q)
            {
                if (!string.IsNullOrEmpty(nomePaciente))
                    q = q.Where(e => e.Pacientes.NomePaciente
                        .ToLower().Contains(nomePaciente.Trim().ToLower()));

                if (codigoExame.HasValue)
                    q = q.Where(e => e.Id == codigoExame.Value);

                if (!string.IsNullOrEmpty(siglaInstituicao))
                    q = q.Where(e => e.Instituicao.Sigla
                        .ToLower().Contains(siglaInstituicao.Trim().ToLower()));

                if (!string.IsNullOrEmpty(nomeInstituicao))
                    q = q.Where(e => e.Instituicao.Nome
                        .ToLower().Contains(nomeInstituicao.Trim().ToLower()));

                if (!string.IsNullOrEmpty(nomePosto))
                    q = q.Where(e => e.Postos != null && e.Postos.NomePosto
                        .ToLower().Contains(nomePosto.Trim().ToLower()));

                if (!string.IsNullOrEmpty(siglaPosto))
                    q = q.Where(e => e.Postos != null && e.Postos.SiglaPosto
                        .ToLower().Contains(siglaPosto.Trim().ToLower()));

                if (!temOutroFiltro)
                    q = q.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id).Take(100);
                else
                    q = q.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id);

                return q;
            }

            IQueryable<ExamesRealizadosAM> AplicarFiltrosAM(IQueryable<ExamesRealizadosAM> q)
            {
                if (!string.IsNullOrEmpty(nomePaciente))
                    q = q.Where(e => e.Pacientes.NomePaciente
                        .ToLower().Contains(nomePaciente.Trim().ToLower()));

                if (codigoExame.HasValue)
                    q = q.Where(e => e.Id == codigoExame.Value);

                if (!string.IsNullOrEmpty(siglaInstituicao))
                    q = q.Where(e => e.Instituicao.Sigla
                        .ToLower().Contains(siglaInstituicao.Trim().ToLower()));

                if (!string.IsNullOrEmpty(nomeInstituicao))
                    q = q.Where(e => e.Instituicao.Nome
                        .ToLower().Contains(nomeInstituicao.Trim().ToLower()));

                if (!string.IsNullOrEmpty(nomePosto))
                    q = q.Where(e => e.Postos != null && e.Postos.NomePosto
                        .ToLower().Contains(nomePosto.Trim().ToLower()));

                if (!string.IsNullOrEmpty(siglaPosto))
                    q = q.Where(e => e.Postos != null && e.Postos.SiglaPosto
                        .ToLower().Contains(siglaPosto.Trim().ToLower()));

                if (!temOutroFiltro)
                    q = q.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id).Take(100);
                else
                    q = q.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id);

                return q;
            }

            void AdicionarItemExamesRealizados(dynamic item, string situacao)
            {
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
                    DataIni = item.DataIni,
                    Liberacao = item.Liberacao,
                    Baixado = item.Baixado,
                    SituacaoExame = situacao
                });
            }

            if (situacaoExame == "baixados")
            {
                var dadosAm = await AplicarFiltrosAM(CriarQueryExamesRealizadosAM()).ToListAsync();
                foreach (var item in dadosAm)
                    AdicionarItemExamesRealizados(item, "baixados");
            }
            else if (situacaoExame == "todos")
            {
                var dados = await AplicarFiltros(CriarQueryExamesRealizados()).ToListAsync();
                foreach (var item in dados)
                    AdicionarItemExamesRealizados(item, "todos");

                var dadosAm = await AplicarFiltrosAM(CriarQueryExamesRealizadosAM()).ToListAsync();
                foreach (var item in dadosAm)
                    AdicionarItemExamesRealizados(item, "todos");
            }
            else
            {
                var dados = await AplicarFiltros(CriarQueryExamesRealizados()).ToListAsync();
                foreach (var item in dados)
                    AdicionarItemExamesRealizados(item, situacaoExame);
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
