using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.Controllers
{
    /// <summary>
    /// Controller do Catálogo de Recebimentos.
    /// Permite lançar recebimentos por instituição/período (origem Faturamento)
    /// e na portaria (origem Portaria), além de consulta e relatório.
    /// </summary>
    public class CatalogoRecebimentosController : BaseController
    {
        public CatalogoRecebimentosController(
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

        #region Tela de Lançamento por Instituição/Período

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CatalogoRecebimentos")]
        public async Task<IActionResult> Index()
        {
            var model = new vmCatalogoRecebimento
            {
                Instituicoes = await CarregarInstituicoes()
            };

            ViewBag.TextoMenu = new object[] { "Catálogo de Recebimentos", false };
            return View(model);
        }

        /// <summary>
        /// Lista exames liberados, não baixados e ainda não incluídos em catálogo,
        /// filtrados por período de realização e instituição.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CatalogoRecebimentos/ListarExames")]
        public async Task<IActionResult> ListarExames([FromForm] DataTableRequest request, DateTime dataIni, DateTime dataFim, int? instituicaoId)
        {
            try
            {
                int draw = request.Draw;
                int start = request.Start;
                int length = Math.Max(request.Length, 10);
                string searchValue = request.Search?.Value?.Trim() ?? string.Empty;

                var dataFimAjustada = dataFim.Date.AddDays(1).AddTicks(-1);

                var query = _db.ExamesRealizados
                    .AsNoTracking()
                    .Include(e => e.Pacientes)
                    .Include(e => e.Instituicao)
                    .Where(e => e.Liberacao == 1
                             && e.Baixado != 1
                             && !e.EmCatalogoRecebimentos
                             && e.DataExame >= dataIni.Date
                             && e.DataExame <= dataFimAjustada)
                    .AsQueryable();

                if (instituicaoId.HasValue && instituicaoId.Value > 0)
                    query = query.Where(e => e.InstituicaoId == instituicaoId.Value);

                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(e =>
                        (e.Pacientes.NomePaciente != null && e.Pacientes.NomePaciente.Contains(searchValue)) ||
                        (e.Pacientes.CPF != null && e.Pacientes.CPF.Contains(searchValue)) ||
                        (e.Instituicao.Sigla != null && e.Instituicao.Sigla.Contains(searchValue)) ||
                        e.Sequencial.ToString().Contains(searchValue));
                }

                int recordsTotal = await query.CountAsync();

                string sortColumn = request.Order.Count > 0 && request.Order[0].Column < request.Columns.Count
                    ? (request.Columns[request.Order[0].Column].Data ?? "exameRealizadoId")
                    : "exameRealizadoId";
                string sortDir = request.Order.Count > 0
                    ? (request.Order[0].Dir ?? "desc")
                    : "desc";

                query = sortColumn.ToLowerInvariant() switch
                {
                    "nomepaciente" => sortDir == "desc" ? query.OrderByDescending(e => e.Pacientes.NomePaciente) : query.OrderBy(e => e.Pacientes.NomePaciente),
                    "siglainstituicao" => sortDir == "desc" ? query.OrderByDescending(e => e.Instituicao.Sigla) : query.OrderBy(e => e.Instituicao.Sigla),
                    "sequencial" => sortDir == "desc" ? query.OrderByDescending(e => e.Sequencial) : query.OrderBy(e => e.Sequencial),
                    "dataexame" => sortDir == "desc" ? query.OrderByDescending(e => e.DataExame) : query.OrderBy(e => e.DataExame),
                    _ => sortDir == "desc" ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id)
                };

                var data = await query
                    .Skip(start)
                    .Take(length)
                    .Select(e => new vmCatalogoRecebimentoItem
                    {
                        ExameRealizadoId = e.Id,
                        PacienteId = e.PacienteId,
                        NomePaciente = e.Pacientes.NomePaciente ?? "",
                        Cpf = e.Pacientes.CPF ?? "",
                        InstituicaoId = e.InstituicaoId,
                        SiglaInstituicao = e.Instituicao.Sigla ?? "",
                        Sequencial = e.Sequencial,
                        DataExame = e.DataExame,
                        ValorTotal = e.ItensExamesRealizados.Where(i => i.ValorItem.HasValue).Sum(i => i.ValorItem.Value)
                    })
                    .ToListAsync();

                var result = data.Select(item => (object)new
                {
                    item.ExameRealizadoId,
                    item.PacienteId,
                    item.NomePaciente,
                    item.Cpf,
                    item.InstituicaoId,
                    item.SiglaInstituicao,
                    item.Sequencial,
                    dataExame = item.DataExame?.ToString("dd/MM/yyyy") ?? "",
                    valorTotal = item.ValorTotal.ToString("N2")
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
                _eventLogHelper.LogEventViewer("[CatalogoRecebimentos] ListarExames - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        #endregion

        #region Dados Auxiliares

        /// <summary>
        /// Retorna formas e contas de recebimento ativas para preencher os combos do modal.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CatalogoRecebimentos/DadosAuxiliares")]
        public async Task<IActionResult> DadosAuxiliares()
        {
            try
            {
                var formas = await _db.FormasRecebimento
                    .AsNoTracking()
                    .Where(f => f.Ativo)
                    .OrderBy(f => f.Nome)
                    .Select(f => new { f.Id, f.Nome })
                    .ToListAsync();

                var contas = await _db.ContasRecebimento
                    .AsNoTracking()
                    .Where(c => c.Ativo)
                    .OrderBy(c => c.Nome)
                    .Select(c => new { c.Id, c.Nome })
                    .ToListAsync();

                return Json(new { sucesso = true, formas, contas });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[CatalogoRecebimentos] DadosAuxiliares - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao carregar dados auxiliares." });
            }
        }

        #endregion

        #region Portaria

        /// <summary>
        /// Retorna os dados necessários para lançamento rápido na portaria,
        /// a partir do código do exame realizado.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CatalogoRecebimentos/ObterDadosRecebimentoPortaria")]
        public async Task<IActionResult> ObterDadosRecebimentoPortaria(int exameRealizadoId)
        {
            try
            {
                var exame = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Include(e => e.Pacientes)
                    .Include(e => e.Instituicao)
                    .FirstOrDefaultAsync(e => e.Id == exameRealizadoId
                                              && e.Liberacao == 1
                                              && e.Baixado != 1);

                if (exame == null)
                    return Json(new { sucesso = false, mensagem = "Exame não encontrado ou não disponível para recebimento." });

                if (exame.EmCatalogoRecebimentos)
                    return Json(new { sucesso = false, mensagem = "Exame já consta no Catálogo de Recebimentos." });

                decimal valorTotal = await _db.ItensExamesRealizados
                    .Where(i => i.ExameRealizadoId == exameRealizadoId && i.ValorItem.HasValue)
                    .SumAsync(i => i.ValorItem.Value);

                var contasPadrao = await _db.ContasRecebimento
                    .AsNoTracking()
                    .Where(c => c.Ativo && c.PadraoPortaria)
                    .OrderBy(c => c.Nome)
                    .Select(c => new { c.Id, c.Nome })
                    .ToListAsync();

                return Json(new
                {
                    sucesso = true,
                    exame = new
                    {
                        exameRealizadoId = exame.Id,
                        pacienteId = exame.PacienteId,
                        nomePaciente = exame.Pacientes?.NomePaciente ?? "",
                        instituicaoId = exame.InstituicaoId,
                        siglaInstituicao = exame.Instituicao?.Sigla ?? "",
                        sequencial = exame.Sequencial,
                        valorTotal
                    },
                    contasPadrao
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[CatalogoRecebimentos] ObterDadosRecebimentoPortaria - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao carregar dados do recebimento." });
            }
        }

        /// <summary>
        /// Salva recebimento originado na portaria.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CatalogoRecebimentos/SalvarRecebimentoPortaria")]
        public async Task<IActionResult> SalvarRecebimentoPortaria([FromBody] vmCatalogoRecebimentoSalvar dto)
        {
            try
            {
                if (dto == null)
                    return Json(new { sucesso = false, mensagem = "Dados inválidos." });

                dto.Origem = 1; // Portaria
                var resultado = await SalvarCatalogoInterno(dto);
                return Json(resultado);
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[CatalogoRecebimentos] SalvarRecebimentoPortaria - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao salvar recebimento na portaria." });
            }
        }

        #endregion

        #region Persistência

        /// <summary>
        /// Salva catálogo de recebimentos (origem Faturamento).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CatalogoRecebimentos/Salvar")]
        public async Task<IActionResult> Salvar([FromBody] vmCatalogoRecebimentoSalvar dto)
        {
            try
            {
                if (dto == null)
                    return Json(new { sucesso = false, mensagem = "Dados inválidos." });

                dto.Origem = 2; // Faturamento
                var resultado = await SalvarCatalogoInterno(dto);
                return Json(resultado);
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[CatalogoRecebimentos] Salvar - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao salvar catálogo de recebimentos." });
            }
        }

        private async Task<object> SalvarCatalogoInterno(vmCatalogoRecebimentoSalvar dto)
        {
            // Validações básicas
            if (dto.InstituicaoId <= 0)
                return new { sucesso = false, mensagem = "Instituição não informada." };

            if (dto.PacienteId <= 0)
                return new { sucesso = false, mensagem = "Paciente não informado." };

            if (dto.ExamesRealizadosIds == null || dto.ExamesRealizadosIds.Count == 0)
                return new { sucesso = false, mensagem = "Selecione pelo menos um exame." };

            if (dto.Formas == null || dto.Formas.Count == 0)
                return new { sucesso = false, mensagem = "Informe pelo menos uma forma de recebimento." };

            var exames = await _db.ExamesRealizados
                .Where(e => dto.ExamesRealizadosIds.Contains(e.Id))
                .ToListAsync();

            if (exames.Count != dto.ExamesRealizadosIds.Count)
                return new { sucesso = false, mensagem = "Um ou mais exames não foram encontrados." };

            if (exames.Any(e => e.PacienteId != dto.PacienteId))
                return new { sucesso = false, mensagem = "Os exames selecionados devem pertencer ao mesmo paciente." };

            if (exames.Any(e => e.InstituicaoId != dto.InstituicaoId))
                return new { sucesso = false, mensagem = "Os exames selecionados devem pertencer à mesma instituição." };

            if (exames.Any(e => e.EmCatalogoRecebimentos))
                return new { sucesso = false, mensagem = "Um ou mais exames já constam no Catálogo de Recebimentos." };

            decimal valorTotalExames = await _db.ItensExamesRealizados
                .Where(i => dto.ExamesRealizadosIds.Contains(i.ExameRealizadoId) && i.ValorItem.HasValue)
                .SumAsync(i => i.ValorItem.Value);

            decimal valorTotalFormas = dto.Formas.Sum(f => f.Valor);

            if (Math.Abs(valorTotalExames - valorTotalFormas) > 0.01m)
                return new { sucesso = false, mensagem = $"Soma das formas ({valorTotalFormas:N2}) diferente do valor total dos exames ({valorTotalExames:N2})." };

            var formasIds = dto.Formas.Select(f => f.FormaRecebimentoId).Distinct();
            var contasIds = dto.Formas.Select(f => f.ContaRecebimentoId).Distinct();

            int formasCount = await _db.FormasRecebimento.CountAsync(f => formasIds.Contains(f.Id) && f.Ativo);
            int contasCount = await _db.ContasRecebimento.CountAsync(c => contasIds.Contains(c.Id) && c.Ativo);

            if (formasCount != formasIds.Count())
                return new { sucesso = false, mensagem = "Uma ou mais formas de recebimento são inválidas ou inativas." };

            if (contasCount != contasIds.Count())
                return new { sucesso = false, mensagem = "Uma ou mais contas de recebimento são inválidas ou inativas." };

            // Cria o catálogo
            var catalogo = new CatalogoRecebimentos
            {
                Origem = dto.Origem,
                InstituicaoId = dto.InstituicaoId,
                PacienteId = dto.PacienteId,
                PeriodoFaturamento = dto.PeriodoFaturamento,
                ValorTotal = valorTotalExames,
                DataRecebimento = dto.DataRecebimento.Date,
                Status = 1, // Recebido
                Observacao = dto.Observacao,
                UsuarioRegistro = HttpContext.Session.GetString("SessionNome") ?? "sistema",
                DataRegistro = _geralController.ObterDataHoraUtc()
            };

            _db.CatalogoRecebimentos.Add(catalogo);

            foreach (var formaDto in dto.Formas)
            {
                catalogo.CatalogoRecebimentosFormas.Add(new CatalogoRecebimentosFormas
                {
                    FormaRecebimentoId = formaDto.FormaRecebimentoId,
                    ContaRecebimentoId = formaDto.ContaRecebimentoId,
                    Valor = formaDto.Valor,
                    DataRecebimento = formaDto.DataRecebimento.Date,
                    Observacao = formaDto.Observacao
                });
            }

            foreach (var exame in exames)
            {
                decimal valorExame = await _db.ItensExamesRealizados
                    .Where(i => i.ExameRealizadoId == exame.Id && i.ValorItem.HasValue)
                    .SumAsync(i => i.ValorItem.Value);

                catalogo.CatalogoRecebimentosExames.Add(new CatalogoRecebimentosExames
                {
                    ExameRealizadoId = exame.Id,
                    Valor = valorExame
                });

                exame.EmCatalogoRecebimentos = true;
            }

            await _db.SaveChangesAsync();

            _eventLogHelper.LogEventViewer(
                $"[CatalogoRecebimentos] Catálogo salvo - Id={catalogo.Id}, Origem={(dto.Origem == 1 ? "Portaria" : "Faturamento")}, PacienteId={dto.PacienteId}, Valor={valorTotalExames:N2}",
                "wInformation");

            return new { sucesso = true, mensagem = "Catálogo de recebimentos salvo com sucesso.", id = catalogo.Id };
        }

        #endregion

        #region Consulta

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CatalogoRecebimentos/Consulta")]
        public async Task<IActionResult> Consulta()
        {
            var model = new vmCatalogoRecebimentoConsulta
            {
                Instituicoes = await CarregarInstituicoes(),
                FormasRecebimento = await CarregarFormas(),
                ContasRecebimento = await CarregarContas()
            };

            ViewBag.TextoMenu = new object[] { "Consulta de Recebimentos", false };
            return View(model);
        }

        /// <summary>
        /// Lista catálogos de recebimentos com filtros (DataTables server-side).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CatalogoRecebimentos/ListarCatalogos")]
        public async Task<IActionResult> ListarCatalogos([FromForm] DataTableRequest request,
            DateTime? dataIni, DateTime? dataFim, int? instituicaoId, int? pacienteId,
            int? formaRecebimentoId, int? contaRecebimentoId, int? status)
        {
            try
            {
                int draw = request.Draw;
                int start = request.Start;
                int length = Math.Max(request.Length, 10);
                string searchValue = request.Search?.Value?.Trim() ?? string.Empty;

                var query = _db.CatalogoRecebimentos
                    .AsNoTracking()
                    .Include(c => c.Instituicao)
                    .Include(c => c.Paciente)
                    .Include(c => c.CatalogoRecebimentosFormas)
                        .ThenInclude(f => f.FormaRecebimento)
                    .AsQueryable();

                if (dataIni.HasValue)
                    query = query.Where(c => c.DataRecebimento >= dataIni.Value.Date);

                if (dataFim.HasValue)
                {
                    var dataFimAjustada = dataFim.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(c => c.DataRecebimento <= dataFimAjustada);
                }

                if (instituicaoId.HasValue && instituicaoId.Value > 0)
                    query = query.Where(c => c.InstituicaoId == instituicaoId.Value);

                if (pacienteId.HasValue && pacienteId.Value > 0)
                    query = query.Where(c => c.PacienteId == pacienteId.Value);

                if (status.HasValue)
                    query = query.Where(c => c.Status == status.Value);

                if (formaRecebimentoId.HasValue && formaRecebimentoId.Value > 0)
                    query = query.Where(c => c.CatalogoRecebimentosFormas.Any(f => f.FormaRecebimentoId == formaRecebimentoId.Value));

                if (contaRecebimentoId.HasValue && contaRecebimentoId.Value > 0)
                    query = query.Where(c => c.CatalogoRecebimentosFormas.Any(f => f.ContaRecebimentoId == contaRecebimentoId.Value));

                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(c =>
                        (c.Paciente.NomePaciente != null && c.Paciente.NomePaciente.Contains(searchValue)) ||
                        (c.Instituicao.Sigla != null && c.Instituicao.Sigla.Contains(searchValue)) ||
                        (c.PeriodoFaturamento != null && c.PeriodoFaturamento.Contains(searchValue)));
                }

                int recordsTotal = await query.CountAsync();

                string sortColumn = request.Order.Count > 0 && request.Order[0].Column < request.Columns.Count
                    ? (request.Columns[request.Order[0].Column].Data ?? "id")
                    : "id";
                string sortDir = request.Order.Count > 0
                    ? (request.Order[0].Dir ?? "desc")
                    : "desc";

                query = sortColumn.ToLowerInvariant() switch
                {
                    "datarecebimento" => sortDir == "desc" ? query.OrderByDescending(c => c.DataRecebimento) : query.OrderBy(c => c.DataRecebimento),
                    "instituicao" => sortDir == "desc" ? query.OrderByDescending(c => c.Instituicao.Sigla) : query.OrderBy(c => c.Instituicao.Sigla),
                    "paciente" => sortDir == "desc" ? query.OrderByDescending(c => c.Paciente.NomePaciente) : query.OrderBy(c => c.Paciente.NomePaciente),
                    "valor" => sortDir == "desc" ? query.OrderByDescending(c => c.ValorTotal) : query.OrderBy(c => c.ValorTotal),
                    "origem" => sortDir == "desc" ? query.OrderByDescending(c => c.Origem) : query.OrderBy(c => c.Origem),
                    _ => sortDir == "desc" ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id)
                };

                var data = await query
                    .Skip(start)
                    .Take(length)
                    .ToListAsync();

                var result = data.Select(c => (object)new
                {
                    id = c.Id,
                    dataRecebimento = c.DataRecebimento.ToString("dd/MM/yyyy"),
                    instituicao = $"{c.Instituicao?.Sigla ?? ""} - {c.Instituicao?.Nome ?? ""}".Trim(' ', '-'),
                    paciente = c.Paciente?.NomePaciente ?? "",
                    periodo = c.PeriodoFaturamento ?? "",
                    valorTotal = c.ValorTotal.ToString("N2"),
                    origem = c.Origem == 1 ? "Portaria" : "Faturamento",
                    status = c.Status == 1 ? "Recebido" : "Pendente",
                    acoes = $"<button class='btn btn-sm btn-info btn-detalhes' data-id='{c.Id}'><i class='fa-solid fa-eye'></i></button>"
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
                _eventLogHelper.LogEventViewer("[CatalogoRecebimentos] ListarCatalogos - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        /// <summary>
        /// Retorna detalhes de um catálogo (exames e formas de pagamento).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CatalogoRecebimentos/ObterDetalhes/{id:int}")]
        public async Task<IActionResult> ObterDetalhes(int id)
        {
            try
            {
                var catalogo = await _db.CatalogoRecebimentos
                    .AsNoTracking()
                    .Include(c => c.Instituicao)
                    .Include(c => c.Paciente)
                    .Include(c => c.CatalogoRecebimentosExames)
                        .ThenInclude(e => e.ExameRealizado)
                    .Include(c => c.CatalogoRecebimentosFormas)
                        .ThenInclude(f => f.FormaRecebimento)
                    .Include(c => c.CatalogoRecebimentosFormas)
                        .ThenInclude(f => f.ContaRecebimento)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (catalogo == null)
                    return Json(new { sucesso = false, mensagem = "Catálogo não encontrado." });

                var exames = catalogo.CatalogoRecebimentosExames.Select(e => new
                {
                    e.ExameRealizadoId,
                    e.ExameRealizado.Sequencial,
                    e.Valor
                }).ToList();

                var formas = catalogo.CatalogoRecebimentosFormas.Select(f => new
                {
                    f.FormaRecebimentoId,
                    formaNome = f.FormaRecebimento?.Nome ?? "",
                    f.ContaRecebimentoId,
                    contaNome = f.ContaRecebimento?.Nome ?? "",
                    f.Valor,
                    dataRecebimento = f.DataRecebimento.ToString("dd/MM/yyyy"),
                    f.Observacao
                }).ToList();

                return Json(new
                {
                    sucesso = true,
                    catalogo = new
                    {
                        catalogo.Id,
                        catalogo.Origem,
                        origemDescricao = catalogo.Origem == 1 ? "Portaria" : "Faturamento",
                        catalogo.InstituicaoId,
                        instituicao = $"{catalogo.Instituicao?.Sigla ?? ""} - {catalogo.Instituicao?.Nome ?? ""}".Trim(' ', '-'),
                        catalogo.PacienteId,
                        paciente = catalogo.Paciente?.NomePaciente ?? "",
                        catalogo.PeriodoFaturamento,
                        catalogo.ValorTotal,
                        dataRecebimento = catalogo.DataRecebimento.ToString("dd/MM/yyyy"),
                        status = catalogo.Status == 1 ? "Recebido" : "Pendente",
                        catalogo.Observacao,
                        catalogo.UsuarioRegistro,
                        dataRegistro = catalogo.DataRegistro.ToString("dd/MM/yyyy HH:mm")
                    },
                    exames,
                    formas
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[CatalogoRecebimentos] ObterDetalhes - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao carregar detalhes do catálogo." });
            }
        }

        #endregion

        #region Relatório

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CatalogoRecebimentos/Relatorio")]
        public async Task<IActionResult> Relatorio()
        {
            var model = new vmCatalogoRecebimentoFiltroRelatorio
            {
                Instituicoes = await CarregarInstituicoes(),
                FormasRecebimento = await CarregarFormas(),
                ContasRecebimento = await CarregarContas()
            };

            ViewBag.TextoMenu = new object[] { "Relatório de Recebimentos", false };
            return View(model);
        }

        /// <summary>
        /// Gera relatório do Catálogo de Recebimentos em PDF, HTML ou Word.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CatalogoRecebimentos/GerarRelatorio")]
        public async Task<IActionResult> GerarRelatorio(vmCatalogoRecebimentoFiltroRelatorio filtro)
        {
            if (filtro.DataFim < filtro.DataIni)
            {
                TempData["MensagemErro"] = "Data Final precisa ser maior ou igual que Data Inicial.";
                await CarregarListasRelatorioAsync(filtro);
                return View("Relatorio", filtro);
            }

            var dados = await MontarDadosRelatorioAsync(filtro);
            var empresa = await _db.Empresa.AsNoTracking().FirstOrDefaultAsync();
            string nomeBase = $"CatalogoRecebimentos_{filtro.DataIni:ddMMyyyy}_a_{filtro.DataFim:ddMMyyyy}";

            return filtro.FormatoSaida switch
            {
                1 => GerarRespostaHtml(dados, empresa, nomeBase),
                2 => GerarRespostaWord(dados, empresa, nomeBase),
                _ => GerarRespostaPdf(dados, empresa, nomeBase)
            };
        }

        private FileResult GerarRespostaPdf(DadosPdfCatalogoRecebimentos dados, Empresa? empresa, string nomeBase)
        {
            var gerador = new GeradorPdfCatalogoRecebimentos();
            byte[] bytes = gerador.Gerar(dados, empresa);
            return File(bytes, "application/pdf", $"{nomeBase}.pdf");
        }

        private FileResult GerarRespostaHtml(DadosPdfCatalogoRecebimentos dados, Empresa? empresa, string nomeBase)
        {
            var gerador = new GeradorHtmlCatalogoRecebimentos();
            byte[] bytes = gerador.Gerar(dados, empresa);
            return File(bytes, "text/html; charset=utf-8", $"{nomeBase}.html");
        }

        private FileResult GerarRespostaWord(DadosPdfCatalogoRecebimentos dados, Empresa? empresa, string nomeBase)
        {
            var gerador = new GeradorWordCatalogoRecebimentos();
            byte[] bytes = gerador.Gerar(dados, empresa);
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{nomeBase}.docx");
        }

        private async Task<DadosPdfCatalogoRecebimentos> MontarDadosRelatorioAsync(vmCatalogoRecebimentoFiltroRelatorio filtro)
        {
            var dataFimAjustada = filtro.DataFim.Date.AddDays(1).AddTicks(-1);

            var query = _db.CatalogoRecebimentos
                .AsNoTracking()
                .Include(c => c.Instituicao)
                .Include(c => c.Paciente)
                .Include(c => c.CatalogoRecebimentosExames)
                .Include(c => c.CatalogoRecebimentosFormas)
                    .ThenInclude(f => f.FormaRecebimento)
                .Include(c => c.CatalogoRecebimentosFormas)
                    .ThenInclude(f => f.ContaRecebimento)
                .Where(c => c.DataRecebimento >= filtro.DataIni.Date && c.DataRecebimento <= dataFimAjustada)
                .AsQueryable();

            if (filtro.InstituicaoId.HasValue && filtro.InstituicaoId.Value > 0)
                query = query.Where(c => c.InstituicaoId == filtro.InstituicaoId.Value);

            if (filtro.FormaRecebimentoId.HasValue && filtro.FormaRecebimentoId.Value > 0)
                query = query.Where(c => c.CatalogoRecebimentosFormas.Any(f => f.FormaRecebimentoId == filtro.FormaRecebimentoId.Value));

            if (filtro.ContaRecebimentoId.HasValue && filtro.ContaRecebimentoId.Value > 0)
                query = query.Where(c => c.CatalogoRecebimentosFormas.Any(f => f.ContaRecebimentoId == filtro.ContaRecebimentoId.Value));

            query = filtro.Ordenacao switch
            {
                1 => query.OrderBy(c => c.Instituicao.Sigla).ThenBy(c => c.DataRecebimento),
                2 => query.OrderBy(c => c.CatalogoRecebimentosFormas.First().FormaRecebimento.Nome).ThenBy(c => c.DataRecebimento),
                _ => query.OrderBy(c => c.DataRecebimento).ThenBy(c => c.Id)
            };

            var catalogos = await query.ToListAsync();

            var dados = new DadosPdfCatalogoRecebimentos
            {
                DataIni = filtro.DataIni,
                DataFim = filtro.DataFim,
                Ordenacao = filtro.Ordenacao
            };

            foreach (var c in catalogos)
            {
                var rec = new RecebimentoCatalogoDto
                {
                    CatalogoId = c.Id,
                    DataRecebimento = c.DataRecebimento,
                    Origem = c.Origem == 1 ? "Portaria" : "Faturamento",
                    SiglaInstituicao = c.Instituicao?.Sigla ?? "",
                    NomeInstituicao = c.Instituicao?.Nome ?? "",
                    NomePaciente = c.Paciente?.NomePaciente ?? "",
                    PeriodoFaturamento = c.PeriodoFaturamento,
                    ValorTotal = c.ValorTotal,
                    Observacao = c.Observacao,
                    Formas = c.CatalogoRecebimentosFormas.Select(f => new FormaRecebimentoCatalogoDto
                    {
                        FormaNome = f.FormaRecebimento?.Nome ?? "",
                        ContaNome = f.ContaRecebimento?.Nome ?? "",
                        Valor = f.Valor,
                        DataRecebimento = f.DataRecebimento,
                        Observacao = f.Observacao
                    }).ToList(),
                    Exames = c.CatalogoRecebimentosExames.Select(e => new ExameCatalogoDto
                    {
                        ExameRealizadoId = e.ExameRealizadoId,
                        Sequencial = e.ExameRealizado?.Sequencial ?? 0,
                        Valor = e.Valor
                    }).ToList()
                };

                dados.Recebimentos.Add(rec);
            }

            dados.TotaisPorForma = dados.Recebimentos
                .SelectMany(r => r.Formas)
                .GroupBy(f => f.FormaNome)
                .Select(g => new TotalCatalogoDto { Descricao = g.Key, Valor = g.Sum(f => f.Valor) })
                .OrderBy(t => t.Descricao)
                .ToList();

            dados.TotaisPorConta = dados.Recebimentos
                .SelectMany(r => r.Formas)
                .GroupBy(f => f.ContaNome)
                .Select(g => new TotalCatalogoDto { Descricao = g.Key, Valor = g.Sum(f => f.Valor) })
                .OrderBy(t => t.Descricao)
                .ToList();

            return dados;
        }

        private async Task CarregarListasRelatorioAsync(vmCatalogoRecebimentoFiltroRelatorio model)
        {
            model.Instituicoes = await CarregarInstituicoes();
            model.FormasRecebimento = await CarregarFormas();
            model.ContasRecebimento = await CarregarContas();
        }

        #endregion

        #region Helpers

        private async Task<List<SelectListItem>> CarregarInstituicoes()
        {
            return await _db.Instituicao
                .AsNoTracking()
                .Where(i => !string.IsNullOrEmpty(i.Sigla))
                .OrderBy(i => i.Sigla)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Sigla} - {i.Nome}"
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> CarregarFormas()
        {
            return await _db.FormasRecebimento
                .AsNoTracking()
                .Where(f => f.Ativo)
                .OrderBy(f => f.Nome)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Nome
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> CarregarContas()
        {
            return await _db.ContasRecebimento
                .AsNoTracking()
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nome)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Nome
                })
                .ToListAsync();
        }

        #endregion
    }
}
