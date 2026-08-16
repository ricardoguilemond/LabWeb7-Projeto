using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ExpressionCombiner;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Strategy;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using static BLL.UtilBLL;
using static LabWebMvc.MVC.UtilHelper.TrataExcecoes;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class PacientesController : BaseController
    {
        private readonly IMemoryCache _cache;

        public PacientesController(IDbFactory dbFactory, 
                                   IValidadorDeSessao validador, 
                                   GeralController geralController, 
                                   IEventLogHelper eventLogHelper, 
                                   Imagem imagem,
                                   ExclusaoService exclusaoService,
                                   IConnectionService connectionService,
                                   IMemoryCache cache)
               : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _cache = cache;
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

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Pacientes")]
        public IActionResult Index(string? nomePaciente = null, string? cpf = null, string? dataNascimento = null)
        {
            MontaControllers("IncluirPaciente", "Pacientes");

            // Os dados do grid são carregados via AJAX pelo DataTables (server-side processing).
            // A view recebe apenas os filtros iniciais para repassar ao endpoint Listar.
            ViewBag.NomePaciente = nomePaciente ?? string.Empty;
            ViewBag.Cpf = cpf ?? string.Empty;
            ViewBag.DataNascimento = dataNascimento ?? string.Empty;

            ViewBag.TextoMenu = new object[] { "Cadastro de Pacientes", false };
            return View(new vmPacientes());
        }

        /// <summary>
        /// Endpoint server-side do DataTables para o cadastro de pacientes.
        /// Carrega blocos de 100 registros do banco (cache de curta duração) e
        /// devolve a página solicitada de 10 em 10, mantendo a navegação rápida
        /// nas 10 primeiras páginas sem perder acesso aos demais registros.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Pacientes/Listar")]
        public async Task<IActionResult> Listar(
            [FromForm] DataTableRequest request,
            string? nomePaciente = null,
            string? cpf = null,
            string? dataNascimento = null)
        {
            try
            {
                int draw = request.Draw;
                int start = request.Start;
                int length = Math.Max(request.Length, 10);
                string searchValue = request.Search?.Value?.Trim() ?? string.Empty;

                const int blockSize = 100;
                int blockIndex = start / blockSize;
                int blockStart = blockIndex * blockSize;

                string sortColumn = request.Order.Count > 0 && request.Order[0].Column < request.Columns.Count
                    ? (request.Columns[request.Order[0].Column].Data ?? "id")
                    : "id";
                string sortDir = request.Order.Count > 0
                    ? (request.Order[0].Dir ?? "desc")
                    : "desc";

                string cacheKey = BuildCacheKey(nomePaciente, cpf, dataNascimento, searchValue, sortColumn, sortDir, blockIndex);

                if (!_cache.TryGetValue(cacheKey, out List<Pacientes>? blockData) || blockData == null)
                {
                    blockData = await LoadBlockAsync(nomePaciente, cpf, dataNascimento, searchValue, sortColumn, sortDir, blockStart, blockSize);
                    _cache.Set(cacheKey, blockData, TimeSpan.FromMinutes(5));
                }

                int recordsTotal = await CountTotalAsync(nomePaciente, cpf, dataNascimento, searchValue);

                int skipInBlock = start - blockStart;
                var pageData = blockData.Skip(skipInBlock).Take(length).ToList();

                List<object> result = pageData.Select(item => (object)new
                {
                    id = item.Id,
                    idPacienteExterno = item.IdPacienteExterno ?? string.Empty,
                    nomePaciente = item.NomePaciente ?? string.Empty,
                    nascimento = FormataData(item.Nascimento),
                    sexo = item.Sexo ?? string.Empty,
                    documento = item.TipoDocumento == 0 ? item.CPF.FormatarCPF() : (item.Identidade ?? string.Empty),
                    tipoDocumento = LabWebMvc.MVC.Areas.Utils.Utils.RetornaItem(item.TipoDocumento, "TipoDocumento"),
                    telefone = item.Telefone.FormataTelefone(),
                    acoes = BuildAcoes(item.Id)
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
                _eventLogHelper.LogEventViewer("[Pacientes] Listar - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        private string BuildCacheKey(string? nomePaciente, string? cpf, string? dataNascimento, string searchValue, string sortColumn, string sortDir, int blockIndex)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            string raw = $"{nomePaciente?.ToLowerInvariant()}|{cpf?.Replace(".", "").Replace("-", "")}|{dataNascimento}|{searchValue.ToLowerInvariant()}|{sortColumn}|{sortDir}|{blockIndex}";
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return "Pacientes_" + Convert.ToHexString(hash);
        }

        private async Task<List<Pacientes>> LoadBlockAsync(string? nomePaciente, string? cpf, string? dataNascimento, string searchValue, string sortColumn, string sortDir, int blockStart, int blockSize)
        {
            IQueryable<Pacientes> query = BuildBaseQuery(nomePaciente, cpf, dataNascimento, searchValue);
            query = ApplyOrdering(query, sortColumn, sortDir);
            return await query.Skip(blockStart).Take(blockSize).ToListAsync();
        }

        private async Task<int> CountTotalAsync(string? nomePaciente, string? cpf, string? dataNascimento, string searchValue)
        {
            IQueryable<Pacientes> query = BuildBaseQuery(nomePaciente, cpf, dataNascimento, searchValue);
            return await query.CountAsync();
        }

        private IQueryable<Pacientes> BuildBaseQuery(string? nomePaciente, string? cpf, string? dataNascimento, string searchValue)
        {
            var query = _db.Pacientes.AsNoTracking();

            // Filtros backend
            if (!string.IsNullOrEmpty(nomePaciente))
            {
                query = query.Where(p => p.NomePaciente.ToLower().Contains(nomePaciente.Trim().ToLower()));
            }

            if (!string.IsNullOrEmpty(cpf))
            {
                string cpfLimpo = cpf.Trim().Replace(".", "").Replace("-", "");
                query = query.Where(p => (p.CPF ?? "").Replace(".", "").Replace("-", "").Contains(cpfLimpo));
            }

            if (!string.IsNullOrEmpty(dataNascimento))
            {
                DateTime dataNascParsed = dataNascimento.Trim().FormataData("dd/MM/yyyy", true);
                var (inicioUtc, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataNascParsed);
                query = query.Where(p => p.Nascimento >= inicioUtc && p.Nascimento <= fimUtc);
            }

            // Busca global do DataTables
            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.FiltrarPorConteudo(searchValue,
                    x => x.CPF,
                    x => x.NomePaciente,
                    x => x.NomeSocial,
                    x => x.Endereco,
                    x => x.Bairro,
                    x => x.Cidade,
                    x => x.Id.ToString());

                if (searchValue.Split('/').Length == 3 || searchValue.Split('-').Length == 3)
                {
                    DateTime dataBusca = searchValue.Trim().FormataData("dd/MM/yyyy", true);
                    var (inicioUtc, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataBusca);
                    query = query.Where(l => l.Nascimento >= inicioUtc && l.Nascimento <= fimUtc);
                }
            }

            return query;
        }

        private IQueryable<Pacientes> ApplyOrdering(IQueryable<Pacientes> query, string sortColumn, string sortDir)
        {
            bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortColumn.ToLowerInvariant() switch
            {
                "nomepaciente" => desc ? query.OrderByDescending(p => p.NomePaciente) : query.OrderBy(p => p.NomePaciente),
                "nascimento" => desc ? query.OrderByDescending(p => p.Nascimento) : query.OrderBy(p => p.Nascimento),
                "sexo" => desc ? query.OrderByDescending(p => p.Sexo) : query.OrderBy(p => p.Sexo),
                "documento" => desc ? query.OrderByDescending(p => p.CPF) : query.OrderBy(p => p.CPF),
                "tipodocumento" => desc ? query.OrderByDescending(p => p.TipoDocumento) : query.OrderBy(p => p.TipoDocumento),
                "telefone" => desc ? query.OrderByDescending(p => p.Telefone) : query.OrderBy(p => p.Telefone),
                _ => desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
            };
        }

        private static string BuildAcoes(int id)
        {
            return $"<a id='{id}' class='grid_itens' onclick=clickConsulta(this) title='Consultar'><i class='fa-sharp fa-solid fa-display'></i> </a>" +
                   $"<a id='{id}' class='grid_itens' onclick=clickExames(this) title='Exames Realizados'><i class='fa-sharp fa-solid fa-file-medical'></i> </a>" +
                   $"<a id='{id}' class='grid_itens' onclick=clickAlterar(this) title='Alterar'><i class='fa-sharp fa-solid fa-file-pen'></i> </a>" +
                   $"<a id='{id}' class='grid_itens' onclick=clickDelete(this) title='Excluir'><i class='fa-sharp fa-solid fa-trash-can'></i> </a>";
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirPaciente")]
        public IActionResult IncluirPaciente()
        {
            var vm = new vmPacientes
            {
                SessionUF = HttpContext.Session.GetString("SessionUF") ?? ""
            };
            ViewBag.TextoMenu = new object[] { "Cadastro de Pacientes", false };
            return View(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirPaciente")]
        public async Task<IActionResult> SalvarPaciente(vmPacientes obj)
        {
            string redirecionaUrl = "Pacientes".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(obj.NomePaciente))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios" });

            Pacientes? pacientes = await _db.Pacientes.Where(s => (s.Email == obj.Email && (!string.IsNullOrEmpty(obj.Email))) ||
                                                                  (s.NomePaciente == obj.NomePaciente && (s.CPF == obj.CPF || s.Identidade == obj.CPF)) ||
                                                                  (s.NomePaciente == obj.NomePaciente && s.Nascimento == obj.Nascimento)).SingleOrDefaultAsync();
            if (pacientes != null)
            {
                if (pacientes.Email == obj.Email && !string.IsNullOrEmpty(obj.Email))
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Já existe paciente cadastrado com este e-mail", action = "", sucesso = false });
                else if (pacientes.NomePaciente == obj.NomePaciente.ToUpper() && pacientes.Nascimento == obj.Nascimento)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Paciente já cadastrado (nome e data nascimento)", action = "", sucesso = false });
                else
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Já existe paciente cadastrado com este documento", action = "", sucesso = false });
            }

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        Pacientes paciente = new Pacientes();

                        /* TipoDocumento --> Controlando o tipo de documento que será gravado para o paciente.
                           Define quando será gravado CPF ou outro documento qualquer.
                           Verificar método ListaDocumento() em UtilsBase.cs
                        */
                        paciente.TipoDocumento = obj.vmGeral.TipoDocumento;
                        //Salva o CPF no CPF, e salva na Identidade qualquer outro tipo de documento!
                        paciente.CPF = obj.vmGeral.TipoDocumento == 0 ? obj.CPF?.CPFSemFormatacao() : string.Empty;
                        paciente.Identidade = obj.vmGeral.TipoDocumento > 0 ? obj.CPF?.CPFSemFormatacao() : string.Empty;
                        paciente.Emissor = obj.vmGeral.TipoOrgaoEmissor;

                        //Colunas NÃO nulas:
                        paciente.NomePaciente = obj.NomePaciente.ToUpper();
                        // Nascimento e DUM são timestamptz — o model binder gera Kind=Unspecified
                        // que o Npgsql 8.x rejeita. Converte para UTC antes de gravar.
                        paciente.Nascimento = _geralController.ConverterLocalParaUtc(obj.Nascimento);
                        paciente.EstadoCivil = obj.EstadoCivil; // obj.vmGeral.TipoEstadoCivil;
                        paciente.TempoGestacao = obj.vmGeral.TipoTempoGestacao;
                        paciente.DataEntrada = _geralController.ObterDataHoraUtc();
                        paciente.DataRegistro = _geralController.ObterDataHoraUtc();
                        paciente.StatusBaixa = 0;
                        paciente.IdPacienteExterno = obj.IdPacienteExterno;

                        //Endereçamento e outros dados que aceitam nulos:
                        paciente.CarteiraSUS = obj.CarteiraSUS;
                        paciente.Complemento = obj.Complemento;
                        paciente.DUM = obj.DUM.HasValue ? _geralController.ConverterLocalParaUtc(obj.DUM.Value) : null;
                        paciente.Email = obj.Email;
                        paciente.CEP = obj.CEP;
                        paciente.Logradouro = obj.Logradouro.ToCapitalize();
                        paciente.Endereco = obj.Endereco.ToCapitalize();
                        paciente.Numero = obj.Numero;
                        paciente.Bairro = obj.Bairro.ToCapitalize();
                        paciente.Cidade = obj.Cidade.ToCapitalize();
                        paciente.UF = obj.vmGeral.TipoUF;

                        // Outros dados que aceitam nulos:
                        paciente.Nacionalidade = obj.Nacionalidade.ToCapitalize();
                        paciente.Naturalidade = obj.Naturalidade.ToCapitalize();
                        paciente.NomeMae = obj.NomeMae.Upper();
                        paciente.NomePai = obj.NomePai.Upper();
                        paciente.NomeSocial = obj.NomeSocial.Upper();
                        paciente.Observacao = obj.Observacao;
                        paciente.Profissao = obj.Profissao.ToCapitalize();
                        paciente.Sexo = obj.Sexo;
                        paciente.Telefone = obj.Telefone;
                        paciente.TipoSanguineo = obj.TipoSanguineo;

                        await _db.Pacientes.AddAsync(paciente);

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Paciente foi salvo", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("ERRO: Paciente não foi salvo CPF: " + obj.CPF, "wError");

                        TrataExceptionViewer(ex, _db);

                        return Json(new
                        {
                            titulo = MensagensError_pt_BR.ErroFalhou,
                            mensagem = $"Paciente NÃO foi salvo",
                            action = "",
                            sucesso = false
                        });
                    }
                }
            });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarPaciente")]
        public async Task<IActionResult> AlterarPaciente(vmPacientes vm, int id)
        {
            Pacientes dados = await _db.Pacientes.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.TipoDocumento = dados.TipoDocumento;
                vm.CPF = dados.TipoDocumento == 0 ? dados.CPF : dados.Identidade;
                vm.Identidade = dados.Identidade;
                vm.Emissor = dados.Emissor;
                vm.NomePaciente = dados.NomePaciente;
                vm.Nascimento = dados.Nascimento;
                vm.EstadoCivil = dados.EstadoCivil;
                vm.TempoGestacao = dados.TempoGestacao;
                vm.StatusBaixa = dados.StatusBaixa;
                vm.Bairro = dados.Bairro;
                vm.CarteiraSUS = dados.CarteiraSUS;
                vm.CEP = dados.CEP;
                vm.Cidade = dados.Cidade;
                vm.Complemento = dados.Complemento;
                vm.DUM = dados.DUM;
                vm.Email = dados.Email;
                vm.Endereco = dados.Endereco;
                vm.IdPacienteExterno = dados.IdPacienteExterno;
                vm.Logradouro = dados.Logradouro;
                vm.Nacionalidade = dados.Nacionalidade;
                vm.Naturalidade = dados.Naturalidade;
                vm.NomeMae = dados.NomeMae;
                vm.NomePai = dados.NomePai;
                vm.NomeSocial = dados.NomeSocial;
                vm.Numero = dados.Numero;
                vm.Observacao = dados.Observacao;
                vm.Profissao = dados.Profissao;
                vm.Sexo = dados.Sexo;
                vm.Telefone = dados.Telefone;
                vm.TipoSanguineo = dados.TipoSanguineo;
                vm.UF = dados.UF;
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new vmGeral()
                {
                    TipoDocumento = dados.TipoDocumento,
                    TipoGenero = dados.Sexo,
                    TipoOrgaoEmissor = dados.Emissor,
                    TipoEstadoCivil = dados.EstadoCivil,
                    TipoUF = dados.UF,
                    TipoTempoGestacao = dados.TempoGestacao
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis via ViewModel tipado
                 */
                vm.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Alterar Cadastro de Pacientes", false };
            //Finalização da View
            _geralController.Validacao("AlterarPaciente,Pacientes", ViewBag.TextoMenu[0]);
            return View(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarPaciente")]
        public async Task<IActionResult> SalvarAlteracaoPaciente(vmPacientes vm, int id)
        {
            string redirecionaUrl = "Pacientes".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.NomePaciente))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios" });

            Pacientes? paciente = await _db.Pacientes.Where(s => s.Id == id).SingleOrDefaultAsync();
            if (paciente == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Não foi possível salvar o registro neste momento", action = "", sucesso = false });

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        paciente.TipoDocumento = vm.vmGeral.TipoDocumento;
                        //Salva o CPF no CPF, e salva na Identidade qualquer outro tipo de documento!
                        paciente.CPF = vm.vmGeral.TipoDocumento == 0 ? vm.CPF?.CPFSemFormatacao() : string.Empty;
                        paciente.Identidade = vm.vmGeral.TipoDocumento > 0 ? vm.CPF?.CPFSemFormatacao() : string.Empty;
                        paciente.Emissor = vm.vmGeral.TipoOrgaoEmissor;

                        //Colunas NÃO nulas:
                        paciente.NomePaciente = vm.NomePaciente.ToUpper();
                        paciente.Nascimento = _geralController.ConverterLocalParaUtc(vm.Nascimento);
                        paciente.EstadoCivil = vm.EstadoCivil;
                        paciente.TempoGestacao = vm.vmGeral.TipoTempoGestacao;

                        //Colunas que aceitam nulas:
                        paciente.Bairro = vm.Bairro.ToCapitalize();
                        paciente.CarteiraSUS = vm.CarteiraSUS;
                        paciente.CEP = vm.CEP;
                        paciente.Cidade = vm.Cidade.ToCapitalize();
                        paciente.Complemento = vm.Complemento;
                        paciente.DUM = vm.DUM.HasValue ? _geralController.ConverterLocalParaUtc(vm.DUM.Value) : null;
                        paciente.Email = vm.Email;
                        paciente.Endereco = vm.Endereco.ToCapitalize();
                        paciente.IdPacienteExterno = vm.IdPacienteExterno;
                        paciente.Logradouro = vm.Logradouro.ToCapitalize();
                        paciente.Nacionalidade = vm.Nacionalidade.ToCapitalize();
                        paciente.Naturalidade = vm.Naturalidade.ToCapitalize();
                        paciente.NomeMae = vm.NomeMae.Upper();
                        paciente.NomePai = vm.NomePai.Upper();
                        paciente.NomeSocial = vm.NomeSocial.Upper();
                        paciente.Numero = vm.Numero;
                        paciente.Observacao = vm.Observacao;
                        paciente.Profissao = vm.Profissao.ToCapitalize();
                        paciente.Sexo = vm.Sexo;
                        paciente.Telefone = vm.Telefone;
                        paciente.TipoSanguineo = vm.TipoSanguineo;
                        paciente.UF = vm.vmGeral.TipoUF;

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Paciente foi salvo", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("ERRO: Paciente não foi salvo CNPJ: " + vm.CPF, "wError");

                        TrataExceptionViewer(ex, _db);

                        return Json(new
                        {
                            titulo = MensagensError_pt_BR.ErroFalhou,
                            mensagem = $"Paciente NÃO foi salvo",
                            action = "",
                            sucesso = false
                        });
                    }
                }
            });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirPaciente")]
        public async Task<IActionResult> ExcluirPaciente(int id)
        {
            //Feito pelo Kiro em 20/04/2026
            // Verifica se o paciente possui exames vinculados antes de excluir
            //Feito pelo Qoder em 12/08/2026 — removido _db.Requisitar.AnyAsync (tabela eliminada)
            bool possuiVinculos = await _db.ExamesRealizados.AnyAsync(e => e.PacienteId == id)
                               || await _db.ExamesPendentes.AnyAsync(e => e.PacienteId == id);

            if (possuiVinculos)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Paciente possui exames vinculados e não pode ser excluído", action = "", sucesso = false });
            //..Kiro

            // Excluindo um registro da tabela Pacientes
            DeleteContext<Pacientes> context = new DeleteContext<Pacientes>(new DeleteStrategy<Pacientes>(_db));
            JsonResult result = await context.DeleteRecordAsync(id, "Pacientes");
            return result;
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarPaciente")]
        public async Task<ActionResult> ConsultarPaciente(vmPacientes vm, int id)
        {
            Pacientes dados = await _db.Pacientes.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.TipoDocumento = dados.TipoDocumento;
                vm.CPF = dados.TipoDocumento == 0 ? dados.CPF : dados.Identidade;
                vm.Identidade = dados.Identidade;
                vm.Emissor = dados.Emissor;
                vm.NomePaciente = dados.NomePaciente;
                vm.Nascimento = dados.Nascimento;
                vm.EstadoCivil = dados.EstadoCivil;
                vm.TempoGestacao = dados.TempoGestacao;
                vm.StatusBaixa = dados.StatusBaixa;
                vm.Bairro = dados.Bairro;
                vm.CarteiraSUS = dados.CarteiraSUS;
                vm.CEP = dados.CEP;
                vm.Cidade = dados.Cidade;
                vm.Complemento = dados.Complemento;
                vm.DUM = dados.DUM;
                vm.Email = dados.Email;
                vm.Endereco = dados.Endereco;
                vm.IdPacienteExterno = dados.IdPacienteExterno;
                vm.Logradouro = dados.Logradouro;
                vm.Nacionalidade = dados.Nacionalidade;
                vm.Naturalidade = dados.Naturalidade;
                vm.NomeMae = dados.NomeMae;
                vm.NomePai = dados.NomePai;
                vm.NomeSocial = dados.NomeSocial;
                vm.Numero = dados.Numero;
                vm.Observacao = dados.Observacao;
                vm.Profissao = dados.Profissao;
                vm.Sexo = dados.Sexo;
                vm.Telefone = dados.Telefone;
                vm.TipoSanguineo = dados.TipoSanguineo;
                vm.UF = dados.UF;
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new vmGeral()
                {
                    TipoDocumento = dados.TipoDocumento,
                    TipoGenero = dados.Sexo,
                    TipoOrgaoEmissor = dados.Emissor,
                    TipoEstadoCivil = dados.EstadoCivil,
                    TipoUF = dados.UF,
                    TipoTempoGestacao = dados.TempoGestacao
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis via ViewModel tipado
                 */
                vm.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Consulta de Paciente", false };
            //Finalização para a View
            _geralController.Validacao("ConsultarPaciente,Pacientes", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Pacientes/ObterExamesPaciente")]
        public async Task<IActionResult> ObterExamesPaciente(int pacienteId, bool expandir = false,
            string? dataInicial = null, string? dataFinal = null)
        {
            try
            {
                bool temFiltroData = !string.IsNullOrEmpty(dataInicial) || !string.IsNullOrEmpty(dataFinal);

                if (temFiltroData)
                {
                    // Filtro por período informado no detail — exibe todos os exames do range
                    const int limiteMaximo = 200;
                    var query = _db.ExamesRealizados
                        .AsNoTracking()
                        .Where(e => e.PacienteId == pacienteId && e.Situacao >= 1);

                    if (!string.IsNullOrEmpty(dataInicial))
                    {
                        DateTime dataIniParsed = dataInicial.Trim().FormataData("dd/MM/yyyy", true);
                        var (inicioUtc, _) = _geralController.ConverterDataLocalParaRangeUtc(dataIniParsed);
                        query = query.Where(e => e.DataIni >= inicioUtc);
                    }

                    if (!string.IsNullOrEmpty(dataFinal))
                    {
                        DateTime dataFimParsed = dataFinal.Trim().FormataData("dd/MM/yyyy", true);
                        var (_, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataFimParsed);
                        query = query.Where(e => e.DataIni <= fimUtc);
                    }

                    int totalExamesFiltrados = await query.CountAsync();

                    var dadosFiltrados = await query
                        .OrderByDescending(e => e.DataIni)
                        .ThenByDescending(e => e.Id)
                        .Take(limiteMaximo)
                        .Include(e => e.Instituicao)
                        .Include(e => e.Postos)
                        .Include(e => e.Medicos)
                        .Include(e => e.ItensExamesRealizados)
                            .ThenInclude(i => i.ClasseExames)
                        .ToListAsync();

                    var examesFiltrados = dadosFiltrados.Select(e => new
                    {
                        e.Id,
                        MedicoId = e.MedicoId,
                        DataIni = e.DataIni.ToLocalString("dd/MM/yyyy"),
                        DataFim = e.DataFim != null ? e.DataFim.Value.ToLocalString("dd/MM/yyyy") : "",
                        SiglaInstituicao = e.Instituicao?.Sigla ?? "",
                        NomePosto = e.Postos != null
                            ? (e.Postos.SiglaPosto ?? "") + "-" + (e.Postos.NomePosto ?? "")
                            : "",
                        NomeMedico = (e.Medicos?.NomeMedico ?? "").Length > 25
                            ? (e.Medicos?.NomeMedico ?? "").Substring(0, 22) + "..."
                            : e.Medicos?.NomeMedico ?? "",
                        CRM = e.Medicos?.CRM ?? "",
                        Folha = e.ItensExamesRealizados
                            .FirstOrDefault()?.ClasseExames?.RefExame ?? "",
                        Itens = e.ItensExamesRealizados
                            .OrderBy(i => i.OrdemItem)
                            .Select(i => new
                            {
                                Folha = i.ClasseExames?.RefExame ?? "",
                                i.RefExame,
                                i.RefItem,
                                ContaExame = i.ContaExame.FormatarContaExameSem11(),
                                Descricao = i.Descricao ?? ""
                            }).ToList()
                    }).ToList();

                    int examesOcultosFiltrados = totalExamesFiltrados > limiteMaximo ? totalExamesFiltrados - limiteMaximo : 0;
                    return Json(new { sucesso = true, exames = examesFiltrados, totalExames = totalExamesFiltrados, examesOcultos = examesOcultosFiltrados });
                }

                if (expandir)
                {
                    // Modo expandido: exames dos últimos 12 meses (sem limite de quantidade)
                    DateTime dataLimite12Meses = DateTime.UtcNow.AddMonths(-12);

                    var dadosExpandidos = await _db.ExamesRealizados
                        .AsNoTracking()
                        .Where(e => e.PacienteId == pacienteId && e.Situacao >= 1 && e.DataIni >= dataLimite12Meses)
                        .OrderByDescending(e => e.DataIni)
                        .ThenByDescending(e => e.Id)
                        .Include(e => e.Instituicao)
                        .Include(e => e.Postos)
                        .Include(e => e.Medicos)
                        .Include(e => e.ItensExamesRealizados)
                            .ThenInclude(i => i.ClasseExames)
                        .ToListAsync();

                    var examesExpandidos = dadosExpandidos.Select(e => new
                    {
                        e.Id,
                        MedicoId = e.MedicoId,
                        DataIni = e.DataIni.ToLocalString("dd/MM/yyyy"),
                        DataFim = e.DataFim != null ? e.DataFim.Value.ToLocalString("dd/MM/yyyy") : "",
                        SiglaInstituicao = e.Instituicao?.Sigla ?? "",
                        NomePosto = e.Postos != null
                            ? (e.Postos.SiglaPosto ?? "") + "-" + (e.Postos.NomePosto ?? "")
                            : "",
                        NomeMedico = (e.Medicos?.NomeMedico ?? "").Length > 25
                            ? (e.Medicos?.NomeMedico ?? "").Substring(0, 22) + "..."
                            : e.Medicos?.NomeMedico ?? "",
                        CRM = e.Medicos?.CRM ?? "",
                        Folha = e.ItensExamesRealizados
                            .FirstOrDefault()?.ClasseExames?.RefExame ?? "",
                        Itens = e.ItensExamesRealizados
                            .OrderBy(i => i.OrdemItem)
                            .Select(i => new
                            {
                                Folha = i.ClasseExames?.RefExame ?? "",
                                i.RefExame,
                                i.RefItem,
                                ContaExame = i.ContaExame.FormatarContaExameSem11(),
                                Descricao = i.Descricao ?? ""
                            }).ToList()
                    }).ToList();

                    return Json(new { sucesso = true, exames = examesExpandidos, totalExames = examesExpandidos.Count, examesOcultos = 0 });
                }

                // Regra de Exibição Inteligente:
                // ExamesExibidos = MAX(últimos 4, exames nos últimos 90 dias) LIMIT 8
                const int minimoExames = 4;
                const int maximoExames = 8;
                const int diasJanela = 90;

                // 1. Buscar os últimos 8 exames do paciente (limite máximo no banco)
                var ultimos8 = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => e.PacienteId == pacienteId && e.Situacao >= 1)
                    .OrderByDescending(e => e.DataIni)
                    .ThenByDescending(e => e.Id)
                    .Take(maximoExames)
                    .Include(e => e.Instituicao)
                    .Include(e => e.Postos)
                    .Include(e => e.Medicos)
                    .Include(e => e.ItensExamesRealizados)
                        .ThenInclude(i => i.ClasseExames)
                    .ToListAsync();

                // 2. Aplicar regra adaptativa em memória (sobre max 8 registros)
                // Regra: MAX(últimos 4, exames nos últimos 90 dias) LIMIT 8
                // - Pegar todos os exames dos últimos 90 dias dentre os 8 buscados
                // - Se forem menos de 4, completar com os mais recentes até ter 4
                DateTime dataLimite90Dias = DateTime.UtcNow.AddDays(-diasJanela);
                var examesDentro90Dias = ultimos8.Where(e => e.DataIni >= dataLimite90Dias).ToList();

                List<ExamesRealizados> examesExibidos;
                if (examesDentro90Dias.Count >= minimoExames)
                {
                    // Já tem 4+ dentro dos 90 dias — exibir todos (max 8 já garantido pelo Take)
                    examesExibidos = examesDentro90Dias;
                }
                else
                {
                    // Menos de 4 dentro dos 90 dias — garantir mínimo de 4 (pegar os mais recentes)
                    examesExibidos = ultimos8.Take(minimoExames).ToList();
                }

                // 3. Contar total para indicador de ocultos
                int totalExames = await _db.ExamesRealizados
                    .Where(e => e.PacienteId == pacienteId && e.Situacao >= 1)
                    .CountAsync();

                int examesOcultos = totalExames - examesExibidos.Count;

                // 4. Projetar resultado
                var exames = examesExibidos.Select(e => new
                {
                    e.Id,
                    MedicoId = e.MedicoId,
                    DataIni = e.DataIni.ToLocalString("dd/MM/yyyy"),
                    DataFim = e.DataFim != null ? e.DataFim.Value.ToLocalString("dd/MM/yyyy") : "",
                    SiglaInstituicao = e.Instituicao?.Sigla ?? "",
                    NomePosto = e.Postos != null
                        ? (e.Postos.SiglaPosto ?? "") + "-" + (e.Postos.NomePosto ?? "")
                        : "",
                    NomeMedico = (e.Medicos?.NomeMedico ?? "").Length > 25
                        ? (e.Medicos?.NomeMedico ?? "").Substring(0, 22) + "..."
                        : e.Medicos?.NomeMedico ?? "",
                    CRM = e.Medicos?.CRM ?? "",
                    Folha = e.ItensExamesRealizados
                        .FirstOrDefault()?.ClasseExames?.RefExame ?? "",
                    Itens = e.ItensExamesRealizados
                        .OrderBy(i => i.OrdemItem)
                        .Select(i => new
                        {
                            Folha = i.ClasseExames?.RefExame ?? "",
                            i.RefExame,
                            i.RefItem,
                            ContaExame = i.ContaExame.FormatarContaExameSem11(),
                            Descricao = i.Descricao ?? ""
                        }).ToList()
                }).ToList();

                return Json(new { sucesso = true, exames, totalExames, examesOcultos });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Pacientes] ObterExamesPaciente - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao obter exames do paciente" });
            }
        }
        //..Kiro

        public IActionResult ConverterPdf()
        {
            try
            {
                //ConversoresPdf pdf = new ConversoresPdf();
                //pdf.ConverteHtmlToPdf(@"F:\Temp2\Arquivo.html");

                //pdf.ConverteHtmlToPdf();

                return Json(new { success = true, responseText = "Salvou com sucesso" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Pacientes] ConverterPdf: " + ex.Message, "wError");
                return Json(new { success = false, responseText = string.Format("{0} {1}", "Falha:", ex.Message) });
            }
        }

    }

    internal class CustomErrorModel
    {
        private string v;

        public CustomErrorModel(string v)
        {
            this.v = v;
        }
    }
}