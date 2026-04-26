using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ExpressionCombiner;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class PostosController : BaseController
    {
        public PostosController(
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
        [Route("Postos")]
        public async Task<IActionResult> Index(string? Conteudo, int registros = 50)
        {
            //monta o controller para chamada dos itens que estão em _PartialMenuPostos.cshtml
            MontaControllers("IncluirPostos", "Postos");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            ICollection<dynamic> listaGrid = [];
            List<Postos> dados = [];

            int totalTabela = 0;
            int totalRegistros = 0;
            if (string.IsNullOrEmpty(Conteudo)) registros = 100; //quando não tem dados para filtrar

            totalTabela = _db.Postos.AsNoTracking().AsEnumerable().Count();

            if (!string.IsNullOrEmpty(Conteudo))
            {
                dados = await _db.Postos.AsNoTracking()
                          .FiltrarPorConteudo(Conteudo, x => x.NomePosto, x => x.Endereco, x => x.Bairro, x => x.Cidade, x => x.Id.ToString())
                          .OrderByDescending(x => x.Id)
                          .ToListAsync();
            }
            else
                dados = await _db.Postos.AsNoTracking().OrderByDescending(o => o.Id).Take(registros).ToListAsync();

            foreach (Postos item in dados)
            {
                totalRegistros++;
                vmPostos resultado = new()
                {
                    Id = item.Id,
                    NomePosto = item.NomePosto,
                    Logradouro = item.Logradouro,
                    Endereco = item.Endereco,
                    Numero = item.Numero,
                    Complemento = item.Complemento,
                    Bairro = item.Bairro,
                    Cidade = item.Cidade,
                    UF = item.UF,
                    CEP = item.CEP,
                    Responsavel = item.Responsavel.ToCapitalizeNotNull(),
                    Telefone = item.Telefone?.FormataTelefoneNotNull()
                };
                listaGrid.Add(resultado);
            }

            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = listaGrid;

            //Finalização da View
            return _geralController.Validacao("Index", "Cadastro de Postos de Coletas e Anexos", totalRegistros, totalTabela, listaGrid);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirPostos")]
        public IActionResult IncluirPostos()
        {
            //Finalização da View
            return _geralController.Validacao("IncluirPostos", "Cadastro de Postos de Coletas e Anexos");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirPostos")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> SalvarPostos(vmPostos vm)
        {
            string redirecionaUrl = "Postos".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.NomePosto))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios ou não havia nada para ser salvo" });

            Postos? Postos = await _db.Postos.Where(s => s.NomePosto == vm.NomePosto).SingleOrDefaultAsync();
            if (Postos != null)
            {
                if (Postos.NomePosto == vm.NomePosto.ToUpper())
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Posto/anexo já cadastrada com este nome", action = "", sucesso = false });
                else
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Posto/anexo já cadastrada", action = "", sucesso = false });
            }

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        await _db.Postos.AddAsync(new Postos()
                        {
                            //Colunas NÃO nulas:
                            NomePosto = vm.NomePosto.ToUpper(),
                            Responsavel = vm.Responsavel,

                            //Colunas que aceitam nulas:
                            Telefone = vm.Telefone,
                            Endereco = vm.Endereco.ToCapitalize(),
                            Logradouro = vm.Logradouro.ToCapitalize(),
                            Numero = vm.Numero,
                            Bairro = vm.Bairro.ToCapitalize(),
                            Complemento = vm.Complemento,
                            Cidade = vm.Cidade.ToCapitalize(),
                            UF = vm.vmGeral.TipoUF,
                            CEP = vm.CEP
                        });

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Posto foi salvo", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Postos] Erro ao salvar Posto/anexo: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Posto NÃO foi salvo", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarPostos")]
        public async Task<IActionResult> AlterarPostos(vmPostos vm, int id)
        {
            Postos dados = await _db.Postos.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.NomePosto = dados.NomePosto.ToUpper();
                vm.Logradouro = dados.Logradouro.ToCapitalize();
                vm.Endereco = dados.Endereco.ToCapitalize();
                vm.Numero = dados.Numero;
                vm.Bairro = dados.Bairro.ToCapitalize();
                vm.Complemento = dados.Complemento;
                vm.Cidade = dados.Cidade.ToCapitalize();
                vm.UF = dados.UF;
                vm.CEP = dados.CEP;
                vm.Telefone = dados.Telefone;
                vm.Responsavel = dados.Responsavel;
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new()
                {
                    TipoUF = dados.UF
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis para uso em comparações que facilitam ir por ViewBag!
                 */
                ViewBag.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Alterar Cadastro de Postos/anexos", false };
            //Finalização da View
            _geralController.Validacao("AlterarPostos,Postos", ViewBag.TextoMenu[0]);
            return View(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarPostos")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> SalvarAlteracaoPostos(vmPostos vm, int id)
        {
            string redirecionaUrl = "Postos".MontaUrl(base.HttpContext.Request);

            if (vm == null || string.IsNullOrEmpty(vm.NomePosto))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios ou não havia nada para ser salvo" });

            Postos? Postos = await _db.Postos.Where(s => s.Id == id).SingleOrDefaultAsync();
            if (Postos == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Não foi possível salvar o registro neste momento", action = "", sucesso = false });

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        //Colunas NÃO nulas:
                        Postos.NomePosto = vm.NomePosto.ToUpper();
                        Postos.Responsavel = vm.Responsavel.ToCapitalizeNotNull();

                        //Colunas que aceitam nulo:
                        Postos.Telefone = vm.Telefone;
                        Postos.Logradouro = vm.Logradouro.ToCapitalize();
                        Postos.Endereco = vm.Endereco.ToCapitalize();
                        Postos.Numero = vm.Numero;
                        Postos.Complemento = vm.Complemento;
                        Postos.Bairro = vm.Bairro.ToCapitalize();
                        Postos.Cidade = vm.Cidade.ToCapitalize();
                        Postos.UF = vm.vmGeral.TipoUF;
                        Postos.CEP = vm.CEP;

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Posto/anexo foi atualizado", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Postos] Erro ao atualizar/alterar: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Posto/anexo NÃO foi atualizado", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirPostos")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> ExcluirPostos(int id)
        {
            //Feito pelo Kiro em 20/04/2026
            // Verifica se o posto possui vínculos antes de excluir
            bool possuiVinculos = await _db.Requisitar.AnyAsync(r => r.PostoId == id)
                               || await _db.ExamesRealizados.AnyAsync(e => e.PostoId == id);

            if (possuiVinculos)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Posto possui requisições ou exames vinculados e não pode ser excluído", action = "", sucesso = false });
            //..Kiro

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        Postos registro = await _db.Postos.FirstAsync(s => s.Id == id);
                        if (registro != null && registro.Id == id)
                        {
                            _db.Remove(registro);
                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Registro foi excluído", action = "", sucesso = true });
                        }

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi encontrado", action = "", sucesso = false });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Postos] Excluir - Erro: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi excluído", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarPostos")]
        public async Task<ActionResult> ConsultarPostos(vmPostos vm, int id)
        {
            Postos dados = await _db.Postos.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.NomePosto = dados.NomePosto;
                vm.Logradouro = dados.Logradouro;
                vm.Endereco = dados.Endereco;
                vm.Numero = dados.Numero;
                vm.Complemento = dados.Complemento;
                vm.Bairro = dados.Bairro;
                vm.Cidade = dados.Cidade;
                vm.UF = dados.UF;
                vm.CEP = dados.CEP;
                vm.Telefone = dados.Telefone.FormataTelefone();
                vm.Responsavel = dados.Responsavel.ToCapitalizeNotNull();
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new()
                {
                    TipoUF = dados.UF
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis para uso em comparações que facilitam ir por ViewBag!
                 */
                ViewBag.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Consulta de Postos/anexos", false };
            //Finalização para a View
            _geralController.Validacao("ConsultarPostos,Postos", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }
    }
}