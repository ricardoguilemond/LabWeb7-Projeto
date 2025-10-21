using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
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
using static BLL.UtilBLL;
using static LabWebMvc.MVC.UtilHelper.TrataExcecoes;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class PacientesController : BaseController
    {
        public PacientesController(IDbFactory dbFactory, 
                                   IValidadorDeSessao validador, 
                                   GeralController geralController, 
                                   IEventLogHelper eventLogHelper, 
                                   Imagem imagem)
               : base(dbFactory, validador, geralController, eventLogHelper, imagem)
        { }

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
        public async Task<IActionResult> Index(string? Conteudo, int registros = 50)
        {
            // ViewBag.TextoMenu = new object[] { "Cadastro de Pacientes", false };

            MontaControllers("IncluirPaciente", "Pacientes");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            ICollection<dynamic> listaGrid = [];
            List<Pacientes> dados = [];

            int totalTabela = 0;
            int totalRegistros = 0;
            if (string.IsNullOrEmpty(Conteudo)) registros = 100; //quando não tem dados para filtrar

            totalTabela = _db.Pacientes.AsNoTracking().AsEnumerable().Count();

            if (!string.IsNullOrEmpty(Conteudo))
            {
                dados = await _db.Pacientes.AsNoTracking()
                          .FiltrarPorConteudo(Conteudo, x => x.CPF, x => x.NomePaciente, x => x.NomeSocial, x => x.Endereco, x => x.Bairro, x => x.Cidade, x => x.Id.ToString())
                          .OrderByDescending(x => x.Id)
                          .ToListAsync();

                if (Conteudo.Split('/').Count() == 3 || Conteudo.Split('-').Count() == 3) //está buscando alguma data
                {
                    DateTime dataBusca = Conteudo.Trim().FormataData("dd/MM/yyyy", true);
                    ICollection<Pacientes> dadosQuery = await _db.Pacientes.AsNoTracking()
                                                       .Where(l => l.Nascimento.Day > 0 &&
                                                                    l.Nascimento.Year == dataBusca.Year &&
                                                                    l.Nascimento.Month == dataBusca.Month &&
                                                                    l.Nascimento.Day == dataBusca.Day
                                                                   )
                                                       .OrderByDescending(o => o.Id)
                                                       .ToListAsync();
                    if (dadosQuery.Count > 0)
                        dados.AddRange(dadosQuery);
                }
            }
            else
                dados = await _db.Pacientes.AsNoTracking().OrderByDescending(o => o.Id).Take(registros).ToListAsync();

            foreach (Pacientes item in dados)
            {
                totalRegistros++;
                vmPacientes resultado = new vmPacientes()
                {
                    Id = item.Id,
                    IdPacienteExterno = item.IdPacienteExterno,
                    NomePaciente = item.NomePaciente,
                    Nascimento = item.Nascimento,
                    Sexo = item.Sexo,
                    CPF = item.CPF.FormatarCPF(),
                    TipoDocumento = item.TipoDocumento,
                    Identidade = item.Identidade,
                    Telefone = item.Telefone.FormataTelefone(),
                    Emissor = item.Emissor
                };
                listaGrid.Add(resultado);
            }

            //ViewBag.TotalRegistros = totalRegistros.ToString();
            //ViewBag.TotalTabela = totalTabela.ToString();
            //ViewBag.ListaDados = listaGrid;

            //Finalização da View
            var vmResposta = new vmListaValidacao<dynamic>
            {
                RetornoDeRota = "Index",
                Titulo = "Cadastro de Pacientes",
                TotalRegistros = totalRegistros,
                TotalTabela = totalTabela,
                ListaDados = listaGrid.Cast<dynamic>().ToList()
            };
            return _geralController.ValidacaoGenerica(vmResposta);
            //return _geralController.Validacao("Index", "Cadastro de Pacientes", totalRegistros, totalTabela, listaGrid);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirPaciente")]
        public IActionResult IncluirPaciente()
        {
            //Finalização da View
            return _geralController.Validacao("IncluirPaciente", "Cadastro de Pacientes");
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
                        paciente.Emissor = obj.vmGeral.TipoOrgaoEmissor > -1 ? obj.vmGeral.TipoOrgaoEmissor : 0;

                        //Colunas NÃO nulas:
                        paciente.NomePaciente = obj.NomePaciente.ToUpper();
                        paciente.Nascimento = obj.Nascimento;
                        paciente.EstadoCivil = obj.EstadoCivil; // obj.vmGeral.TipoEstadoCivil;
                        paciente.TempoGestacao = obj.vmGeral.TipoTempoGestacao;
                        paciente.DataEntrada = _geralController.ObterDataHoraServidor().ToFormataData();   //DateTime.Now;
                        paciente.DataRegistro = _geralController.ObterDataHoraServidor().ToFormataData();   //DateTime.Now;
                        paciente.StatusBaixa = 0;
                        paciente.IdPacienteExterno = obj.IdPacienteExterno;

                        //Endereçamento e outros dados que aceitam nulos:
                        paciente.CarteiraSUS = obj.CarteiraSUS;
                        paciente.Complemento = obj.Complemento;
                        paciente.DUM = obj.DUM;
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

                        _eventLogHelper.LogEventViewer("ERRO: Paciente não foi salvo CNPJ: " + obj.CPF, "wError");

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
                 * variáveis para uso em comparações que facilitam ir por ViewBag!
                 */
                ViewBag.Sexo = dados.Sexo;
                ViewBag.EstadoCivil = dados.EstadoCivil;
                ViewBag.SessionUF = dados.UF;
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
                        paciente.Emissor = vm.vmGeral.TipoOrgaoEmissor > -1 ? vm.vmGeral.TipoOrgaoEmissor : 0;

                        //Colunas NÃO nulas:
                        paciente.NomePaciente = vm.NomePaciente.ToUpper();
                        paciente.Nascimento = vm.Nascimento;
                        paciente.EstadoCivil = vm.EstadoCivil;
                        paciente.TempoGestacao = vm.vmGeral.TipoTempoGestacao;

                        //Colunas que aceitam nulas:
                        paciente.Bairro = vm.Bairro.ToCapitalize();
                        paciente.CarteiraSUS = vm.CarteiraSUS;
                        paciente.CEP = vm.CEP;
                        paciente.Cidade = vm.Cidade.ToCapitalize();
                        paciente.Complemento = vm.Complemento;
                        paciente.DUM = vm.DUM;
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
                 * variáveis para uso em comparações que facilitam ir por ViewBag!
                 */
                ViewBag.TipoDocumento = dados.TipoDocumento;
                ViewBag.Sexo = dados.Sexo;
                ViewBag.EstadoCivil = dados.EstadoCivil;
                ViewBag.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Consulta de Paciente", false };
            //Finalização para a View
            _geralController.Validacao("ConsultarPaciente,Pacientes", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }

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