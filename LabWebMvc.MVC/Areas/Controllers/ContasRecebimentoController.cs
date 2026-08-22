using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Servicos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class ContasRecebimentoController : BaseController
    {
        private static bool _sequenceVerificado;
        private readonly IGeralService _geralService;

        public ContasRecebimentoController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IGeralService geralService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _geralService = geralService;
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ContasRecebimento")]
        public IActionResult Index()
        {
            ViewBag.TextoMenu = new object[] { "Contas de Recebimento", false };
            return View(new vmContasRecebimento());
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ContasRecebimento/Listar")]
        public async Task<IActionResult> Listar([FromForm] DataTableRequest request)
        {
            try
            {
                int draw = request.Draw;
                int start = request.Start;
                int length = Math.Max(request.Length, 10);
                string searchValue = request.Search?.Value?.Trim() ?? string.Empty;

                var query = _db.ContasRecebimento.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(x =>
                        (x.Nome != null && x.Nome.Contains(searchValue)) ||
                        (x.Identificacao != null && x.Identificacao.Contains(searchValue)));
                }

                int recordsTotal = await query.CountAsync();

                string sortColumn = request.Order.Count > 0 && request.Order[0].Column < request.Columns.Count
                    ? (request.Columns[request.Order[0].Column].Data ?? "id")
                    : "id";
                string sortDir = request.Order.Count > 0
                    ? (request.Order[0].Dir ?? "asc")
                    : "asc";

                query = sortColumn.ToLowerInvariant() switch
                {
                    "nome" => sortDir == "desc" ? query.OrderByDescending(x => x.Nome) : query.OrderBy(x => x.Nome),
                    "tipo" => sortDir == "desc" ? query.OrderByDescending(x => x.Tipo) : query.OrderBy(x => x.Tipo),
                    "padraoportaria" => sortDir == "desc" ? query.OrderByDescending(x => x.PadraoPortaria) : query.OrderBy(x => x.PadraoPortaria),
                    "ativo" => sortDir == "desc" ? query.OrderByDescending(x => x.Ativo) : query.OrderBy(x => x.Ativo),
                    _ => sortDir == "desc" ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
                };

                var data = await query
                    .Skip(start)
                    .Take(length)
                    .ToListAsync();

                var result = data.Select(item => (object)new
                {
                    id = item.Id,
                    nome = item.Nome ?? string.Empty,
                    tipo = ObterDescricaoTipo(item.Tipo),
                    identificacao = item.Identificacao ?? string.Empty,
                    padraoPortaria = item.PadraoPortaria,
                    ativo = item.Ativo,
                    acoes = item.Id == 1
                        ? "<span class='text-muted' title='Conta padrão do sistema'><i class='fa-solid fa-lock'></i></span>"
                        : $"<button class='btn btn-sm btn-primary btn-editar' data-id='{item.Id}'><i class='fa-solid fa-pen'></i></button>"
                }).ToList();

                return Json(new DataTableResponse<object>
                {
                    Draw = draw,
                    RecordsTotal = recordsTotal,
                    RecordsFiltered = recordsTotal,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ContasRecebimento] Listar - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        private static string ObterDescricaoTipo(int tipo)
        {
            return tipo switch
            {
                1 => "Caixa",
                2 => "Banco",
                3 => "Cofre",
                4 => "Outro",
                _ => "Outro"
            };
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ContasRecebimento/Obter/{id:int}")]
        public async Task<IActionResult> Obter(int id)
        {
            try
            {
                var conta = await _db.ContasRecebimento
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (conta == null)
                    return Json(new { sucesso = false, mensagem = "Conta não encontrada." });

                return Json(new
                {
                    sucesso = true,
                    dados = new
                    {
                        conta.Id,
                        conta.Nome,
                        conta.Tipo,
                        conta.Identificacao,
                        conta.PadraoPortaria,
                        conta.Ativo
                    }
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ContasRecebimento] Obter - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao carregar conta." });
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ContasRecebimento/Salvar")]
        public async Task<IActionResult> Salvar([FromBody] vmContasRecebimento model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { sucesso = false, mensagem = "Dados inválidos." });

                ContasRecebimento entity;

                if (model.Id > 0)
                {
                    if (model.Id == 1)
                        return Json(new { sucesso = false, mensagem = "A conta padrão 'Caixa' não pode ser alterada." });

                    entity = await _db.ContasRecebimento.FirstOrDefaultAsync(x => x.Id == model.Id);
                    if (entity == null)
                        return Json(new { sucesso = false, mensagem = "Conta não encontrada." });
                }
                else
                {
                    await GarantirSequenceSincronizado();

                    entity = new ContasRecebimento
                    {
                        DataRegistro = _geralService.ObterDataHoraUtc()
                    };
                    _db.ContasRecebimento.Add(entity);
                }

                entity.Nome = model.Nome;
                entity.Tipo = model.Tipo;
                entity.Identificacao = model.Identificacao;
                entity.PadraoPortaria = model.PadraoPortaria;
                entity.Ativo = model.Ativo;

                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Conta salva com sucesso.", id = entity.Id });
            }
            catch (Exception ex)
            {
                string detalhes = ObterMensagemErroCompleta(ex);
                _eventLogHelper.LogEventViewer("[ContasRecebimento] Salvar - Erro: " + detalhes + " | Stack: " + ex.StackTrace, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao salvar conta.", detalhes });
            }
        }

        private static string ObterMensagemErroCompleta(Exception ex)
        {
            var sb = new StringBuilder(ex.Message);
            Exception? inner = ex.InnerException;
            while (inner != null)
            {
                sb.Append(" -> ");
                sb.Append(inner.Message);
                inner = inner.InnerException;
            }
            return sb.ToString();
        }

        private async Task GarantirSequenceSincronizado()
        {
            if (_sequenceVerificado)
                return;

            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    DO $$
                    DECLARE
                        seq_name text;
                        max_id int;
                    BEGIN
                        seq_name := pg_get_serial_sequence('""ContasRecebimento""', 'Id');
                        IF seq_name IS NULL THEN
                            seq_name := pg_get_serial_sequence('ContasRecebimento', 'Id');
                        END IF;
                        IF seq_name IS NOT NULL THEN
                            SELECT COALESCE(MAX(Id), 0) + 1 INTO max_id FROM ContasRecebimento;
                            PERFORM setval(seq_name, max_id, false);
                        END IF;
                    END $$;
                ");
                _sequenceVerificado = true;
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ContasRecebimento] GarantirSequenceSincronizado - Aviso: " + ex.Message, "wWarning");
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ContasRecebimento/Excluir/{id:int}")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                if (id == 1)
                    return Json(new { sucesso = false, mensagem = "A conta padrão 'Caixa' não pode ser excluída." });

                var entity = await _db.ContasRecebimento.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                    return Json(new { sucesso = false, mensagem = "Conta não encontrada." });

                // Não permite excluir contas já utilizadas em catálogos
                bool utilizada = await _db.CatalogoRecebimentosFormas.AnyAsync(x => x.ContaRecebimentoId == id);
                if (utilizada)
                    return Json(new { sucesso = false, mensagem = "Esta conta já foi utilizada em recebimentos e não pode ser excluída." });

                _db.ContasRecebimento.Remove(entity);
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Conta excluída com sucesso." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ContasRecebimento] Excluir - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao excluir conta." });
            }
        }
    }
}
