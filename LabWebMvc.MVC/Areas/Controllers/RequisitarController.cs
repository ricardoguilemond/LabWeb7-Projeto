using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using Google.Api;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Impressoras;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Interfaces.Collections;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class RequisitarController : BaseController
    {
        private readonly IServiceProvider _serviceProvider;
        public record ApiResult(bool sucesso, string mensagem, string? action, object? dados);

        public RequisitarController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IServiceProvider serviceProvider)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _serviceProvider = serviceProvider;
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
        [Route("Requisitar")]
        public async Task<IActionResult> Index(string? Conteudo, int registros = 100)
        {
            MontaControllers("IncluirRequisicao", "Requisição de Exames");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            List<Requisitar> dados = [];

            int totalTabela = 0;

            if (string.IsNullOrEmpty(Conteudo)) registros = 100; //quando não tem dados para filtrar

            totalTabela = _db.Requisitar.AsNoTracking().AsEnumerable().Count();

            dados = await _db.Requisitar.AsNoTracking().OrderByDescending(o => o.Id).Take(registros).ToListAsync();

            int totalRegistros = dados.Count();

            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = dados;

            //Finalização da View
            var vmResposta = new vmListaValidacao<dynamic>
            {
                RetornoDeRota = "Index",
                Titulo = "Requisição de Exames",
                TotalRegistros = totalRegistros,
                TotalTabela = totalTabela,
                ListaDados = dados.Cast<dynamic>().ToList()
            };
            return _geralController.ValidacaoGenerica(vmResposta);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirRequisicao")]
        public IActionResult IncluirRequisicao()
        {
            //Finalização da View
            return _geralController.Validacao("IncluirRequisicao", "Lançar Requisição");
        }

        // ValidarDados NÃO retorna IActionResult.
        // Ela retorna um objeto de domínio:
        private ApiResult? ValidarDadosDominio(vmRequisitar vm)
        {
            if (vm.VmPacientes.Id <= 0)
            {
                if (string.IsNullOrEmpty(vm.VmPacientes.NomePaciente))
                    return new ApiResult(false, "O nome do paciente é obrigatório.", null, null);

                if (vm.VmPacientes.Nascimento == DateTime.MinValue)
                    return new ApiResult(false, "A data de nascimento do paciente é obrigatório.", null, null);

                if (string.IsNullOrEmpty(vm.VmPacientes.CPF) && string.IsNullOrEmpty(vm.VmPacientes.CarteiraSUS) && string.IsNullOrEmpty(vm.VmPacientes.Identidade))
                    return new ApiResult(false, "Um documento de registro nacional do Paciente é obrigatório.", null, null);
            }

            if (string.IsNullOrEmpty(vm.VmMedicos.CRM) && string.IsNullOrEmpty(vm.VmMedicos.NomeMedico))
                return new ApiResult(false, "O nome e o CRM do Médico são obrigatórios. Ou coloque CRM=0 'Sem Médico.", null, null);

            if (string.IsNullOrEmpty(vm.VmInstituicao.Sigla) || string.IsNullOrEmpty(vm.VmInstituicao.Nome))
                return new ApiResult(false, "A sigla e o nome da Instituição são obrigatórios.", null, null);

            if (string.IsNullOrEmpty(vm.VmTabelaExames.SiglaTabela) || string.IsNullOrEmpty(vm.VmTabelaExames.NomeTabela))
                return new ApiResult(false, "A sigla e o nome da Tabela de Exames são obrigatórios.", null, null);

            if (vm.ListaCupom == null || !vm.ListaCupom.Any())
                return new ApiResult(false, "Nenhum exame foi adicionado ao Cupom.", null, null);

            return null;
        }

        private IActionResult? ValidarDados(vmRequisitar vm, string redirecionaUrl)
        {
            if (vm.VmPacientes.Id <= 0)
            {
                if (string.IsNullOrEmpty(vm.VmPacientes.NomePaciente))
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "O nome do Paciente é obrigatório.", action = redirecionaUrl, sucesso = false });

                if (vm.VmPacientes.Nascimento == DateTime.MinValue)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "A data de nascimento do Paciente é obrigatória.", action = redirecionaUrl, sucesso = false });

                if (string.IsNullOrEmpty(vm.VmPacientes.CPF) && string.IsNullOrEmpty(vm.VmPacientes.CarteiraSUS) && string.IsNullOrEmpty(vm.VmPacientes.Identidade))
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Um documento de registro nacional do Paciente é obrigatório.", action = redirecionaUrl, sucesso = false });
            }

            if (string.IsNullOrEmpty(vm.VmMedicos.CRM) && string.IsNullOrEmpty(vm.VmMedicos.NomeMedico))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "O nome e o CRM do Médico são obrigatórios. Ou coloque CRM=0 'Sem Médico'", action = redirecionaUrl, sucesso = false });

            if (string.IsNullOrEmpty(vm.VmInstituicao.Sigla) || string.IsNullOrEmpty(vm.VmInstituicao.Nome))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "A sigla e o nome da Instituição são obrigatórios.", action = redirecionaUrl, sucesso = false });

            if (string.IsNullOrEmpty(vm.VmTabelaExames.SiglaTabela) || string.IsNullOrEmpty(vm.VmTabelaExames.NomeTabela))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "A sigla e o nome da Tabela de Exames são obrigatórios.", action = redirecionaUrl, sucesso = false });

            if (string.IsNullOrEmpty(vm.ContaExame) || string.IsNullOrEmpty(vm.Descricao) || vm.ValorItem <= 0)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Alguma divergência em um dos dados de exames estão invalidando o lançamento. Verifique o valor.", action = redirecionaUrl, sucesso = false });

            if (vm.ListaCupom == null || !vm.ListaCupom.Any())
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Nenhum exame foi adicionado ao Cupom.", action = redirecionaUrl, sucesso = false });

            return null;
        }

        private async Task<Pacientes> CriarOuAtualizarPacienteAsync(vmRequisitar vm)
        {
            Pacientes paciente;

            if (vm.VmPacientes.Id > 0)
            {
                // Busca o paciente existente no banco
                paciente = await _db.Pacientes.FindAsync(vm.VmPacientes.Id) ?? new Pacientes();

                if (paciente == null)
                {
                    // Se não encontrar, cria novo
                    paciente = new Pacientes();
                    _db.Pacientes.Add(paciente);
                    paciente.DataEntrada = _geralController.ObterDataHoraUtc();
                    paciente.DataRegistro = _geralController.ObterDataHoraUtc();
                    paciente.StatusBaixa = 0;
                }
                else
                {
                    // Marca como modificado para atualizar
                    _db.Entry(paciente).State = EntityState.Modified;
                }
            }
            else
            {
                // Cria novo paciente
                paciente = new Pacientes();
                _db.Pacientes.Add(paciente);
                paciente.DataEntrada = _geralController.ObterDataHoraUtc();
                paciente.DataRegistro = _geralController.ObterDataHoraUtc();
                paciente.StatusBaixa = 0;
            }

            // Atualiza os dados (comum para novo ou existente)
            paciente.IdPacienteExterno = vm.VmPacientes.IdPacienteExterno.Safe();
            paciente.NomePaciente = vm.VmPacientes.NomePaciente.ToUpper();
            // Nascimento, DUM e DataEntradaBrasil são colunas timestamptz — o model binder gera Kind=Unspecified,
            // que o Npgsql 8.x rejeita. Converte para UTC antes de gravar.
            paciente.Nascimento = _geralController.ConverterLocalParaUtc(vm.VmPacientes.Nascimento);
            paciente.NomeSocial = vm.VmPacientes.NomeSocial.SafeUpper();
            paciente.NomeMae = vm.VmPacientes.NomeMae.SafeUpper();
            paciente.NomePai = vm.VmPacientes.NomePai.SafeUpper();
            paciente.TipoDocumento = vm.VmPacientes.TipoDocumento;
            paciente.CPF = vm.VmPacientes.CPF.ApenasNumeros();
            paciente.Identidade = vm.VmPacientes.Identidade.ApenasNumeros();
            paciente.Emissor = vm.VmPacientes.Emissor;
            paciente.CarteiraSUS = vm.VmPacientes.CarteiraSUS.Safe();
            paciente.EstadoCivil = vm.VmPacientes.EstadoCivil;
            paciente.Sexo = vm.VmPacientes.Sexo.Safe();
            paciente.Cor = vm.VmPacientes.Cor.Safe();
            paciente.EtniaIndigena = vm.VmPacientes.EtniaIndigena.SafeUpper();
            paciente.TipoSanguineo = vm.VmPacientes.TipoSanguineo.Safe();
            paciente.DUM = vm.VmPacientes.DUM.HasValue
                ? _geralController.ConverterLocalParaUtc(vm.VmPacientes.DUM.Value)
                : null;
            paciente.TempoGestacao = vm.VmPacientes.TempoGestacao;
            paciente.Profissao = vm.VmPacientes.Profissao.SafeUpper();
            paciente.Naturalidade = vm.VmPacientes.Naturalidade.SafeUpper();
            paciente.Nacionalidade = vm.VmPacientes.Nacionalidade.SafeUpper();
            paciente.DataEntradaBrasil = vm.VmPacientes.DataEntradaBrasil.HasValue
                ? _geralController.ConverterLocalParaUtc(vm.VmPacientes.DataEntradaBrasil.Value)
                : null;
            paciente.Logradouro = vm.VmPacientes.Logradouro.SafeUpper();
            paciente.Endereco = vm.VmPacientes.Endereco.SafeUpper();
            paciente.Numero = vm.VmPacientes.Numero.Safe();
            paciente.Complemento = vm.VmPacientes.Complemento.Safe();
            paciente.Bairro = vm.VmPacientes.Bairro.SafeUpper();
            paciente.Cidade = vm.VmPacientes.Cidade.SafeUpper();
            paciente.UF = vm.VmPacientes.UF.Safe();
            paciente.CEP = vm.VmPacientes.CEP.ApenasNumeros();
            paciente.Email = vm.VmPacientes.Email.SafeLower();
            paciente.Telefone = vm.VmPacientes.Telefone.ApenasNumeros();
            paciente.Observacao = vm.VmPacientes.Observacao.Safe();

            return paciente;
        }

        private Pacientes CriarPaciente(vmRequisitar vm)
        {
            if (vm.VmPacientes.Id > 0) //paciente já existe
            {
                // Retorna apenas o Id para vinculação no EF
                return new Pacientes { Id = vm.VmPacientes.Id };
            }
            else
            {
                // Cria um novo paciente com todos os dados
                return new Pacientes
                {
                    IdPacienteExterno = vm.VmPacientes.IdPacienteExterno.Safe(),
                    NomePaciente = vm.VmPacientes.NomePaciente.ToUpper(),
                    Nascimento = _geralController.ConverterLocalParaUtc(vm.VmPacientes.Nascimento),
                    NomeSocial = vm.VmPacientes.NomeSocial.SafeUpper(),
                    NomeMae = vm.VmPacientes.NomeMae.SafeUpper(),
                    NomePai = vm.VmPacientes.NomePai.SafeUpper(),
                    TipoDocumento = vm.VmPacientes.TipoDocumento,
                    CPF = vm.VmPacientes.CPF.ApenasNumeros(),
                    Identidade = vm.VmPacientes.Identidade.ApenasNumeros(),
                    Emissor = vm.VmPacientes.Emissor,
                    CarteiraSUS = vm.VmPacientes.CarteiraSUS.Safe(),
                    EstadoCivil = vm.VmPacientes.EstadoCivil,
                    Sexo = vm.VmPacientes.Sexo.Safe(),
                    Cor = vm.VmPacientes.Cor.Safe(),
                    EtniaIndigena = vm.VmPacientes.EtniaIndigena.SafeUpper(),
                    TipoSanguineo = vm.VmPacientes.TipoSanguineo.Safe(),
                    DUM = vm.VmPacientes.DUM.HasValue ? _geralController.ConverterLocalParaUtc(vm.VmPacientes.DUM.Value) : null,
                    TempoGestacao = vm.VmPacientes.TempoGestacao,
                    Profissao = vm.VmPacientes.Profissao.SafeUpper(),
                    Naturalidade = vm.VmPacientes.Naturalidade.SafeUpper(),
                    Nacionalidade = vm.VmPacientes.Nacionalidade.SafeUpper(),
                    DataEntradaBrasil = vm.VmPacientes.DataEntradaBrasil.HasValue ? _geralController.ConverterLocalParaUtc(vm.VmPacientes.DataEntradaBrasil.Value) : null,
                    Logradouro = vm.VmPacientes.Logradouro.SafeUpper(),
                    Endereco = vm.VmPacientes.Endereco.SafeUpper(),
                    Numero = vm.VmPacientes.Numero.Safe(),
                    Complemento = vm.VmPacientes.Complemento.Safe(),
                    Bairro = vm.VmPacientes.Bairro.SafeUpper(),
                    Cidade = vm.VmPacientes.Cidade.SafeUpper(),
                    UF = vm.VmPacientes.UF.Safe(),
                    CEP = vm.VmPacientes.CEP.ApenasNumeros(),
                    Email = vm.VmPacientes.Email.SafeLower(),
                    Telefone = vm.VmPacientes.Telefone.ApenasNumeros(),
                    Observacao = vm.VmPacientes.Observacao.Safe(),
                    DataEntrada = _geralController.ObterDataHoraUtc(),
                    DataRegistro = _geralController.ObterDataHoraUtc(),
                    StatusBaixa = 0  //ativo
                };
            }
        }

        private Medicos CriarMedico(vmRequisitar vm)
        {
            if (vm.VmMedicos.Id > 0)
            {
                // Retorna apenas o Id para vinculação no EF
                return new Medicos { Id = vm.VmMedicos.Id };
            }
            else
                // Se não existe, mas foi informado CRM e Nome, cria novo
                return new Medicos
                {
                    NomeMedico = (vm.VmMedicos.NomeMedico ?? "").ToUpperInvariant(),
                    CRM = (vm.VmMedicos.CRM ?? "").ToUpperInvariant()
                };
        }

        private List<Requisitar> CriarRequisicoes(vmRequisitar vm)
        {
            // ObterDataHoraUtc() retorna UTC do servidor PostgreSQL — fonte canônica
            // Fallback: DateTime.UtcNow do servidor de aplicação
            DateTime dataIni = _geralController.ObterDataHoraUtc();
            // DataEntregaParcial vem do cliente como horário local — converter para UTC
            // antes de gravar em timestamptz (Npgsql 8.x rejeita Unspecified)
            DateTime dataEntregaParcial = vm.DataEntregaParcial.HasValue
                ? _geralController.ConverterLocalParaUtc(vm.DataEntregaParcial.Value)
                : dataIni.AddDays(7);

            var listaRequisitar = new List<Requisitar>();

            int ordem = 0;

            // Para cada item do cupom, cria um Requisitar separado
            if (vm.ListaCupom != null)
            {
                foreach (var itemCupom in vm.ListaCupom)
                {
                    var requisicao = new Requisitar();
                    ordem++;

                    // Dados para serem salvos na Requisição
                    // Vincula IDs e navegações
                    requisicao.PacienteId = vm.VmPacientes.Id;
                    requisicao.ClasseExamesId = itemCupom.ExameId;       // Título da Folha
                    requisicao.ClasseExamesNome = (itemCupom.RefExame ?? "").ToUpperInvariant();    // Nome da Folha
                    requisicao.ExameId = itemCupom.ExameId;
                    requisicao.OrdemItem = ordem;
                    requisicao.RefExame = (itemCupom.RefExame ?? "").ToUpperInvariant();
                    requisicao.RefItem = itemCupom.RefItem;
                    requisicao.ContaExame = itemCupom.ContaExame;
                    requisicao.InstituicaoId = vm.VmInstituicao.Id;
                    requisicao.PostoId = vm.VmPostos.Id;
                    requisicao.TabelaExamesId = vm.VmTabelaExames.Id;
                    requisicao.MedicoId = vm.VmMedicos.Id;

                    // Dados do Laboratóio de Apoio e materiais enviados
                    //requisicao.LaboratorioApoio =
                    //requisicao.ControleApoio =
                    //requisicao.LaboratorioExterno =
                    //requisicao.MaterialSaida =

                    // Dados do exame
                    requisicao.Descricao = itemCupom.Descricao;
                    requisicao.ValorItem = itemCupom.ValorItem ?? 0.00m;
                    requisicao.Etiquetas = itemCupom.Etiquetas;

                    // Datas
                    requisicao.DataIni = dataIni;
                    requisicao.DataEntregaParcial = dataEntregaParcial;

                    // Flags de controle
                    requisicao.Liberado = 0;
                    requisicao.Baixado = 0;

                    listaRequisitar.Add(requisicao);
                }
            }

            return listaRequisitar;
        }

        private async Task<int> PersistirDadosRequisitarAsync(List<Requisitar> listaRequisitar, int pacienteId, int medicoId)
        {
            // Associa paciente, médico e instituição a cada requisição
            foreach (var r in listaRequisitar)
            {
                r.PacienteId = pacienteId;   // Associa por navegação e nunca será nulo ou não poderia ser
                r.MedicoId = medicoId;       // Associa por navegação e nunca será nulo ou não poderia ser
            }

            // Adiciona todas as requisições
            _db.Requisitar.AddRange(listaRequisitar);

            // Salva tudo de uma vez. EF Core detecta novas entidades e gera IDs
            await _db.SaveChangesAsync();

            // Retorna o Id da primeira requisição (ou outro critério)
            return listaRequisitar.FirstOrDefault()?.Id ?? 0;
        }

        //Gera o código sequencial do exame por instituição
        // Feito pelo Qoder em 21/04/2026 — substituído UPDLOCK/ROWLOCK (SQL Server) por FOR UPDATE (PostgreSQL)
        private async Task<int> GeraSequencialAsync(string siglaInstituicao, DbContext? dbTransacional = null)
        {
            if (string.IsNullOrWhiteSpace(siglaInstituicao))
                throw new ArgumentException("A sigla da instituição é obrigatória para gerar o sequencial.");

            // Usa o dbContext passado (de transação externa) ou cria transação própria
            var db = (dbTransacional as Db) ?? _db;
            bool transacaoExterna = dbTransacional != null;

            int seq;

            // Só cria transação própria se não veio uma transação de fora
            var transaction = transacaoExterna ? null : await db.Database.BeginTransactionAsync();

            try
            {
                // Busca a instituição com lock pessimista PostgreSQL (FOR UPDATE)
                var sigla = siglaInstituicao.Trim();
                var instituicao = await db.Instituicao
                    .FromSqlRaw(@"SELECT * FROM ""Instituicao"" WHERE ""Sigla"" = {0} FOR UPDATE", sigla)
                    .FirstOrDefaultAsync();

                if (instituicao == null)
                    throw new InvalidOperationException("Instituição não encontrada!");

                // Incrementa o sequencial
                seq = instituicao.Sequencial + 1;

                if (seq > 999_999_998) // limite de 9 dígitos
                    seq = 1;

                // Atualiza e salva
                instituicao.Sequencial = seq;
                await db.SaveChangesAsync();

                // Confirma transação própria (se não veio de fora)
                if (transaction != null)
                    await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
            }
            return seq;
        }
        //..Qoder

        // Feito pelo Qoder em 21/04/2026 — método agora participa da transação externa passada por SalvarRequisicao
        //Feito pelo Kiro em 03/05/2026
        // APENAS INCLUSÃO NOVA — cria header ExamesRealizados + ItensExamesRealizados.
        // A edição é controlada pelo orquestrador SalvarRequisicao.
        // Retorna o ExameRealizadoId gerado para propagar ao Requisitar.
        private async Task<int> SalvarExameRealizadoAsync(vmRequisitar vm, List<Requisitar> listaRequisitar)
        {
            if (vm == null || listaRequisitar == null || !listaRequisitar.Any())
                return 0;

            try
            {
                var primeiroRequisitar = listaRequisitar.First();
                int seq = await GeraSequencialAsync(vm.VmInstituicao?.Sigla!, _db);

                var exame = new ExamesRealizados
                {
                    PacienteId = vm.VmPacientes?.Id ?? 0,
                    TabelaExamesId = vm.VmTabelaExames?.Id ?? 0,
                    InstituicaoId = vm.VmInstituicao?.Id ?? 0,
                    PostoId = vm.VmPostos?.Id ?? 0,
                    MedicoId = vm.VmMedicos?.Id ?? 0,
                    Sequencial = seq,
                    LaboratorioApoio = vm.LaboratorioApoio,
                    ControleApoio = vm.ControleApoio ?? string.Empty,
                    DataIni = primeiroRequisitar.DataIni,
                    Liberacao = 0,
                    DataExame = _geralController.ObterDataHoraUtc(),
                    DataColeta = primeiroRequisitar.DataIni.ToString("yyyy-MM-dd"),
                    Baixado = 0,
                    EnviarEmail = 0,
                    Situacao = 0,
                    TotalImpresso = 0
                };

                _db.ExamesRealizados.Add(exame);
                await _db.SaveChangesAsync();

                // Insere itens vinculados ao novo header
                int ordemItem = 0;
                var itensExames = new List<ItensExamesRealizados>();

                foreach (var item in listaRequisitar)
                {
                    itensExames.Add(new ItensExamesRealizados
                    {
                        PacienteId = vm.VmPacientes!.Id,
                        ClasseExamesId = item.ClasseExamesId,
                        ClasseExamesNome = item.ClasseExamesNome,
                        ExameRealizadoId = exame.Id,
                        TabelaExamesId = vm.VmTabelaExames!.Id,
                        OrdemItem = ++ordemItem,
                        RefExame = item.RefExame!,
                        RefItem = item.RefItem!,
                        ContaExame = item.ContaExame,
                        Descricao = item.Descricao,
                        ValorItem = item.ValorItem,
                        Etiquetas = item.Etiquetas,
                        InstituicaoId = vm.VmInstituicao!.Id,
                        Sequencial = exame.Sequencial,
                        LaboratorioApoio = item.LaboratorioApoio,
                        ControleApoio = item.ControleApoio,
                        LaboratorioExterno = item.LaboratorioExterno,
                        MaterialSaida = item.MaterialSaida,
                        MaterialRetorno = item.MaterialRetorno,
                        DataEntregaParcial = item.DataEntregaParcial,
                        Liberado = 0,
                        Baixado = 0
                    });
                }

                _db.ItensExamesRealizados.AddRange(itensExames);
                await _db.SaveChangesAsync();

                return exame.Id;
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer($"Erro ao salvar exame realizado: {ex.Message}", "Error");
                return 0;
            }
        }
        //..Kiro


        [HttpPost]
        [Route("SalvarRequisicao")]
        [Produces("application/json")]
        public async Task<IActionResult> SalvarRequisicao(vmRequisitar vm, int registroID)
        {
            string? usuarioId = HttpContext.Session.GetString("SessionEmail");
            if (string.IsNullOrEmpty(usuarioId))
                return Unauthorized(new { titulo = "Acesso negado", mensagem = "Usuário não autenticado", sucesso = false });

            usuarioId ??= "anonimo";
            vm.ListaCupom = ListaAcumulativa.Instancia.ObterCupom(usuarioId);
            string redirecionaUrl = "Requisitar".MontaUrl(HttpContext.Request);

            var validacao = ValidarDadosDominio(vm);
            if (validacao is not null)
                return Ok(validacao);

            if (vm == null)
                return BadRequest("Dados inválidos.");

            try
            {
                // === Valida o Id do Posto que precisa ser no mínimo 0 ou maior ===
                if (!vm.ValidarPostoId(_db))
                {
                    ModelState.AddModelError(nameof(vm.PostoId), "O 'Id' do Posto foi recusado.");
                    return Ok(new ApiResult(false, "Falha ao salvar dados, onde o 'Id' do Posto foi recusado.", redirecionaUrl, null));
                }

                // === PACIENTE === (fora da transação)
                Pacientes paciente = await CriarOuAtualizarPacienteAsync(vm);
                await _db.SaveChangesAsync();
                vm.VmPacientes.Id = paciente.Id;

                // === MÉDICO === (fora da transação)
                if (vm.VmMedicos.Id == 0)
                {
                    Medicos medico = CriarMedico(vm);
                    _db.Medicos.Add(medico);
                    await _db.SaveChangesAsync();
                    vm.VmMedicos.Id = medico.Id;
                }

                // === INÍCIO DA TRANSAÇÃO ===
                using var transaction = await _db.Database.BeginTransactionAsync();

                //Feito pelo Kiro em 03/05/2026
                // ORQUESTRADOR: SalvarRequisicao controla toda a exclusão e inserção.
                // SalvarExameRealizadoAsync é responsável apenas por header + itens (sem exclusão).
                // Decisão inclusão vs edição: exclusivamente por ExameRealizadoId.

                int exameRealizadoId = vm.ExameRealizadoId;
                List<Requisitar> listaRequisitar = CriarRequisicoes(vm);

                // Variáveis locais validadas — ValidarDadosDominio garante que não são nulos
                int pacienteIdValidado = vm.VmPacientes?.Id ?? 0;
                int medicoIdValidado = vm.VmMedicos?.Id ?? 0;
                int tabelaExamesIdValidado = vm.VmTabelaExames?.Id ?? 0;

                if (exameRealizadoId > 0)
                {
                    // === EDIÇÃO ===
                    // 1. Validar: buscar header existente
                    var headerExistente = await _db.ExamesRealizados.FindAsync(exameRealizadoId);
                    if (headerExistente == null)
                    {
                        await transaction.RollbackAsync();
                        return Ok(new ApiResult(false, $"Exame Realizado Id={exameRealizadoId} não encontrado. Não é possível editar.", redirecionaUrl, null));
                    }

                    // 2. Atualizar header (sem recriar)
                    headerExistente.TabelaExamesId = vm.VmTabelaExames?.Id ?? headerExistente.TabelaExamesId;
                    headerExistente.InstituicaoId = vm.VmInstituicao?.Id ?? headerExistente.InstituicaoId;
                    headerExistente.PostoId = vm.VmPostos?.Id ?? headerExistente.PostoId;
                    headerExistente.MedicoId = vm.VmMedicos?.Id ?? headerExistente.MedicoId;
                    headerExistente.LaboratorioApoio = vm.LaboratorioApoio;
                    headerExistente.ControleApoio = vm.ControleApoio ?? string.Empty;

                    // 3. Excluir ItensExamesRealizados por ExameRealizadoId
                    var itensAntigos = await _db.ItensExamesRealizados
                        .Where(i => i.ExameRealizadoId == exameRealizadoId)
                        .ToListAsync();
                    if (itensAntigos.Any())
                        _db.ItensExamesRealizados.RemoveRange(itensAntigos);

                    // 4. Excluir Requisitar por ExameRealizadoId
                    var requisitarAntigos = await _db.Requisitar
                        .Where(r => r.ExameRealizadoId == exameRealizadoId)
                        .ToListAsync();
                    if (requisitarAntigos.Any())
                        _db.Requisitar.RemoveRange(requisitarAntigos);

                    await _db.SaveChangesAsync();

                    // 5. Inserir novos ItensExamesRealizados
                    int ordemItem = 0;
                    var novosItens = new List<ItensExamesRealizados>();
                    foreach (var item in listaRequisitar)
                    {
                        novosItens.Add(new ItensExamesRealizados
                        {
                            PacienteId = vm.VmPacientes!.Id,
                            ClasseExamesId = item.ClasseExamesId,
                            ClasseExamesNome = item.ClasseExamesNome,
                            ExameRealizadoId = exameRealizadoId,
                            TabelaExamesId = vm.VmTabelaExames!.Id,
                            OrdemItem = ++ordemItem,
                            RefExame = item.RefExame!,
                            RefItem = item.RefItem!,
                            ContaExame = item.ContaExame,
                            Descricao = item.Descricao,
                            ValorItem = item.ValorItem,
                            Etiquetas = item.Etiquetas,
                            InstituicaoId = vm.VmInstituicao!.Id,
                            Sequencial = headerExistente.Sequencial,
                            LaboratorioApoio = item.LaboratorioApoio,
                            ControleApoio = item.ControleApoio,
                            LaboratorioExterno = item.LaboratorioExterno,
                            MaterialSaida = item.MaterialSaida,
                            MaterialRetorno = item.MaterialRetorno,
                            DataEntregaParcial = item.DataEntregaParcial,
                            Liberado = 0,
                            Baixado = 0
                        });
                    }
                    _db.ItensExamesRealizados.AddRange(novosItens);

                    // 6. Inserir novos Requisitar com ExameRealizadoId
                    foreach (var req in listaRequisitar)
                    {
                        req.ExameRealizadoId = exameRealizadoId;
                    }
                    registroID = await PersistirDadosRequisitarAsync(listaRequisitar, pacienteIdValidado, medicoIdValidado);

                    await _db.SaveChangesAsync();
                }
                else
                {
                    // === INCLUSÃO NOVA ===
                    // SalvarExameRealizadoAsync cria header + itens e retorna o Id
                    exameRealizadoId = await SalvarExameRealizadoAsync(vm, listaRequisitar);
                    if (exameRealizadoId <= 0)
                    {
                        await transaction.RollbackAsync();
                        return Ok(new ApiResult(false, "Falha ao salvar dados na tabela de Exames.", redirecionaUrl, null));
                    }

                    // Propagar ExameRealizadoId a todos os registros Requisitar
                    foreach (var req in listaRequisitar)
                    {
                        req.ExameRealizadoId = exameRealizadoId;
                    }
                    registroID = await PersistirDadosRequisitarAsync(listaRequisitar, pacienteIdValidado, medicoIdValidado);
                }

                if (registroID <= 0)
                {
                    await transaction.RollbackAsync();
                    return Ok(new ApiResult(false, "Falha ao salvar dados na tabela de Requisitos.", redirecionaUrl, null));
                }
                //..Kiro

                await transaction.CommitAsync();

                // Limpa cupom do usuário
                ListaAcumulativa.Instancia.EsvaziarCupom(usuarioId);

                // Retorna JSON com sucesso — o JavaScript chama CupomRequisicao separadamente para impressão
                // action=null para NÃO redirecionar após fechar o modal de sucesso (permanece na tela)
                return Ok(new
                {
                    sucesso = true,
                    titulo = "Sucesso",
                    mensagem = "Requisição salva com sucesso!",
                    pacienteId = pacienteIdValidado,
                    tabelaExamesId = tabelaExamesIdValidado,
                    exameRealizadoId = exameRealizadoId,
                    action = (string?)null
                });
            }
            catch (Exception ex)
            {
                var detalhe = ex.InnerException != null
                    ? $"{ex.Message} | Inner: {ex.InnerException.Message}"
                    : ex.Message;
                _eventLogHelper.LogEventViewer($"Erro ao tentar salvar requisição: {detalhe}\n{ex.StackTrace}", "Error");  
                return StatusCode(500, $"Erro ao tentar salvar requisição: {detalhe}");
            }
        }


        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModalPacientes")]
        public async Task<ActionResult> ModalPacientes(vmRequisitar vm)
        {
            List<Pacientes> dados = [];

            dados = await _db.Pacientes.AsNoTracking().Take(1000).ToListAsync();

            vm.ListaPacientes = dados;

            //Parâmetros auxiliares em ViewBag
            ViewBag.ListaPacientes = dados;
            ViewBag.TextoMenu = new object[] { "Consulta Tabelas de Pacientes", false };
            //Finalização para a View
            _geralController.Validacao("ModalPacientes", "Tabela de Pacientes", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModalInstituicoes")]
        public async Task<ActionResult> ModalInstituicoes(vmRequisitar vm)
        {
            ICollection<Instituicao> dados = [];

            dados = await _db.Instituicao.AsNoTracking().Take(1000).ToListAsync();

            //Parâmetros auxiliares em ViewBag
            ViewBag.ListaIntituicoes = dados;
            ViewBag.TextoMenu = new object[] { "Consulta Instituições", false };
            //Finalização para a View
            _geralController.Validacao("ModalInstituicoes", "Instituições", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModalPostos")]
        public async Task<ActionResult> ModalPostos(vmRequisitar vm)
        {
            ICollection<Postos> dados = [];

            dados = await _db.Postos.AsNoTracking().Take(1000).ToListAsync();

            //Parâmetros auxiliares em ViewBag
            ViewBag.ListaPostos = dados;
            ViewBag.TextoMenu = new object[] { "Consulta Postos de Coleta", false };
            //Finalização para a View
            _geralController.Validacao("ModalPostos", "Postos", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModalTabelas")]
        public async Task<ActionResult> ModalTabelas(vmRequisitar vm)
        {
            ICollection<TabelaExames> dados = [];

            dados = await _db.TabelaExames.AsNoTracking().Take(1000).ToListAsync();

            //Parâmetros auxiliares em ViewBag
            ViewBag.ListaTabelas = dados;
            ViewBag.TextoMenu = new object[] { "Consulta Tabelas de Exames", false };
            //Finalização para a View
            _geralController.Validacao("ModalTabelas", "Tabela de Exames", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModalMedicos")]
        public async Task<ActionResult> ModalMedicos(vmRequisitar vm)
        {
            List<Medicos> dados = [];

            dados = await _db.Medicos.AsNoTracking().Take(1000).ToListAsync();

            vm.ListaMedicos = dados;

            //Parâmetros auxiliares em ViewBag
            ViewBag.ListaMedicos = dados;
            ViewBag.TextoMenu = new object[] { "Consulta Tabelas de Médicos", false };
            //Finalização para a View
            _geralController.Validacao("ModalMedicos", "Tabela de Médicos", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        /* Manipulando as variáveis do Modal para Instituições, Não mostra References mas está sendo utilizado */

        [HttpGet]
        [Route("RetornoDoModalPacientes")]
        public async Task<JsonResult> RetornoDoModalPacientes(vmRequisitar vm, string id)
        {
            string busca = id.Trim().ToUpper();

            var dados = await _db.Pacientes.Where(c => c.NomePaciente.Contains(busca)).AsNoTracking().FirstOrDefaultAsync();

            if (dados == null)
                return Json(new { success = false, message = "Paciente não encontrado." });

            if (dados != null)
            {   //Monta a Requisição (vmRequisitar)...
                vm.PacienteId = dados.Id;         //Id do Paciente para localizar pro salvamento
                vm.CPFPaciente = dados.CPF;
                vm.NomePaciente = dados.NomePaciente.ToCapitalizeNotNull();
                vm.Nascimento = dados.Nascimento.ToString("yyyy-MM-dd");
                vm.Email = (dados.Email ?? "").SafeLower();

                vm.NomeSocial = dados.NomeSocial;
                vm.NomeMae = dados.NomeMae;
                vm.NomePai = dados.NomePai;
                vm.IdPacienteExterno = dados.IdPacienteExterno;
                vm.TipoDocumento = dados.TipoDocumento;
                vm.CarteiraSUS = dados.CarteiraSUS;
                vm.Identidade = dados.Identidade;
                vm.Emissor = dados.Emissor;
                vm.Cor = dados.Cor;
                vm.EtniaIndigena = dados.EtniaIndigena;
                vm.TipoSanguineo = dados.TipoSanguineo;

                vm.Sexo = dados.Sexo;
                vm.EstadoCivil = dados.EstadoCivil;
                vm.Naturalidade = dados.Naturalidade;
                vm.Nacionalidade = dados.Nacionalidade;
                vm.Profissao = dados.Profissao;
                vm.CEP = dados.CEP;
                vm.Logradouro = dados.Logradouro;
                vm.Endereco = dados.Endereco;
                vm.Numero = dados.Numero;
                vm.Complemento = dados.Complemento;
                vm.Bairro = dados.Bairro;
                vm.Cidade = dados.Cidade;
                vm.UF = dados.UF;
                vm.Telefone = dados.Telefone;
                vm.DUM = dados.DUM?.ToString("yyyy-MM-dd");
                vm.TempoGestacao = dados.TempoGestacao;
                vm.Observacao = dados.Observacao;

            }
            else return Json(new { success = false, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!

            return Json(new { success = true, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!
        }
        //..

        /* Manipulando as variáveis do Modal para Instituições, Não mostra References mas está sendo utilizado */
        [HttpGet]
        [Route("RetornoDoModalInstituicoes")]
        public async Task<JsonResult> RetornoDoModalInstituicoes(vmRequisitar vm, string id)
        {
            string busca = id.Trim().ToUpper();

            var dados = await _db.Instituicao.Where(c => c.Sigla.Contains(busca) || c.Nome.Contains(busca)).AsNoTracking().FirstOrDefaultAsync();

            if (dados == null)
                return Json(new { success = false, message = "Instituição não encontrada." });

            if (dados != null)
            {   //Monta a Requisição (vmRequisitar)...
                vm.InstituicaoId = dados.Id;         //Id da Instituição para localizar pro salvamento
                vm.SiglaInstituicao = dados.Sigla;
                vm.NomeInstituicao = dados.Nome;
            }
            else return Json(new { success = false, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!

            return Json(new { success = true, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!
        }
        //..

        /* Manipulando as variáveis do Modal para Postos de Coletas, Não mostra References mas está sendo utilizado */
        [HttpGet]
        [Route("RetornoDoModalPostos")]
        public async Task<JsonResult> RetornoDoModalPostos(vmRequisitar vm, string id)
        {
            string busca = id.Trim().ToUpper();

            var dados = await _db.Postos.Where(c => c.NomePosto.Contains(busca)).AsNoTracking().FirstOrDefaultAsync();

            if (dados == null)
                return Json(new { success = false, message = "Posto de Coleta não encontrado." });

            if (dados != null)
            {   //Monta a Requisição (vmRequisitar)...
                vm.PostoId = dados.Id;         //Id do Posto para localizar pro salvamento
                vm.NomePosto = dados.NomePosto;
            }
            else return Json(new { success = false, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!

            return Json(new { success = true, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!
        }
        //..

        /* Manipulando as variáveis do Modal para Médicos (RETORNO PARA OS CAMPOS DO MÉDICO ESCOLHIDO), Não mostra References mas está sendo utilizado */
        [HttpGet]
        [Route("RetornoDoModalMedico")]
        public async Task<JsonResult> RetornoDoModalMedico(vmRequisitar vm, string id)
        {
            string busca = id.Trim().ToUpper();

            var dados = await _db.Medicos.Where(c => c.NomeMedico.Contains(busca) || c.CRM.Contains(busca)).AsNoTracking().FirstOrDefaultAsync();

            if (dados == null)
                return Json(new { success = false, message = "Médico não encontrado." });

            if (dados != null)
            {   //Monta a Requisição (vmRequisitar)...
                vm.MedicoId = dados.Id;         //Id do Médico na Instituição para localizar pro salvamento
                vm.CRM = dados.CRM;             //CRM do Médico
                vm.NomeMedico = dados.NomeMedico;
            }
            else return Json(new { success = false, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon quando não houver sucesso!

            return Json(new { success = true, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon quando houver sucesso!
        }
        //..

        /* Manipulando as variáveis do Modal para Tabela de Preço, Não mostra References mas está sendo utilizado */
        [HttpGet]
        [Route("RetornoDoModalTabela")]
        public async Task<JsonResult> RetornoDoModalTabela(vmRequisitar vm, string id)
        {
            string usuarioId = HttpContext.Session.GetString("SessionEmail") ?? "anonimo";
            //Esvaziar a lista acumulativa
            ListaAcumulativa.Instancia.EsvaziarCupom(usuarioId);

            string busca = id.Trim().ToUpper();

            var dados = await _db.TabelaExames.Where(c => c.SiglaTabela.Contains(busca) || c.NomeTabela.Contains(busca)).AsNoTracking().FirstOrDefaultAsync();

            if (dados == null)
                return Json(new { success = false, message = "Tabela não encontrada." });

            if (dados != null)
            {   //Monta a Requisição (vmRequisitar)...
                vm.TabelaExamesId = dados.Id;        //Id da Tabela de Exames para localizar pro salvamento
                vm.SiglaTabela = dados.SiglaTabela;  //Sigla da Tabela de Exames
                vm.NomeTabela = dados.NomeTabela;
            }
            else return Json(new { success = false, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!

            return Json(new { success = true, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!
        }
        //..

        //ESTE MÉTODO NÃO ESTÁ APARECENDO O APONTAMENTO DE "0 references", MAS ELE É SIM UTILIZADO no _PartialLancarExames.cshtml!!!
        [HttpGet]
        [Route("PartialLancarExames")]
        public async Task<ActionResult> PartialLancarExames(int tabelaExamesId)
        {
            ICollection<vmPlanoExames> listaGrid = [];

            vmPlanoExames resultado = new();

            //Filtra todos os exames da TABELA (TabelaExamesId) que estão no Plano de Exames
            List<PlanoExames> dados = await _db.PlanoExames.Where(s => s.TabelaExamesId == tabelaExamesId && s.NaoMostrar == 0).OrderBy(o => o.ContaExame).AsNoTracking().ToListAsync();

            foreach (PlanoExames? item in dados)
            {
                resultado = new vmPlanoExames()
                {
                    Id = item.Id,
                    ContaExame = item.ContaExame,
                    Descricao = item.Descricao,
                    ValorItem = item.ValorItem
                };
                listaGrid.Add(resultado);
            }

            ViewBag.ListaDeExames = listaGrid;

            return PartialView("Partials/_PartialLancarExames");
        }
        //..

        [HttpGet]
        [Route("IncluirExameCupom")]
        public async Task<ActionResult> IncluirExameCupom(vmPlanoExames vm, string id)
        {
            int idBusca = id.ToInt32();

            PlanoExames dados = await _db.PlanoExames.Where(c => c.Id == idBusca).FirstAsync();

            if (dados != null)
            {   //Dados para montar o Cupom com os itens de exames.
                vm.Id = dados.Id;        //Id do Plano de Exames para localizar pro salvamento
                vm.TabelaExamesId = dados.TabelaExamesId; //Id da Tabela de Exames
                vm.ContaExame = dados.ContaExame; //Conta do Exame
                vm.Descricao = dados.Descricao;   //Descrição do Exame
                vm.ValorItem = dados.ValorItem == null ? "0.00".ToDecimalInvariant() : dados.ValorItem;   //Valor do Exame
            }
            else return Json(new { success = false, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!

            return Json(new { success = true, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!
        }

        //Feito pelo Qoder em 21/04/2026 — remove um item específico do cupom ao desselecionar a linha no grid
        [HttpGet]
        [Route("RemoverExameCupom")]
        public ActionResult RemoverExameCupom(vmPlanoExames vm, string id)
        {
            string usuarioId = HttpContext.Session.GetString("SessionEmail") ?? "anonimo";
            int idBusca = id.ToInt32();

            ListaAcumulativa.Instancia.RemoverItemCupom(usuarioId, idBusca);

            // Recalcula o total e retorna a partial atualizada
            decimal? totalCupom = 0;
            var lista = ListaAcumulativa.Instancia.ObterCupom(usuarioId);
            foreach (var item in lista) totalCupom += item.ValorItem;

            ViewBag.TotalCupom = totalCupom?.ToString("N2");
            ViewBag.ListaCupom = lista;

            return PartialView("Partials/_PartialMontarItensCupom");
        }
        //..Qoder

        //..

        [HttpGet]
        [Route("PartialMontarItensCupom")]
        public async Task<ActionResult> PartialMontarItensCupom(vmRequisitar vm, string id)   //monta apenas os registros dentro do grid do Cupom
        {
            decimal? totalCupom = 0;

            //Identificador do usuário atual (pode ser User.Identity.Name ou SessionId) para isolar os dados do usuário na lista estática.
            //Se não isolar por usuário, todos verão a mesma lista acumulada.
            string usuarioId = HttpContext.Session.GetString("SessionEmail") ?? "anonimo";

            if (id == "0")
            {  //Esvaziar a lista acumulativa
                ListaAcumulativa.Instancia.EsvaziarCupom(usuarioId);
            }
            else
            {
                int idBusca = id.ToInt32();

                ICollection<PlanoExames> dados = [];

                //Feito pelo Kiro em 02/05/2026
                // Busca o PlanoExames pelo Id sem filtro de valor para poder
                // validar e avisar o usuário se o item não tem valor definido.
                var itemBuscado = await _db.PlanoExames.Where(s => s.Id == idBusca).AsNoTracking().FirstOrDefaultAsync();

                if (itemBuscado == null)
                {
                    ViewBag.TotalCupom = "0,00";
                    ViewBag.ListaCupom = ListaAcumulativa.Instancia.ObterCupom(usuarioId);
                    ViewBag.MensagemErro = "Item de exame não encontrado.";
                    return PartialView("Partials/_PartialMontarItensCupom");
                }

                if (itemBuscado.ValorItem == null || itemBuscado.ValorItem <= 0)
                {
                    ViewBag.TotalCupom = "0,00";
                    ViewBag.ListaCupom = ListaAcumulativa.Instancia.ObterCupom(usuarioId);
                    ViewBag.MensagemErro = "Item sem valor definido, não pode ser selecionado.";
                    return PartialView("Partials/_PartialMontarItensCupom");
                }

                dados = new List<PlanoExames> { itemBuscado };
                //..Kiro

                //Adicionando linhas no Cupom, a cada vez que entrar por este método "PartialMontarCupom"
                ListaAcumulativa.Instancia.AdicionarCupom(usuarioId, dados);

                if (vm.ListaCupom == null)
                {
                    vm.ListaCupom = [];
                }
                vm.ListaCupom = ListaAcumulativa.Instancia.ObterCupom(usuarioId);  //obtém a lista acumulada dos itens do cupom

                //Totaliza o resultado do Cupom
                foreach (PlanoExames item in vm.ListaCupom)
                {
                    totalCupom += item.ValorItem;
                }
            }
            //Parâmetros auxiliares em ViewBag
            ViewBag.TotalCupom = totalCupom?.ToString("N2");
            ViewBag.ListaCupom = vm.ListaCupom;

            return PartialView("Partials/_PartialMontarItensCupom");
        }
        //..

        //Modelo do Layout do Cupom:
        /*
         LABORATORIO BARROS
         Medicina Laboratorial
         ----------------------------------------
         CNPJ: 02.557.289/0001-70
         ----------------------------------------
         TeleFax: (34) 3263-2010
         ----------------------------------------
            * * * CUPOM SEM VALOR FISCAL * * *   
         ----------------------------------------
         HOJE: 27/04/2023 HORA: 19:47 horas.
         ----------------------------------------
         CÓDIGO DE EXAME Nº 80720
         CÓDIGO PACIENTE/NOME Nº 288
         ANGELO BARROS
         ----------------------------------------
         DATA PREVISTA: 05/05/2023 PARA RESULTADO,
         OBSERVANDO A DISPONIBILIDADE PARA:

         1) GLICOSE 
         ----------------------------------------
         Alguns exames podem ultrapassar a data  
         inicialmente prevista para resultado    
         devido às condições técnicas exigidas   
         para as análises. Obrigado.             
         ----------------------------------------
         #                                       
         #      OBRIGADO PELA PREFERÊNCIA!       
         #                                       
         ----------------------------------------
         */

        //Feito pelo Kiro em 01/05/2026
        /// <summary>
        /// Carrega todos os dados de uma requisição do paciente na data para edição no formulário.
        /// Bloqueia o carregamento se qualquer item já possuir resultado lançado.
        /// </summary>
        [HttpGet]
        [Route("Requisitar/CarregarRequisicaoParaEdicao")]
        public IActionResult CarregarRequisicaoParaEdicao(int pacienteId, string data, int tabelaExamesId = 0)
        {
            if (pacienteId <= 0 || string.IsNullOrWhiteSpace(data))
                return Json(new { sucesso = false, mensagem = "Dados inválidos para carregamento." });

            // Converte data recebida (dd/MM/yyyy) para DateTime
            if (!DateTime.TryParseExact(data, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime dataConsulta))
                return Json(new { sucesso = false, mensagem = "Formato de data inválido." });

            // Converte data local para range UTC — necessário para comparar com timestamptz no Npgsql 8.x
            var (dataInicio, dataFim) = _geralController.ConverterDataLocalParaRangeUtc(dataConsulta.Date);

            //Feito pelo Kiro em 02/05/2026
            // Filtra por PacienteId + Data + TabelaExamesId para identificar
            // a requisição específica quando o paciente tem múltiplas no mesmo dia.
            var query = _db.Requisitar
                .Include(r => r.Pacientes)
                .Include(r => r.Medicos)
                .Include(r => r.Instituicao)
                .Include(r => r.Posto)
                .Include(r => r.TabelaExames)
                .Where(r => r.PacienteId == pacienteId
                         && r.DataIni >= dataInicio
                         && r.DataIni <= dataFim);

            if (tabelaExamesId > 0)
                query = query.Where(r => r.TabelaExamesId == tabelaExamesId);

            var itens = query
                .OrderBy(r => r.OrdemItem)
                .AsNoTracking()
                .ToList();
            //..Kiro

            if (!itens.Any())
                return Json(new { sucesso = false, mensagem = "Nenhuma requisição encontrada para este paciente na data informada." });

            // Verifica se algum item já possui resultado lançado
            bool temResultado = itens.Any(r => !string.IsNullOrWhiteSpace(r.Resultado));
            if (temResultado)
                return Json(new { sucesso = false, mensagem = "Esta requisição não pode ser editada pois um ou mais exames já possuem resultado lançado." });

            var primeiro = itens.First();

            // Monta a lista de itens do cupom para recarregar no formulário
            var listaCupom = itens.Select(r => new
            {
                id          = r.Id,
                contaExame  = r.ContaExame,
                descricao   = r.Descricao,
                valorItem   = r.ValorItem,
                refExame    = r.RefExame,
                refItem     = r.RefItem,
                exameId     = r.ExameId,
                etiquetas   = r.Etiquetas
            }).ToList();

            var resultado = new
            {
                sucesso    = true,
                pacienteId = primeiro.PacienteId,
                nomePaciente      = primeiro.Pacientes?.NomePaciente ?? "",
                nascimento        = primeiro.Pacientes?.Nascimento.ToString("yyyy-MM-dd") ?? "",
                cpfPaciente       = primeiro.Pacientes?.CPF ?? "",
                telefone          = primeiro.Pacientes?.Telefone ?? "",
                email             = primeiro.Pacientes?.Email ?? "",
                nomeMae           = primeiro.Pacientes?.NomeMae ?? "",
                naturalidade      = primeiro.Pacientes?.Naturalidade ?? "",
                nacionalidade     = primeiro.Pacientes?.Nacionalidade ?? "",
                profissao         = primeiro.Pacientes?.Profissao ?? "",
                cep               = primeiro.Pacientes?.CEP ?? "",
                logradouro        = primeiro.Pacientes?.Logradouro ?? "",
                endereco          = primeiro.Pacientes?.Endereco ?? "",
                numero            = primeiro.Pacientes?.Numero ?? "",
                complemento       = primeiro.Pacientes?.Complemento ?? "",
                bairro            = primeiro.Pacientes?.Bairro ?? "",
                cidade            = primeiro.Pacientes?.Cidade ?? "",
                uf                = primeiro.Pacientes?.UF ?? "",
                observacao        = primeiro.Pacientes?.Observacao ?? "",
                sexo              = primeiro.Pacientes?.Sexo ?? "",
                estadoCivil       = (int)(primeiro.Pacientes?.EstadoCivil ?? 0),
                tempoGestacao     = (int)(primeiro.Pacientes?.TempoGestacao ?? 0),
                dum               = primeiro.Pacientes?.DUM?.ToString("yyyy-MM-dd") ?? "",
                medicoId          = primeiro.MedicoId,
                nomeMedico        = primeiro.Medicos?.NomeMedico ?? "",
                crm               = primeiro.Medicos?.CRM ?? "",
                instituicaoId     = primeiro.InstituicaoId,
                siglaInstituicao  = primeiro.Instituicao?.Sigla ?? "",
                nomeInstituicao   = primeiro.Instituicao?.Nome ?? "",
                postoId           = primeiro.PostoId ?? 0,
                nomePosto         = primeiro.Posto?.NomePosto ?? "",
                tabelaExamesId    = primeiro.TabelaExamesId,
                siglaTabela       = primeiro.TabelaExames?.SiglaTabela ?? "",
                nomeTabela        = primeiro.TabelaExames?.NomeTabela ?? "",
                dataIni           = primeiro.DataIni.ToString("dd/MM/yyyy"),
                //Feito pelo Kiro em 03/05/2026
                exameRealizadoId  = primeiro.ExameRealizadoId ?? 0,
                //..Kiro
                listaCupom
            };

            return Json(resultado);
        }
        //..Kiro

        //Feito pelo Kiro em 02/05/2026
        /// <summary>
        /// Recarrega o cupom de exames para edição de uma requisição existente.
        /// Busca os itens da requisição do paciente na data, localiza os PlanoExames
        /// correspondentes por ContaExame + TabelaExamesId, popula a ListaAcumulativa,
        /// e retorna a partial _PartialMontarItensCupom renderizada.
        /// </summary>
        [HttpGet]
        [Route("Requisitar/CarregarCupomEdicao")]
        public async Task<ActionResult> CarregarCupomEdicao(int pacienteId, string data)
        {
            string usuarioId = HttpContext.Session.GetString("SessionEmail") ?? "anonimo";

            // Esvazia o cupom atual antes de recarregar
            ListaAcumulativa.Instancia.EsvaziarCupom(usuarioId);

            decimal? totalCupom = 0;

            if (pacienteId > 0 && !string.IsNullOrWhiteSpace(data))
            {
                if (DateTime.TryParseExact(data, "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime dataConsulta))
                {
                    // Converte data local para range UTC — necessário para comparar com timestamptz no Npgsql 8.x
                    var (dataInicio, dataFim) = _geralController.ConverterDataLocalParaRangeUtc(dataConsulta.Date);

                    // Busca os itens da requisição do paciente na data
                    var itensRequisicao = await _db.Requisitar
                        .AsNoTracking()
                        .Where(r => r.PacienteId == pacienteId
                                 && r.DataIni >= dataInicio
                                 && r.DataIni <= dataFim)
                        .Select(r => new { r.ContaExame, r.TabelaExamesId })
                        .ToListAsync();

                    if (itensRequisicao.Any())
                    {
                        // Extrai as ContaExame e o TabelaExamesId para buscar no PlanoExames
                        var contasExame = itensRequisicao.Select(r => r.ContaExame).Distinct().ToList();
                        var tabelaId = itensRequisicao.First().TabelaExamesId;

                        // Localiza os PlanoExames correspondentes
                        var planoExames = await _db.PlanoExames
                            .AsNoTracking()
                            .Where(p => p.TabelaExamesId == tabelaId
                                     && contasExame.Contains(p.ContaExame))
                            .ToListAsync();

                        // Popula a ListaAcumulativa com os PlanoExames encontrados
                        if (planoExames.Any())
                        {
                            ListaAcumulativa.Instancia.AdicionarCupom(usuarioId, planoExames);
                        }
                    }
                }
            }

            // Obtém a lista acumulada e calcula o total
            var lista = ListaAcumulativa.Instancia.ObterCupom(usuarioId);
            foreach (var item in lista)
            {
                totalCupom += item.ValorItem;
            }

            ViewBag.TotalCupom = totalCupom?.ToString("N2");
            ViewBag.ListaCupom = lista;

            return PartialView("Partials/_PartialMontarItensCupom");
        }
        //..Kiro

        //Feito pelo Kiro em 01/05/2026
        /// <summary>
        /// Exclui todos os itens de Requisitar do paciente na data.
        /// Bloqueia a exclusão se qualquer item já possuir resultado lançado.
        /// Mantém o cadastro do paciente e do médico intactos.
        /// </summary>
        [HttpPost]
        [Route("Requisitar/ExcluirRequisicao")]
        public async Task<IActionResult> ExcluirRequisicao([FromBody] CupomRequisicaoViewModel vm)
        {
            if (vm == null || vm.IdPaciente <= 0 || vm.Data == null)
                return Json(new { sucesso = false, mensagem = "Dados inválidos para exclusão." });

            // Converte data local para range UTC — necessário para comparar com timestamptz no Npgsql 8.x
            var (dataInicio, dataFim) = _geralController.ConverterDataLocalParaRangeUtc(vm.Data.Value.Date);

            var itens = await _db.Requisitar
                .Where(r => r.PacienteId == vm.IdPaciente
                         && r.DataIni >= dataInicio
                         && r.DataIni <= dataFim)
                .ToListAsync();

            if (!itens.Any())
                return Json(new { sucesso = false, mensagem = "Nenhuma requisição encontrada para exclusão." });

            // Verifica se algum item já possui resultado lançado
            bool temResultado = itens.Any(r => !string.IsNullOrWhiteSpace(r.Resultado));
            if (temResultado)
                return Json(new { sucesso = false, mensagem = "Esta requisição não pode ser excluída pois um ou mais exames já possuem resultado lançado." });

            try
            {
                using var transaction = await _db.Database.BeginTransactionAsync();

                _db.Requisitar.RemoveRange(itens);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new { sucesso = true, mensagem = $"{itens.Count} item(ns) de exame excluído(s) com sucesso." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer($"Erro ao excluir requisição: {ex.Message}", "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao excluir a requisição: " + ex.Message });
            }
        }
        //..Kiro

        //Imprimir Cupom
        [HttpGet]
        [Route("Requisitar/CupomRequisicao")]   //Rota de uma chamada javascript para imprimir o cupom de requisição
        public IActionResult CupomRequisicao([FromQuery] CupomRequisicaoViewModel vm)
        {
            if (vm == null || vm.IdPaciente <= 0)
            {
                _eventLogHelper.LogEventViewer("Bad Request ::: Dados inválidos na impressão de cupom", "wError");
                return BadRequest("Bad Request ::: Dados inválidos.");
            }
            // Se a data não vier na query, usa a data/hora atual do servidor
            var dataConsulta = vm.Data ?? _geralController.ObterDataHoraLocal().Date;

            var paciente = _db.Pacientes.Where(s => s.Id == vm.IdPaciente).FirstOrDefault();
            if (paciente == null)
            {
                _eventLogHelper.LogEventViewer("Paciente não encontrado ::: Dados inválidos na impressão de cupom", "wError");
                return NotFound("Paciente não encontrado.");
            }

            ResultadoImpressao resultado;

            //Filtra por TabelaExamesId quando disponível para evitar incluir
            // itens de outras requisições do mesmo paciente no mesmo dia.
            // Usa range UTC em vez de .Date (Npgsql 8.x + timestamptz)
            var (dataInicioUtc, dataFimUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataConsulta.Date);
            var queryExames = _db.Requisitar
                         .Where(r => r.PacienteId == vm.IdPaciente
                                  && r.DataIni >= dataInicioUtc
                                  && r.DataIni <= dataFimUtc);

            if (vm.TabelaExamesId > 0)
                queryExames = queryExames.Where(r => r.TabelaExamesId == vm.TabelaExamesId);

            var exames = queryExames.OrderBy(r => r.Id).ToList();
            //..Kiro

            if (!exames.Any())
                return Content("Nenhuma requisição de exame encontrada para esta data.", "text/plain");

            int codigoPaciente = paciente.Id;
            string nomePaciente = paciente.NomePaciente.ToUpper();

            int instituicaoId = exames.FirstOrDefault()?.InstituicaoId ?? 0;
            int tabelaExamesId = exames.FirstOrDefault()?.TabelaExamesId ?? 0;
            //Feito pelo Kiro em 03/05/2026
            // Código do exame = ExameRealizadoId (vínculo lógico com ExamesRealizados.Id)
            // Nunca usar Requisitar.Id como código do exame.
            string codigoExame = exames.FirstOrDefault()?.ExameRealizadoId?.ToString() ?? "-";
            //..Kiro 

            string nomeInstituicao = _db.Instituicao.Where(s => s.Id == instituicaoId).FirstOrDefault()?.Nome ?? "N/A";

            // Feito pelo Qoder em 21/04/2026 — consolidado em uma única consulta (antes eram 6 chamadas separadas)
            var empresa = _db.Empresa.FirstOrDefault();
            string nomeLaboratorioTitulo = empresa?.TituloEmpresa ?? "LABORATÓRIO";
            string nomeLaboratorioSubTitulo = empresa?.SubTituloEmpresa ?? "";
            string cnpjLaboratorio = "CNPJ: " + (empresa?.CNPJ.FormatarCNPJNotNull() ?? "");
            string telefoneLaboratorio = "Tel: " + (empresa?.Telefones.FormataTelefoneNotNull() ?? "");
            string emailLaboratorio = "Email: " + (empresa?.Email?.ToLower() ?? "");

            string enderecoLaboratorio = empresa?.Logradouro?.TrimEnd() + " " +
                                         empresa?.Endereco?.TrimEnd() + ", " +
                                         empresa?.Numero?.TrimEnd() +
                                         empresa?.Complemento?.TrimEnd() + " - " +
                                         empresa?.Bairro?.TrimEnd() + " - " +
                                         empresa?.Cidade?.TrimEnd() + " - " +
                                         empresa?.UF?.TrimEnd() + " - CEP: " +
                                         empresa?.CEP?.FormatarCEP();
            //..Qoder

            //Feito pelo Kiro em 20/04/2026
            // Usa ObterDataHoraLocal() — converte UTC do PostgreSQL para timezone local
            var dataServidorCupom = _geralController.ObterDataHoraLocal();
            string dataHoje     = dataServidorCupom.ToString("dd/MM/yyyy");
            string horaHoje     = dataServidorCupom.ToString("HH:mm");
            string dataPrevista = dataServidorCupom.AddDays(7).ToString("dd/MM/yyyy"); //padrão 7 dias para entrega inicial
            //..Kiro

            //Impressão do Cupom
            var sb = new StringBuilder();
            AppendTextoQuebrado(sb, nomeLaboratorioTitulo);
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, nomeLaboratorioSubTitulo);
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, cnpjLaboratorio);
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, telefoneLaboratorio);
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, emailLaboratorio);
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, enderecoLaboratorio);
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, $"   * * * CUPOM SEM VALOR FISCAL * * *   ");
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, $"HOJE: {dataHoje} HORA: {horaHoje} horas.");
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, $"CÓDIGO DE EXAME Nº {codigoExame}");
            AppendTextoQuebrado(sb, $"CÓDIGO/NOME PACIENTE Nº {codigoPaciente}");
            AppendTextoQuebrado(sb, nomePaciente);
            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, $"DATA PREVISTA: {dataPrevista} PARA RESULTADO, OBSERVANDO A DISPONIBILIDADE PARA:");
            sb.AppendLine($"");

            int contador = 0;

            foreach (var exame in exames)
            {
                contador++;
                AppendTextoQuebrado(sb, $"{contador}) {exame.Descricao}");
            }

            AppendTextoQuebrado(sb, $"-");
            AppendTextoQuebrado(sb, $"Aviso Importante ao Paciente:");
            sb.AppendLine($"");
            AppendTextoQuebrado(sb, $"Os prazos para entrega dos resultados são estimativas.");
            sb.AppendLine($"");
            AppendTextoQuebrado(sb, $"Algumas análises podem exigir mais tempo por critérios técnicos.");
            sb.AppendLine($"");
            AppendTextoQuebrado(sb, $"O laboratório informará o paciente em caso de alterações relevantes.");
            sb.AppendLine($"");
            AppendTextoQuebrado(sb, $"Agradecemos pela compreensão.");
            AppendTextoQuebrado(sb, $"-");
            sb.AppendLine($"");
            AppendTextoQuebrado(sb, $"OBRIGADO PELA PREFERÊNCIA");
            sb.AppendLine($"");
            AppendTextoQuebrado(sb, $"-");
            sb.AppendLine($"");
            sb.AppendLine($"");
            sb.AppendLine($"");
            sb.AppendLine($"");

            try
            {
                var servico = ActivatorUtilities.CreateInstance<ServicoImpressaoCupom>(_serviceProvider, sb.ToString(), _db);
                resultado = servico.Executar(codigoExame);
            }
            catch (Exception ex)
            {
                // Log ou tratamento de erro
                _eventLogHelper.LogEventViewer("Erro ao tentar imprimir cupom: " + ex.Message, "wError");
                Console.WriteLine($"Erro ao imprimir: {ex.Message}");
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Erro ao imprimir: " + ex.Message, action = "", sucesso = false });
            }

            //return NoContent(); // HTTP 204   não retornam dados, mas retorna Ok, apenas para manter o padrão MVC.
            return Json(new
            {
                titulo = resultado.Sucesso ? "Sucesso" : "Erro",
                mensagem = resultado.Mensagem,
                sucesso = resultado.Sucesso
            });
        }
        //..

        //Lançamentos no partial Grid dos Lançamentos dos Exames do Dia
        //Feito pelo Kiro em 01/05/2026
        // Otimização de performance: query única com projeção direta + AsNoTracking.
        // Antes: 2 roundtrips ao banco, 4 Includes (JOINs completos), GroupBy em memória.
        // Agora: 1 roundtrip, subquery para MAX(Id) por paciente, projeção sem Include.
        [HttpGet]
        [Route("Requisitar/GetLancamentosHoje")]
        public IActionResult GetLancamentosHoje()
        {
            try
            {
                // Range do dia em UTC — necessário para comparar com colunas timestamptz no Npgsql 8.x
                // (DateTimeKind.Unspecified causa InvalidOperationException em timestamptz)
                var (hojeInicio, hojeFim) = _geralController.ObterRangeDiaUtc();

                //Feito pelo Kiro em 03/05/2026
                // Agrupamento por ExameRealizadoId (quando disponível) ou PacienteId (legado).
                // Cada ExameRealizadoId representa uma requisição única.
                // Registros antigos sem ExameRealizadoId são agrupados por PacienteId (fallback).
                var idsMaisRecentes = _db.Requisitar
                    .AsNoTracking()
                    .Where(r => r.DataIni >= hojeInicio && r.DataIni <= hojeFim)
                    .GroupBy(r => r.ExameRealizadoId != null ? r.ExameRealizadoId : -r.PacienteId)
                    .Select(g => g.Max(r => r.Id));
                //..Kiro

                // Query principal: projeta diretamente para o ViewModel sem Include
                var lista = _db.Requisitar
                    .AsNoTracking()
                    .Where(r => idsMaisRecentes.Contains(r.Id))
                    .Select(r => new vmRequisitarSimplificado
                    {
                        Id                 = r.Id,
                        PacienteId         = r.PacienteId,
                        NomePaciente       = r.Pacientes != null ? r.Pacientes.NomePaciente ?? "N/A" : "N/A",
                        Nascimento         = r.Pacientes != null ? r.Pacientes.Nascimento.ToString("dd/MM/yyyy") : "N/A",
                        NomeInstituicao    = (r.Instituicao != null ? r.Instituicao.Sigla ?? "" : "") + " - " + (r.Instituicao != null ? r.Instituicao.Nome ?? "" : ""),
                        NomePosto          = r.Posto != null ? r.Posto.NomePosto ?? "-" : "-",
                        NomeTabela         = (r.TabelaExames != null ? r.TabelaExames.SiglaTabela ?? "" : "") + " - " + (r.TabelaExames != null ? r.TabelaExames.NomeTabela ?? "" : ""),
                        LaboratorioApoio   = r.LaboratorioApoio ?? "-",
                        DataIni            = r.DataIni.ToString("dd/MM/yyyy"),
                        DataEntregaParcial = r.DataEntregaParcial != null ? r.DataEntregaParcial.Value.ToString("dd/MM/yyyy") : "",
                        TabelaExamesId     = r.TabelaExamesId,
                        ExameRealizadoId   = r.ExameRealizadoId
                    })
                    .ToList();

                return Json(new { data = lista });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("GetLancamentosHoje ERRO: " + ex.Message + " | " + ex.StackTrace, "wError");
                return Json(new { data = new List<vmRequisitarSimplificado>(), erro = ex.Message, stack = ex.StackTrace });
            }
        }
        //..Kiro

        // Endpoint temporário de diagnóstico — remover após confirmação
        [HttpGet]
        [Route("Requisitar/DiagnosticoHoje")]
        public IActionResult DiagnosticoHoje()
        {
            var dataServidorStr = _geralController.ObterDataHoraServidor();
            var dtNow = DateTime.Now;
            var dtToday = DateTime.Today;
            var dtUtcNow = DateTime.UtcNow;

            DateTime dataServidor;
            DateTime.TryParseExact(dataServidorStr, "dd/MM/yyyy HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out dataServidor);

            // Range do dia em UTC — necessário para comparar com colunas timestamptz no Npgsql 8.x
            var (hojeInicioUtc, hojeProximoDiaUtc) = _geralController.ObterRangeDiaUtc();

            // Total de registros sem filtro de data
            int totalSemFiltro = _db.Requisitar.Count();

            // As 5 últimas datas gravadas na tabela (brutas, com Kind)
            var ultimasDatas = _db.Requisitar
                .OrderByDescending(r => r.Id)
                .Take(5)
                .Select(r => new { r.Id, r.DataIni })
                .AsEnumerable()
                .Select(r => new {
                    r.Id,
                    DataIniStr = r.DataIni.ToString("yyyy-MM-dd HH:mm:ss"),
                    Kind = r.DataIni.Kind.ToString(),
                    Date = r.DataIni.Date.ToString("yyyy-MM-dd")
                })
                .ToList();

            // Filtra usando range UTC
            var hojeLocal = DateTime.Today;
            int totalHojeLINQ = _db.Requisitar
                .Where(r => r.DataIni >= hojeInicioUtc && r.DataIni < hojeProximoDiaUtc)
                .Count();
            int totalHojeMemoria = _db.Requisitar
                .AsEnumerable()
                .Count(r => r.DataIni.Date == hojeLocal);

            return Json(new
            {
                dataServidorStr,
                dataServidor = dataServidor.ToString("dd/MM/yyyy HH:mm:ss"),
                dtNow = dtNow.ToString("dd/MM/yyyy HH:mm:ss"),
                dtToday = dtToday.ToString("dd/MM/yyyy"),
                dtUtcNow = dtUtcNow.ToString("dd/MM/yyyy HH:mm:ss"),
                hojeInicio = hojeInicioUtc.ToString("yyyy-MM-dd"),
                hojeProximoDia = hojeProximoDiaUtc.ToString("yyyy-MM-dd"),
                totalSemFiltro,
                totalHojeLINQ,
                totalHojeMemoria,
                ultimasDatas
            });
        }



    }//Fim
}