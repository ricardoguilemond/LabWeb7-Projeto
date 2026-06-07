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
                                   Imagem imagem,
                                   ExclusaoService exclusaoService,
                                   IConnectionService connectionService)
               : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
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
        public async Task<IActionResult> Index(string? Conteudo, int registros = 50,
            string? dataInicial = null, string? dataFinal = null,
            string? nomePaciente = null, int? folhaId = null)
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
                    // Converte data local para range UTC — necessário para comparar com colunas timestamptz no Npgsql 8.x
                    // (Usar .Day/.Month/.Year em timestamptz traduz para EXTRACT() que opera em UTC, causando resultados incorretos)
                    var (inicioUtc, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataBusca);
                    ICollection<Pacientes> dadosQuery = await _db.Pacientes.AsNoTracking()
                                                       .Where(l => l.Nascimento >= inicioUtc &&
                                                                    l.Nascimento <= fimUtc
                                                                   )
                                                       .OrderByDescending(o => o.Id)
                                                       .ToListAsync();
                    if (dadosQuery.Count > 0)
                        dados.AddRange(dadosQuery);
                }
            }
            else
            {
                //Feito pelo Kiro em 17/05/2026
                // Filtros backend de exames — só se aplicam quando Conteudo está vazio
                bool temFiltroExames = !string.IsNullOrEmpty(dataInicial)
                                    || !string.IsNullOrEmpty(dataFinal)
                                    || !string.IsNullOrEmpty(nomePaciente)
                                    || folhaId.HasValue;

                if (temFiltroExames)
                {
                    var query = _db.Pacientes.AsNoTracking().AsQueryable();

                    // Filtro por período (DataIni dos ExamesRealizados dentro do range UTC)
                    if (!string.IsNullOrEmpty(dataInicial))
                    {
                        DateTime dataIniParsed = dataInicial.Trim().FormataData("dd/MM/yyyy", true);
                        var (inicioUtc, _) = _geralController.ConverterDataLocalParaRangeUtc(dataIniParsed);
                        query = query.Where(p => p.ExamesRealizados.Any(e => e.DataIni >= inicioUtc));
                    }

                    if (!string.IsNullOrEmpty(dataFinal))
                    {
                        DateTime dataFimParsed = dataFinal.Trim().FormataData("dd/MM/yyyy", true);
                        var (_, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataFimParsed);
                        query = query.Where(p => p.ExamesRealizados.Any(e => e.DataIni <= fimUtc));
                    }

                    // Filtro por nome do paciente (case-insensitive)
                    if (!string.IsNullOrEmpty(nomePaciente))
                    {
                        query = query.Where(p => p.NomePaciente.ToLower().Contains(nomePaciente.Trim().ToLower()));
                    }

                    // Filtro por folha de exame (via ItensExamesRealizados.ClasseExamesId)
                    if (folhaId.HasValue)
                    {
                        query = query.Where(p => p.ExamesRealizados.Any(e => e.ItensExamesRealizados.Any(i => i.ClasseExamesId == folhaId.Value)));
                    }

                    dados = await query.OrderByDescending(o => o.Id).ToListAsync();
                }
                else
                {
                    dados = await _db.Pacientes.AsNoTracking().OrderByDescending(o => o.Id).Take(registros).ToListAsync();
                }
                //..Kiro
            }

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

            //Finalização da View
            ViewBag.TextoMenu = new object[] { "Cadastro de Pacientes", false };
            var vmIndex = new vmPacientes { ListaDados = listaGrid };
            return View(vmIndex);
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
                        paciente.Emissor = obj.vmGeral.TipoOrgaoEmissor > -1 ? obj.vmGeral.TipoOrgaoEmissor : 0;

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
                        paciente.Emissor = vm.vmGeral.TipoOrgaoEmissor > -1 ? vm.vmGeral.TipoOrgaoEmissor : 0;

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
            bool possuiVinculos = await _db.Requisitar.AnyAsync(r => r.PacienteId == id)
                               || await _db.ExamesRealizados.AnyAsync(e => e.PacienteId == id)
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
        [Route("Pacientes/ObterFolhasExame")]
        public IActionResult ObterFolhasExame()
        {
            try
            {
                var folhas = _db.ClasseExames
                    .AsNoTracking()
                    .OrderBy(c => c.RefExame)
                    .Select(c => new { c.Id, c.RefExame })
                    .ToList();

                return Json(new { sucesso = true, folhas });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Pacientes] ObterFolhasExame - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao obter folhas de exame" });
            }
        }
        //..Kiro

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Pacientes/ObterExamesPaciente")]
        public async Task<IActionResult> ObterExamesPaciente(int pacienteId, bool expandir = false)
        {
            try
            {
                if (expandir)
                {
                    // Modo expandido: exames dos últimos 12 meses (sem limite de quantidade)
                    DateTime dataLimite12Meses = DateTime.UtcNow.AddMonths(-12);

                    var dadosExpandidos = await _db.ExamesRealizados
                        .AsNoTracking()
                        .Where(e => e.PacienteId == pacienteId && e.DataIni >= dataLimite12Meses)
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
                    .Where(e => e.PacienteId == pacienteId)
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
                    .Where(e => e.PacienteId == pacienteId)
                    .CountAsync();

                int examesOcultos = totalExames - examesExibidos.Count;

                // 4. Projetar resultado
                var exames = examesExibidos.Select(e => new
                {
                    e.Id,
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

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Pacientes/ObterItensExame")]
        public async Task<IActionResult> ObterItensExame(int exameRealizadoId)
        {
            try
            {
                var dados = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => i.ExameRealizadoId == exameRealizadoId)
                    .OrderBy(i => i.OrdemItem)
                    .ToListAsync();

                var itens = dados.Select(i => new
                {
                    i.RefExame,
                    i.RefItem,
                    ContaExame = i.ContaExame.FormatarContaExameSem11(),
                    Descricao = i.Descricao ?? ""
                }).ToList();

                return Json(new { sucesso = true, itens });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Pacientes] ObterItensExame - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao obter itens do exame" });
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