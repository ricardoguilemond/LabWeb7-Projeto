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
using static LabWebMvc.MVC.Areas.Utils.Utils;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class InstituicoesController : BaseController
    {
        private readonly IPathHelper _pathHelper;
        private readonly IMemoryCache _cache;

        public InstituicoesController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IPathHelper pathHelper,
            IMemoryCache cache)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _pathHelper = pathHelper;
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
        [Route("Instituicoes")]
        public IActionResult Index()
        {
            MontaControllers("IncluirInstituicao", "Instituicoes");

            // Dados do grid carregados via AJAX pelo DataTables (server-side processing).
            ViewBag.TextoMenu = new object[] { "Cadastro de Instituições", false };
            return View(new vmInstituicao());
        }

        /// <summary>
        /// Endpoint server-side do DataTables para o cadastro de instituições.
        /// Carrega blocos de 100 registros do banco (cache de curta duração) e
        /// devolve a página solicitada de 10 em 10.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Instituicoes/Listar")]
        public async Task<IActionResult> Listar([FromForm] DataTableRequest request)
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

                string cacheKey = BuildCacheKey(searchValue, sortColumn, sortDir, blockIndex);

                if (!_cache.TryGetValue(cacheKey, out List<Instituicao>? blockData) || blockData == null)
                {
                    blockData = await LoadBlockAsync(searchValue, sortColumn, sortDir, blockStart, blockSize);
                    _cache.Set(cacheKey, blockData, TimeSpan.FromMinutes(5));
                }

                int recordsTotal = await CountTotalAsync(searchValue);

                int skipInBlock = start - blockStart;
                var pageData = blockData.Skip(skipInBlock).Take(length).ToList();

                List<object> result = pageData.Select(item => (object)new
                {
                    id = item.Id,
                    sigla = item.Sigla ?? string.Empty,
                    nome = item.Nome ?? string.Empty,
                    cnpj = item.CNPJ.FormatarCNPJNotNull(),
                    email = item.Email ?? string.Empty,
                    telefone = item.Telefone.FormataTelefoneNotNull(),
                    celular = item.Celular.FormataTelefone(),
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
                _eventLogHelper.LogEventViewer("[Instituicoes] Listar - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        private string BuildCacheKey(string searchValue, string sortColumn, string sortDir, int blockIndex)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            string raw = $"{searchValue.ToLowerInvariant()}|{sortColumn}|{sortDir}|{blockIndex}";
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return "Instituicoes_" + Convert.ToHexString(hash);
        }

        private async Task<List<Instituicao>> LoadBlockAsync(string searchValue, string sortColumn, string sortDir, int blockStart, int blockSize)
        {
            IQueryable<Instituicao> query = BuildBaseQuery(searchValue);
            query = ApplyOrdering(query, sortColumn, sortDir);
            return await query.Skip(blockStart).Take(blockSize).ToListAsync();
        }

        private async Task<int> CountTotalAsync(string searchValue)
        {
            return await BuildBaseQuery(searchValue).CountAsync();
        }

        private IQueryable<Instituicao> BuildBaseQuery(string searchValue)
        {
            var query = _db.Instituicao.AsNoTracking();

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.FiltrarPorConteudo(searchValue,
                    x => x.Nome!,
                    x => x.CNPJ,
                    x => x.Endereco,
                    x => x.Bairro,
                    x => x.Cidade,
                    x => x.Id.ToString());
            }

            return query;
        }

        private IQueryable<Instituicao> ApplyOrdering(IQueryable<Instituicao> query, string sortColumn, string sortDir)
        {
            bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortColumn.ToLowerInvariant() switch
            {
                "sigla" => desc ? query.OrderByDescending(p => p.Sigla) : query.OrderBy(p => p.Sigla),
                "nome" => desc ? query.OrderByDescending(p => p.Nome) : query.OrderBy(p => p.Nome),
                "cnpj" => desc ? query.OrderByDescending(p => p.CNPJ) : query.OrderBy(p => p.CNPJ),
                "email" => desc ? query.OrderByDescending(p => p.Email) : query.OrderBy(p => p.Email),
                "telefone" => desc ? query.OrderByDescending(p => p.Telefone) : query.OrderBy(p => p.Telefone),
                "celular" => desc ? query.OrderByDescending(p => p.Celular) : query.OrderBy(p => p.Celular),
                _ => desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
            };
        }

        private static string BuildAcoes(int id)
        {
            return $"<a id='{id}' class='grid_itens' onclick=clickConsulta(this) title='Consultar'><i class='fa-sharp fa-solid fa-display'></i> </a>" +
                   $"<a id='{id}' class='grid_itens' onclick=clickPostos(this) title='Postos e Anexos'><i class='fa-sharp fa-solid fa-file-medical'></i> </a>" +
                   $"<a id='{id}' class='grid_itens' onclick=clickAlterar(this) title='Alterar'><i class='fa-sharp fa-solid fa-file-pen'></i> </a>" +
                   $"<a id='{id}' class='grid_itens' onclick=clickDelete(this) title='Excluir'><i class='fa-sharp fa-solid fa-trash-can'></i> </a>";
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirInstituicao")]
        public IActionResult IncluirInstituicao()
        {
            var vm = new vmInstituicao
            {
                PathImages = Utils.Utils.GetLocalPathImagens()
            };
            ViewBag.TextoMenu = new object[] { "Cadastro de Instituições", false };
            return View(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirInstituicao")]
        public async Task<IActionResult> SalvarInstituicao(vmInstituicao vm)
        {
            string redirecionaUrl = "Instituicoes".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.Nome))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios ou não havia nada para ser salvo" });

            Instituicao? Instituicoes = await _db.Instituicao.Where(s => s.Nome == vm.Nome || s.Sigla == vm.Sigla || s.CNPJ == vm.CNPJ).SingleOrDefaultAsync();
            if (Instituicoes != null)
            {
                if (Instituicoes.Nome == vm.Nome.ToUpper())
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição já cadastrada com este nome", action = "", sucesso = false });
                else if (Instituicoes.Sigla == vm.Sigla.ToUpper())
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição já cadastrada com esta sigla", action = "", sucesso = false });
                else if (Instituicoes.CNPJ == vm.CNPJ)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição já cadastrada com este CNPJ", action = "", sucesso = false });
                else
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição já cadastrada", action = "", sucesso = false });
            }
            try
            {
                //capturando arquivos/upload ou arquivos de imagens em bytes[]
                GetImagemTimbre(vm);
                GetImagemLogomarca(vm);

                //Feito pelo Kiro em 20/04/2026
                Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            await _db.Instituicao.AddAsync(new Instituicao()
                            {
                                //Colunas NÃO nulas:
                                Nome = vm.Nome.ToUpper(),
                                Sigla = vm.Sigla.ToUpper(),
                                CNPJ = vm.CNPJ.CNPJSemFormatacao(),
                                Email = vm.Email.ToLower(),
                                Telefone = vm.Telefone,
                                Contato = vm.Contato,
                                CarimboSN = vm.CarimboSN,
                                TimbreSN = vm.TimbreSN,

                                //Colunas que aceitam nulas:
                                Endereco = vm.Endereco.ToCapitalize(),
                                Logradouro = vm.Logradouro.ToCapitalize(),
                                Numero = vm.Numero,
                                Bairro = vm.Bairro.ToCapitalize(),
                                Complemento = vm.Complemento,
                                Cidade = vm.Cidade.ToCapitalize(),
                                UF = vm.vmGeral.TipoUF,
                                CEP = vm.CEP,
                                Celular = vm.Celular,
                                Sequencial = vm.Sequencial,
                                TituloTimbre = vm.TituloTimbre != null ? vm.TituloTimbre.ToUpper() : string.Empty,
                                SubTituloTimbre = vm.SubTituloTimbre.ToCapitalize(),
                                UsuarioCaminhoFTP = vm.UsuarioCaminhoFTP,
                                UsuarioEmailFTP = vm.UsuarioEmailFTP,
                                UsuarioPortaFTP = vm.UsuarioPortaFTP,
                                UsuarioSenhaFTP = vm.UsuarioSenhaFTP,
                                ValorExameCitologia = vm.ValorExameCitologia,
                                Propaganda = vm.Propaganda,
                                AvisoRodape1 = vm.AvisoRodape1,
                                AvisoRodape2 = vm.AvisoRodape2,

                                /*
                                 * Gravando as imagens em bytes[] e nomes das imagens
                                 */
                                Timbre = vm.Timbre,
                                Logomarca = vm.Logomarca,
                                NomeTimbre = vm.NomeTimbre,
                                NomeLogomarca = vm.NomeLogomarca
                            });

                            await _db.SaveChangesAsync();

                            await transaction.CommitAsync();

                            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Instituição foi salva", action = "", sucesso = true });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();

                            _eventLogHelper.LogEventViewer("[Instituicoes] Inclusão - Erro: " + ex.Message, "wError");

                            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição NÃO foi salva", action = "", sucesso = false });
                        }
                    }
                });
                //..Kiro
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Instituicoes] Inclusão - TransactionAbortedException Message: {0} ::: " + ex.Message, "wError");
            }

            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Instituição foi salva", action = "", sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarInstituicao")]
        public async Task<IActionResult> AlterarInstituicao(vmInstituicao vm, int id)
        {
            string pathImages = Utils.Utils.GetLocalPathImagens();

            Instituicao dados = await _db.Instituicao.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.Nome = dados.Nome.ToUpper();
                vm.Sigla = dados.Sigla;
                vm.CNPJ = dados.CNPJ.CNPJSemFormatacao();
                vm.Email = dados.Email;
                vm.Endereco = dados.Endereco.ToCapitalize();
                vm.Logradouro = dados.Logradouro.ToCapitalize();
                vm.Numero = dados.Numero;
                vm.Bairro = dados.Bairro.ToCapitalize();
                vm.Complemento = dados.Complemento;
                vm.Cidade = dados.Cidade.ToCapitalize();
                vm.UF = dados.UF;
                vm.CEP = dados.CEP;
                vm.Telefone = dados.Telefone;
                vm.Celular = dados.Celular;
                vm.Sequencial = dados.Sequencial;
                vm.TituloTimbre = dados.TituloTimbre;
                vm.SubTituloTimbre = dados.SubTituloTimbre;
                vm.CarimboSN = dados.CarimboSN;
                vm.TimbreSN = dados.TimbreSN;
                vm.Contato = dados.Contato;
                vm.UsuarioCaminhoFTP = dados.UsuarioCaminhoFTP;
                vm.UsuarioEmailFTP = dados.UsuarioEmailFTP;
                vm.UsuarioPortaFTP = dados.UsuarioPortaFTP;
                vm.UsuarioSenhaFTP = dados.UsuarioSenhaFTP;
                vm.ValorExameCitologia = dados.ValorExameCitologia;
                vm.Propaganda = dados.Propaganda;
                vm.AvisoRodape1 = dados.AvisoRodape1;
                vm.AvisoRodape2 = dados.AvisoRodape2;
                /*
                 * Imagens
                 */
                vm.Timbre = dados.Timbre;
                vm.Logomarca = dados.Logomarca;
                vm.NomeTimbre = dados.NomeTimbre;
                vm.NomeLogomarca = dados.NomeLogomarca;
                vm.CaminhoImagemTimbre = pathImages;      //pasta que contém imagens para upload
                vm.CaminhoImagemLogomarca = pathImages;   //pasta que contém imagens para upload
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new()
                {
                    TipoUF = dados.UF
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis via ViewModel tipado
                 */
                vm.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares
            vm.PathImages = pathImages;
            ViewBag.TextoMenu = new object[] { "Alterar Cadastro de Instituições", false };
            //Finalização da View
            _geralController.Validacao("AlterarInstituicao,Instituicoes", ViewBag.TextoMenu[0]);
            return View(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarInstituicao")]
        public async Task<IActionResult> SalvarAlteracaoInstituicao(vmInstituicao vm, int id)
        {
            string redirecionaUrl = "Instituicoes".MontaUrl(base.HttpContext.Request);

            if (vm == null || string.IsNullOrEmpty(vm.Nome) || string.IsNullOrEmpty(vm.CNPJ) || string.IsNullOrEmpty(vm.Sigla) || 
                string.IsNullOrEmpty(vm.Telefone) || string.IsNullOrEmpty(vm.Contato))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios (*) ou não havia nada para ser salvo" });

            /* ATENÇÃO:
             * Como os navegadores atuais possuem segurança que impossibilitam pegar o path completo, o nome do path será sempre "fakepath",
             * ENTÃO, SOMENTE AQUI (GetImagem...) CAPTURAMOS O ARQUIVO E GUARDAMOS A PASTA COMPLETA E CORRETA DE ONDE FOI FEITO O UPLOAD, E TAMBÉM GUARDANOS OS bytes[] do arquivo.
             */
            GetImagemTimbre(vm);
            GetImagemLogomarca(vm);

            Instituicao? Instituicoes = await _db.Instituicao.Where(s => s.Id == id).SingleOrDefaultAsync();
            if (Instituicoes == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Não foi possível salvar o registro neste momento", action = "", sucesso = false });

            //Feito pelo Kiro em 20/04/2026
            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        //Colunas NÃO nulas:
                        Instituicoes.Nome = vm.Nome.ToUpper();
                        Instituicoes.Sigla = vm.Sigla.ToUpper();
                        Instituicoes.CNPJ = vm.CNPJ.CNPJSemFormatacao();
                        Instituicoes.Email = vm.Email;
                        Instituicoes.Telefone = vm.Telefone;
                        Instituicoes.Contato = vm.Contato;
                        Instituicoes.CarimboSN = vm.CarimboSN;
                        Instituicoes.TimbreSN = vm.TimbreSN;

                        //Colunas que aceitam nulo:
                        Instituicoes.Logradouro = vm.Logradouro.ToCapitalize();
                        Instituicoes.Endereco = vm.Endereco.ToCapitalize();
                        Instituicoes.Numero = vm.Numero;
                        Instituicoes.Complemento = vm.Complemento;
                        Instituicoes.Bairro = vm.Bairro.ToCapitalize();
                        Instituicoes.Cidade = vm.Cidade.ToCapitalize();
                        Instituicoes.UF = vm.vmGeral.TipoUF;
                        Instituicoes.CEP = vm.CEP;
                        Instituicoes.Celular = vm.Celular;
                        Instituicoes.Sequencial = vm.Sequencial;
                        Instituicoes.TituloTimbre = vm.TituloTimbre;
                        Instituicoes.SubTituloTimbre = vm.SubTituloTimbre;
                        Instituicoes.UsuarioCaminhoFTP = vm.UsuarioCaminhoFTP;
                        Instituicoes.UsuarioEmailFTP = vm.UsuarioEmailFTP;
                        Instituicoes.UsuarioPortaFTP = vm.UsuarioPortaFTP;
                        Instituicoes.UsuarioSenhaFTP = vm.UsuarioSenhaFTP;
                        Instituicoes.ValorExameCitologia = vm.ValorExameCitologia;
                        Instituicoes.Propaganda = vm.Propaganda;
                        Instituicoes.AvisoRodape1 = vm.AvisoRodape1;
                        Instituicoes.AvisoRodape2 = vm.AvisoRodape2;

                        /*
                         * Gravando as imagens em bytes[]
                         * Obs: o caminho de origem da imagem não é salvo, por questões de privacidade.
                         */
                        if (vm.Timbre != null)
                            Instituicoes.Timbre = vm.Timbre;

                        if (vm.NomeTimbre != null)
                            Instituicoes.NomeTimbre = vm.NomeTimbre;

                        if (vm.Logomarca != null)
                            Instituicoes.Logomarca = vm.Logomarca;

                        if (vm.NomeLogomarca != null)
                            Instituicoes.NomeLogomarca = vm.NomeLogomarca;

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Instituição foi atualizada", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Instituicoes] Não foi atualizada - Erro: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição NÃO foi atualizada", action = "", sucesso = false });
                    }
                }
            });
            //..Kiro
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirInstituicao")]
        public async Task<IActionResult> ExcluirInstituicao(int id)
        {
            //Feito pelo Kiro em 20/04/2026
            // Verifica se a instituição possui vínculos antes de excluir
            //Feito pelo Qoder em 21/04/2026 - inclui ExamesRealizadosAM (D5) e Postos vinculados
            bool possuiVinculos = await _db.Requisitar.AnyAsync(r => r.InstituicaoId == id)
                               || await _db.ExamesRealizados.AnyAsync(e => e.InstituicaoId == id)
                               || await _db.ExamesRealizadosAM.AnyAsync(e => e.InstituicaoId == id)
                               || await _db.ExamesPendentes.AnyAsync(e => e.InstituicaoId == id);

            if (possuiVinculos)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição possui exames, requisições ou fichas vinculadas e não pode ser excluída", action = "", sucesso = false });

            // Bloqueia exclusão se houver Postos vinculados (FK Restrict)
            bool possuiPostos = await _db.Postos.AnyAsync(p => p.InstituicaoId == id);
            if (possuiPostos)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição possui postos vinculados e não pode ser excluída", action = "", sucesso = false });
            //..Qoder
            //..Kiro

            // Excluindo um registro da tabela
            DeleteContext<Instituicao> context = new DeleteContext<Instituicao>(new DeleteStrategy<Instituicao>(_db));
            JsonResult result = await context.DeleteRecordAsync(id, "Instituicao");
            return result;
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarInstituicao")]
        public async Task<ActionResult> ConsultarInstituicao(vmInstituicao vm, int id)
        {
            Instituicao dados = await _db.Instituicao.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.Nome = dados.Nome;
                vm.Sigla = dados.Sigla;
                vm.CNPJ = dados.CNPJ.FormatarCNPJNotNull();
                vm.Email = dados.Email;
                vm.Logradouro = dados.Logradouro;
                vm.Endereco = dados.Endereco;
                vm.Numero = dados.Numero;
                vm.Complemento = dados.Complemento;
                vm.Bairro = dados.Bairro;
                vm.Cidade = dados.Cidade;
                vm.UF = dados.UF;
                vm.CEP = dados.CEP;
                vm.Telefone = dados.Telefone.FormataTelefoneNotNull();
                vm.Celular = dados.Celular;
                vm.Sequencial = dados.Sequencial;
                vm.TituloTimbre = dados.TituloTimbre;
                vm.SubTituloTimbre = dados.SubTituloTimbre;
                vm.CarimboSN = dados.CarimboSN;
                vm.TimbreSN = dados.TimbreSN;
                vm.Contato = dados.Contato;
                vm.UsuarioCaminhoFTP = dados.UsuarioCaminhoFTP;
                vm.UsuarioEmailFTP = dados.UsuarioEmailFTP;
                vm.UsuarioPortaFTP = dados.UsuarioPortaFTP;
                vm.UsuarioSenhaFTP = dados.UsuarioSenhaFTP;
                vm.ValorExameCitologia = dados.ValorExameCitologia;
                vm.Propaganda = dados.Propaganda;
                vm.AvisoRodape1 = dados.AvisoRodape1;
                vm.AvisoRodape2 = dados.AvisoRodape2;
                /*
                 * Imagens
                 */
                vm.Timbre = dados.Timbre;
                vm.Logomarca = dados.Logomarca;
                vm.NomeTimbre = dados.NomeTimbre;
                vm.NomeLogomarca = dados.NomeLogomarca;
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new()
                {
                    TipoUF = dados.UF
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis via ViewModel tipado
                 */
                vm.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares
            ViewBag.TextoMenu = new object[] { "Consulta de Instituição", false };
            //Finalização para a View
            _geralController.Validacao("ConsultarInstituicao,Instituicoes", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirImagemTimbre")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> ExcluirImagemTimbre(string sigla)
        {
            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        Instituicao registro = await _db.Instituicao.FirstAsync(s => s.Sigla == sigla);
                        if (registro != null && registro.Sigla == sigla)
                        {
                            registro.NomeTimbre = "";
                            registro.Timbre = null;

                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Imagem foi excluída da instituição", action = "", sucesso = true });
                        }

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Imagem não foi excluída", action = "", sucesso = false });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Instituicoes] ExcluirImagemTimbre - Erro: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Imagem não foi excluída", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirImagemLogomarca")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> ExcluirImagemLogomarca(string sigla)
        {
            //Na verdade, não é uma exclusão de registro e sim LIMPEZA do campo!
            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        Instituicao registro = await _db.Instituicao.FirstAsync(s => s.Sigla == sigla);
                        if (registro != null && registro.Sigla == sigla)
                        {
                            registro.NomeLogomarca = "";
                            registro.Logomarca = null;

                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Imagem foi excluída da instituição", action = "", sucesso = true });
                        }

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Imagem não foi excluída", action = "", sucesso = false });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Instituicoes] ExcluirImagemLogomarca - Erro: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Imagem não foi excluída", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConverterPdf")]
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
                _eventLogHelper.LogEventViewer("[Instituicoes] Falha ao converter PDF: " + ex.Message, "wError");
                return Json(new { success = false, responseText = string.Format("{0} {1}", "Falha:", ex.Message) });
            }
        }

        /*
         * Salva uma imagem em bytes[] já pronta para ser exibida em outro momento
         */

        private void GetImagemTimbre(vmInstituicao vm)
        {
            if (NaoExistePath(vm.CaminhoImagemTimbre))
            {
                /* Na inclusão nunca teremos o path correto via JQuery, por isso manobramos aqui para pegar C:\Images\ no computador Local */
                vm.CaminhoImagemTimbre = Utils.Utils.GetLocalPathImagens();
            }
            vm.CaminhoImagemTimbre = _pathHelper.GetPathTrue(vm.CaminhoImagemTimbre, vm.NomeTimbre);

            if (!string.IsNullOrEmpty(vm.CaminhoImagemTimbre) && !string.IsNullOrEmpty(vm.NomeTimbre))
            {
                string path = Path.Combine(vm.CaminhoImagemTimbre, vm.NomeTimbre);
                if (path != null)
                {
                    if (System.IO.File.Exists(path))
                    {
                        FileStream oFileStream = new(path, FileMode.Open, FileAccess.Read);
                        // Create a byte array of file size.
                        byte[] FileByteArrayData = new byte[oFileStream.Length];
                        //Read file in bytes from stream into the byte array
                        oFileStream.Read(FileByteArrayData, 0, System.Convert.ToInt32(oFileStream.Length));
                        //Close the File Stream
                        oFileStream.Close();

                        vm.Timbre = FileByteArrayData; //return the byte data
                    }
                }
            }
        }

        /*
         * Salva uma imagem em bytes[] já pronta para ser exibida em outro momento
         */

        private void GetImagemLogomarca(vmInstituicao vm)
        {
            if (NaoExistePath(vm.CaminhoImagemLogomarca))
            {
                /* Na inclusão nunca teremos o path correto via JQuery, por isso manobramos aqui para pegar C:\Images\ no computador Local */
                vm.CaminhoImagemLogomarca = Utils.Utils.GetLocalPathImagens();
            }
            vm.CaminhoImagemLogomarca = _pathHelper.GetPathTrue(vm.CaminhoImagemLogomarca, vm.NomeLogomarca);

            if (!string.IsNullOrEmpty(vm.CaminhoImagemLogomarca) && !string.IsNullOrEmpty(vm.NomeLogomarca))
            {
                string path = Path.Combine(vm.CaminhoImagemLogomarca, vm.NomeLogomarca);
                if (path != null)
                {
                    if (System.IO.File.Exists(path))
                    {
                        FileStream oFileStream = new(path, FileMode.Open, FileAccess.Read);
                        // Create a byte array of file size.
                        byte[] FileByteArrayData = new byte[oFileStream.Length];
                        //Read file in bytes from stream into the byte array
                        oFileStream.Read(FileByteArrayData, 0, System.Convert.ToInt32(oFileStream.Length));
                        //Close the File Stream
                        oFileStream.Close();

                        vm.Logomarca = FileByteArrayData; //return the byte data
                    }
                }
            }
        }
    }

    //internal class CustomErrorModel
    //{
    //    private string v;

    //    public CustomErrorModel(string v)
    //    {
    //        this.v = v;
    //    }
    //}
}