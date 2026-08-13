using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class FormasRecebimentoController : BaseController
    {
        public FormasRecebimentoController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("FormasRecebimento")]
        public IActionResult Index()
        {
            ViewBag.TextoMenu = new object[] { "Formas de Recebimento", false };
            return View(new vmFormasRecebimento());
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("FormasRecebimento/Listar")]
        public async Task<IActionResult> Listar([FromForm] DataTableRequest request)
        {
            try
            {
                int draw = request.Draw;
                int start = request.Start;
                int length = Math.Max(request.Length, 10);
                string searchValue = request.Search?.Value?.Trim() ?? string.Empty;

                var query = _db.FormasRecebimento.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(x => x.Nome != null && x.Nome.Contains(searchValue));
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
                    "permiteparticular" => sortDir == "desc" ? query.OrderByDescending(x => x.PermiteParticular) : query.OrderBy(x => x.PermiteParticular),
                    "permiteinstituicao" => sortDir == "desc" ? query.OrderByDescending(x => x.PermiteInstituicao) : query.OrderBy(x => x.PermiteInstituicao),
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
                    permiteParticular = item.PermiteParticular,
                    permiteInstituicao = item.PermiteInstituicao,
                    ativo = item.Ativo,
                    acoes = $"<button class='btn btn-sm btn-primary btn-editar' data-id='{item.Id}'><i class='fa-solid fa-pen'></i></button>"
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
                _eventLogHelper.LogEventViewer("[FormasRecebimento] Listar - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("FormasRecebimento/Obter/{id:int}")]
        public async Task<IActionResult> Obter(int id)
        {
            try
            {
                var forma = await _db.FormasRecebimento
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (forma == null)
                    return Json(new { sucesso = false, mensagem = "Forma não encontrada." });

                return Json(new
                {
                    sucesso = true,
                    dados = new
                    {
                        forma.Id,
                        forma.Nome,
                        forma.PermiteParticular,
                        forma.PermiteInstituicao,
                        forma.Ativo
                    }
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[FormasRecebimento] Obter - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao carregar forma." });
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("FormasRecebimento/Salvar")]
        public async Task<IActionResult> Salvar([FromBody] vmFormasRecebimento model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { sucesso = false, mensagem = "Dados inválidos." });

                FormasRecebimento entity;

                if (model.Id > 0)
                {
                    entity = await _db.FormasRecebimento.FirstOrDefaultAsync(x => x.Id == model.Id);
                    if (entity == null)
                        return Json(new { sucesso = false, mensagem = "Forma não encontrada." });
                }
                else
                {
                    entity = new FormasRecebimento
                    {
                        DataRegistro = _geralController.ObterDataHoraUtc()
                    };
                    _db.FormasRecebimento.Add(entity);
                }

                entity.Nome = model.Nome;
                entity.PermiteParticular = model.PermiteParticular;
                entity.PermiteInstituicao = model.PermiteInstituicao;
                entity.Ativo = model.Ativo;

                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Forma salva com sucesso.", id = entity.Id });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[FormasRecebimento] Salvar - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao salvar forma." });
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("FormasRecebimento/Excluir/{id:int}")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var entity = await _db.FormasRecebimento.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                    return Json(new { sucesso = false, mensagem = "Forma não encontrada." });

                // Não permite excluir formas padrão ou já utilizadas em catálogos
                bool utilizada = await _db.CatalogoRecebimentosFormas.AnyAsync(x => x.FormaRecebimentoId == id);
                if (utilizada)
                    return Json(new { sucesso = false, mensagem = "Esta forma já foi utilizada em recebimentos e não pode ser excluída." });

                _db.FormasRecebimento.Remove(entity);
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Forma excluída com sucesso." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[FormasRecebimento] Excluir - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao excluir forma." });
            }
        }
    }
}
