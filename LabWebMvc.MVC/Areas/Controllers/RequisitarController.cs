using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Impressoras;
using LabWebMvc.MVC.Areas.Servicos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Interfaces.Collections;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IGeralService _geralService;
        public record ApiResult(bool sucesso, string mensagem, string? action, object? dados);

        public RequisitarController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IServiceProvider serviceProvider,
            IGeralService geralService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _serviceProvider = serviceProvider;
            _geralService = geralService;
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

            //Feito pelo Qoder em 12/08/2026
            // Index simplificado: o grid de lançamentos é carregado via AJAX
            // pelo endpoint GetRequisicoesNaoEnviadas, que consulta ExamesRealizados.
            // Não é mais necessário carregar dados de Requisitar aqui.
            if (string.IsNullOrEmpty(Conteudo)) registros = 100;

            ViewBag.TextoMenu = new object[] { "Requisição de Exames", false };
            var vmIndex = new vmRequisitar();
            return View(vmIndex);
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
                    paciente.DataEntrada = _geralService.ObterDataHoraLocal().Date; //Feito pelo Qoder em 22/08/2026 — agora DATE: data local do dia
                    paciente.DataRegistro = _geralService.ObterDataHoraUtc();
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
                paciente.DataEntrada = _geralService.ObterDataHoraLocal().Date; //Feito pelo Qoder em 22/08/2026 — agora DATE: data local do dia
                paciente.DataRegistro = _geralService.ObterDataHoraUtc();
                paciente.StatusBaixa = 0;
            }

            // Atualiza os dados (comum para novo ou existente)
            paciente.IdPacienteExterno = vm.VmPacientes.IdPacienteExterno.Safe();
            paciente.NomePaciente = vm.VmPacientes.NomePaciente.ToUpper();
            // Feito pelo Qoder em 22/08/2026 — Nascimento, DUM e DataEntradaBrasil agora são DATE:
            // grava apenas a data digitada (colunas date aceitam Kind=Unspecified no Npgsql 8.x)
            paciente.Nascimento = vm.VmPacientes.Nascimento.Date;
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
                ? vm.VmPacientes.DUM.Value.Date
                : null;
            paciente.TempoGestacao = vm.VmPacientes.TempoGestacao;
            paciente.Profissao = vm.VmPacientes.Profissao.SafeUpper();
            paciente.Naturalidade = vm.VmPacientes.Naturalidade.SafeUpper();
            paciente.Nacionalidade = vm.VmPacientes.Nacionalidade.SafeUpper();
            paciente.DataEntradaBrasil = vm.VmPacientes.DataEntradaBrasil.HasValue
                ? vm.VmPacientes.DataEntradaBrasil.Value.Date
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

        //Feito pelo Qoder em 12/08/2026
        // DTO interno para transportar dados do cupom para os métodos de persistência.
        // Substitui a antiga entidade Requisitar como intermediário.
        private sealed class DadosItemCupom
        {
            public int ClasseExamesId { get; init; }
            public string ClasseExamesNome { get; init; } = null!;
            public string RefExame { get; init; } = null!;
            public string? RefItem { get; init; }
            public string ContaExame { get; init; } = null!;
            public string? Descricao { get; init; }
            public decimal? ValorItem { get; init; }
            public int Etiqueta { get; init; }
            public int Etiquetas { get; init; }
            public string? LaboratorioApoio { get; init; }
            public string? ControleApoio { get; init; }
            public string? LaboratorioExterno { get; init; }
            public string? MaterialSaida { get; init; }
            public string? MaterialRetorno { get; init; }
            public DateTime DataIni { get; init; }
            public DateTime DataEntregaParcial { get; init; }
        }

        /// <summary>
        /// Constrói a lista de itens do cupom a partir do vm.ListaCupom (PlanoExames),
        /// enriquecendo com dados do cabeçalho (datas, laboratório, etc.).
        /// </summary>
        private List<DadosItemCupom> ConstruirItensDoCupom(vmRequisitar vm)
        {
            // ObterDataHoraUtc() retorna UTC do servidor PostgreSQL — fonte canônica
            // Fallback: DateTime.UtcNow do servidor de aplicação
            // Feito pelo Qoder em 22/08/2026 — DataIni/DataEntregaParcial agora são DATE:
            // grava apenas a data local (sem conversão UTC)
            DateTime dataIni = _geralService.ObterDataHoraLocal().Date;
            DateTime dataEntregaParcial = vm.DataEntregaParcial.HasValue
                ? vm.DataEntregaParcial.Value.Date
                : dataIni.AddDays(7);

            var lista = new List<DadosItemCupom>();

            if (vm.ListaCupom != null)
            {
                foreach (var item in vm.ListaCupom)
                {
                    lista.Add(new DadosItemCupom
                    {
                        ClasseExamesId       = item.ClasseExamesId,
                        ClasseExamesNome     = (item.RefExame ?? "").ToUpperInvariant(),
                        RefExame             = (item.RefExame ?? "").ToUpperInvariant(),
                        RefItem              = item.RefItem,
                        ContaExame           = item.ContaExame,
                        Descricao            = item.Descricao,
                        ValorItem            = item.ValorItem ?? 0.00m,
                        Etiqueta             = item.Etiqueta,
                        Etiquetas            = item.Etiquetas,
                        LaboratorioApoio     = vm.LaboratorioApoio,
                        ControleApoio        = vm.ControleApoio,
                        LaboratorioExterno   = item.LaboratorioExterno,
                        MaterialSaida        = null,
                        MaterialRetorno      = null,
                        DataIni              = dataIni,
                        DataEntregaParcial   = dataEntregaParcial
                    });
                }
            }

            return lista;
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
                //Feito pelo Qoder em 23/08/2026 — Fase 2.4 do plano: o FOR UPDATE é mantido (correto);
                //telemetria de espera adicionada para avaliar contenção por instituição (fila só se recorrente).
                var swLock = System.Diagnostics.Stopwatch.StartNew();
                var instituicao = await db.Instituicao
                    .FromSqlRaw(@"SELECT * FROM ""Instituicao"" WHERE ""Sigla"" = {0} FOR UPDATE", sigla)
                    .FirstOrDefaultAsync();
                swLock.Stop();
                if (swLock.ElapsedMilliseconds > 500)
                    _eventLogHelper.LogEventViewer($"[GeraSequencial] Espera de lock de {swLock.ElapsedMilliseconds}ms na instituição '{sigla}' — avaliar fila por instituição se recorrente.", "wWarning");

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

        //Feito pelo Qoder em 15/08/2026
        // Espelha GeraControleApoioExame do Delphi (FRequisicaoExames.pas):
        // protocolo diário no formato YYYYMMDD####, incrementando o sufixo a partir
        // do MAX(ControleApoio) dos exames lançados no dia local.
        private async Task<string> GerarControleApoioAsync()
        {
            // Feito pelo Qoder em 22/08/2026 — DataIni agora é DATE: comparação direta pela data local (sem conversão UTC)
            var hojeLocal = DateTime.Now.Date;

            var maxControle = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.DataIni == hojeLocal
                         && e.ControleApoio != null && e.ControleApoio != "")
                .MaxAsync(e => e.ControleApoio);

            string prefixo = DateTime.Now.ToString("yyyyMMdd");

            if (string.IsNullOrEmpty(maxControle) || maxControle.Length < 12 || !maxControle.StartsWith(prefixo))
                return prefixo + "0001";

            return int.TryParse(maxControle.Substring(8), out int sufixo) && sufixo < 9999
                ? prefixo + (sufixo + 1).ToString("0000")
                : prefixo + "9999";
        }
        //..Qoder

        // Feito pelo Qoder em 21/04/2026 — método agora participa da transação externa passada por SalvarRequisicao
        //Feito pelo Kiro em 03/05/2026
        // APENAS INCLUSÃO NOVA — cria header ExamesRealizados + ItensExamesRealizados.
        // A edição é controlada pelo orquestrador SalvarRequisicao.
        //Feito pelo Qoder em 12/08/2026 — alterado para receber List<DadosItemCupom> em vez de List<Requisitar>.
        // Retorna o ExameRealizadoId gerado.
        private async Task<int> SalvarExameRealizadoAsync(vmRequisitar vm, List<DadosItemCupom> listaItens)
        {
            if (vm == null || listaItens == null || !listaItens.Any())
                return 0;

            try
            {
                var primeiroItem = listaItens.First();
                int seq = await GeraSequencialAsync(vm.VmInstituicao?.Sigla!, _db);

                //Feito pelo Qoder em 15/08/2026 — Laboratório de Apoio (Fase 1 do plano):
                // LaboratorioApoio = sigla da instituição responsável (como no Delphi);
                // ControleApoio = protocolo sequencial diário YYYYMMDD####.
                string laboratorioApoioEfetivo = vm.VmInstituicao?.Sigla ?? vm.LaboratorioApoio ?? string.Empty;
                string controleApoioEfetivo = await GerarControleApoioAsync();
                //..Qoder

                var exame = new ExamesRealizados
                {
                    PacienteId = vm.VmPacientes?.Id ?? 0,
                    TabelaExamesId = vm.VmTabelaExames?.Id ?? 0,
                    InstituicaoId = vm.VmInstituicao?.Id ?? 0,
                    PostoId = vm.VmPostos?.Id > 0 ? vm.VmPostos.Id : null,
                    MedicoId = vm.VmMedicos?.Id ?? 0,
                    Sequencial = seq,
                    LaboratorioApoio = laboratorioApoioEfetivo,
                    ControleApoio = controleApoioEfetivo,
                    DataIni = primeiroItem.DataIni,
                    Liberacao = 0,
                    DataExame = _geralService.ObterDataHoraLocal().Date, //Feito pelo Qoder em 22/08/2026 — agora DATE: data local do dia
                    DataColeta = primeiroItem.DataIni.ToString("yyyy-MM-dd"),
                    Baixado = 0,
                    EnviarEmail = 0,
                    Situacao = 0,
                    TotalImpresso = 0
                };

                _db.ExamesRealizados.Add(exame);
                await _db.SaveChangesAsync();

                // Insere itens vinculados ao novo header
                // Regra de expansão: se o item do cupom é um Principal (ContaExame termina em "0000"),
                // buscar seus sub-itens no PlanoExames e inseri-los como ItensExamesRealizados.
                // Os sub-itens são os que receberão resultados de exame.
                int ordemItem = 0;
                var itensExames = new List<ItensExamesRealizados>();

                foreach (var item in listaItens)
                {
                    string contaExame = item.ContaExame ?? "";
                    bool ehPrincipal = contaExame.Length >= 4 && contaExame.Substring(contaExame.Length - 4) == "0000";

                    if (ehPrincipal)
                    {
                        //Feito pelo Kiro em 07/06/2026
                        // Expansão automática: buscar sub-itens do PlanoExames para este Principal
                        string prefixoPrincipal = contaExame.Substring(0, contaExame.Length - 4); // ex: "1102001" de "11020010000"
                        var subItens = await _db.PlanoExames
                            .AsNoTracking()
                            .Where(p => p.TabelaExamesId == vm.VmTabelaExames!.Id
                                     && p.ContaExame.StartsWith(prefixoPrincipal)
                                     && p.ContaExame != contaExame
                                     && !p.ContaExame.EndsWith("0000000")) // excluir folha geral
                            .OrderBy(p => p.ContaExame)
                            .ToListAsync();

                        if (subItens.Any())
                        {
                            // Inserir o Principal como agrupador (sem resultado, apenas referência de preço)
                            itensExames.Add(new ItensExamesRealizados
                            {
                                PacienteId = vm.VmPacientes!.Id,
                                ClasseExamesId = item.ClasseExamesId,
                                ClasseExamesNome = item.ClasseExamesNome,
                                ExameRealizadoId = exame.Id,
                                TabelaExamesId = vm.VmTabelaExames!.Id,
                                OrdemItem = ++ordemItem,
                                RefExame = item.RefExame!,
                                RefItem = item.RefItem ?? string.Empty,
                                ContaExame = item.ContaExame ?? "",
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

                            // Inserir cada sub-item expandido
                            foreach (var sub in subItens)
                            {
                                itensExames.Add(new ItensExamesRealizados
                                {
                                    PacienteId = vm.VmPacientes!.Id,
                                    ClasseExamesId = item.ClasseExamesId,
                                    ClasseExamesNome = item.ClasseExamesNome,
                                    ExameRealizadoId = exame.Id,
                                    TabelaExamesId = vm.VmTabelaExames!.Id,
                                    OrdemItem = ++ordemItem,
                                    RefExame = sub.RefExame ?? item.RefExame!,
                                    RefItem = (sub.RefItem ?? item.RefItem) ?? string.Empty,
                                    ContaExame = sub.ContaExame,
                                    Descricao = sub.Descricao,
                                    ValorItem = sub.ValorItem,
                                    Etiquetas = sub.Etiquetas,
                                    InstituicaoId = vm.VmInstituicao!.Id,
                                    Sequencial = exame.Sequencial,
                                    LaboratorioApoio = item.LaboratorioApoio,
                                    ControleApoio = item.ControleApoio,
                                    LaboratorioExterno = sub.LaboratorioExterno,
                                    MaterialSaida = item.MaterialSaida,
                                    MaterialRetorno = item.MaterialRetorno,
                                    DataEntregaParcial = item.DataEntregaParcial,
                                    Liberado = 0,
                                    Baixado = 0
                                });
                            }
                        }
                        else
                        {
                            // Principal sem sub-itens — salvar normalmente
                            itensExames.Add(new ItensExamesRealizados
                            {
                                PacienteId = vm.VmPacientes!.Id,
                                ClasseExamesId = item.ClasseExamesId,
                                ClasseExamesNome = item.ClasseExamesNome,
                                ExameRealizadoId = exame.Id,
                                TabelaExamesId = vm.VmTabelaExames!.Id,
                                OrdemItem = ++ordemItem,
                                RefExame = item.RefExame!,
                                RefItem = item.RefItem ?? string.Empty,
                                ContaExame = item.ContaExame ?? "",
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
                        //..Kiro
                    }
                    else
                    {
                        // Item normal (não é Principal) — salvar diretamente
                        itensExames.Add(new ItensExamesRealizados
                        {
                            PacienteId = vm.VmPacientes!.Id,
                            ClasseExamesId = item.ClasseExamesId,
                            ClasseExamesNome = item.ClasseExamesNome,
                            ExameRealizadoId = exame.Id,
                            TabelaExamesId = vm.VmTabelaExames!.Id,
                            OrdemItem = ++ordemItem,
                            RefExame = item.RefExame!,
                            RefItem = item.RefItem ?? string.Empty,
                            ContaExame = item.ContaExame ?? "",
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
                }

                //Feito pelo Qoder em 15/08/2026 — todos os itens herdam o LaboratorioApoio
                // e o ControleApoio do header (mesmo valor, como no Delphi).
                foreach (var it in itensExames)
                {
                    it.LaboratorioApoio = laboratorioApoioEfetivo;
                    it.ControleApoio = controleApoioEfetivo;
                }
                //..Qoder

                _db.ItensExamesRealizados.AddRange(itensExames);
                await _db.SaveChangesAsync();

                return exame.Id;
            }
            catch (Exception ex)
            {
                var detalhe = ex.InnerException != null
                    ? $"{ex.Message} | Inner: {ex.InnerException.Message}"
                    : ex.Message;
                _eventLogHelper.LogEventViewer($"Erro ao salvar exame realizado: {detalhe}\n{ex.StackTrace}", "Error");
                return 0;
            }
        }
        //..Kiro


        [HttpPost]
        [Route("SalvarRequisicao")]
        [Produces("application/json")]
        [TypeFilter(typeof(SessionFilter))]
        public async Task<IActionResult> SalvarRequisicao([FromForm] vmRequisitar vm, int registroID = 0)
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
                //Feito pelo Qoder em 12/08/2026 — substituído CriarRequisicoes por ConstruirItensDoCupom
                List<DadosItemCupom> listaItens = ConstruirItensDoCupom(vm);

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

                    // 1.1. Bloqueia edição se o exame já foi enviado para análise
                    if (headerExistente.Situacao >= 1)
                    {
                        await transaction.RollbackAsync();
                        return Ok(new ApiResult(false, "Este exame já foi enviado para análise e só pode ser alterado pela tela de Consultar Exames.", redirecionaUrl, null));
                    }

                    // 2. Atualizar header (sem recriar)
                    headerExistente.TabelaExamesId = vm.VmTabelaExames?.Id ?? headerExistente.TabelaExamesId;
                    headerExistente.InstituicaoId = vm.VmInstituicao?.Id ?? headerExistente.InstituicaoId;
                    headerExistente.PostoId = vm.VmPostos?.Id > 0 ? vm.VmPostos.Id : null;
                    headerExistente.MedicoId = vm.VmMedicos?.Id ?? headerExistente.MedicoId;
                    //Feito pelo Qoder em 15/08/2026 — Laboratório de Apoio (Fase 1 do plano):
                    // LaboratorioApoio segue a sigla da instituição efetiva após a edição
                    // (preserva a existente se o vm não trouxer instituição);
                    // ControleApoio é preservado — o protocolo nasce no lançamento.
                    headerExistente.LaboratorioApoio = vm.VmInstituicao?.Sigla ?? headerExistente.LaboratorioApoio;
                    //..Qoder

                    // 3. Excluir ItensExamesRealizados por ExameRealizadoId
                    var itensAntigos = await _db.ItensExamesRealizados
                        .Where(i => i.ExameRealizadoId == exameRealizadoId)
                        .ToListAsync();
                    if (itensAntigos.Any())
                        _db.ItensExamesRealizados.RemoveRange(itensAntigos);

                    await _db.SaveChangesAsync();

                    // 4. Inserir novos ItensExamesRealizados
                    //Feito pelo Qoder em 12/08/2026 — usa DadosItemCupom em vez de Requisitar
                    int ordemItem = 0;
                    var novosItens = new List<ItensExamesRealizados>();
                    foreach (var item in listaItens)
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
                            RefItem = item.RefItem ?? string.Empty,
                            ContaExame = item.ContaExame,
                            Descricao = item.Descricao,
                            ValorItem = item.ValorItem,
                            Etiquetas = item.Etiquetas,
                            InstituicaoId = vm.VmInstituicao!.Id,
                            Sequencial = headerExistente.Sequencial,
                            //Feito pelo Qoder em 15/08/2026 — itens recriados na edição
                            // herdam LaboratorioApoio/ControleApoio do header (nunca null).
                            LaboratorioApoio = headerExistente.LaboratorioApoio,
                            ControleApoio = headerExistente.ControleApoio,
                            //..Qoder
                            LaboratorioExterno = item.LaboratorioExterno,
                            MaterialSaida = item.MaterialSaida,
                            MaterialRetorno = item.MaterialRetorno,
                            DataEntregaParcial = item.DataEntregaParcial,
                            Liberado = 0,
                            Baixado = 0
                        });
                    }
                    _db.ItensExamesRealizados.AddRange(novosItens);

                    await _db.SaveChangesAsync();
                }
                else
                {
                    // === INCLUSÃO NOVA ===
                    // SalvarExameRealizadoAsync cria header + itens e retorna o Id
                    //Feito pelo Qoder em 12/08/2026 — passa listaItens (DadosItemCupom) em vez de listaRequisitar
                    exameRealizadoId = await SalvarExameRealizadoAsync(vm, listaItens);
                    if (exameRealizadoId <= 0)
                    {
                        await transaction.RollbackAsync();
                        return Ok(new ApiResult(false, "Falha ao salvar dados na tabela de Exames.", redirecionaUrl, null));
                    }
                }

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
        public ActionResult ModalPacientes(vmRequisitar vm)
        {
            //Feito pelo Qoder em 23/08/2026 — Fase 2.3 do plano: o Take(1000) foi removido.
            //O grid agora carrega via AJAX paginado pelo endpoint ModalPacientesListar
            //(serverSide), eliminando o teto em que pacientes além do 1000º ficavam invisíveis.
            //..Qoder

            ViewBag.TextoMenu = new object[] { "Consulta Tabelas de Pacientes", false };
            return PartialView(vm);
        }

        /* Fase 2.3 do plano — endpoint server-side do modal de pacientes do Requisitar.
           Recebe o POST do DataTables (draw/start/length/search/order) e devolve apenas
           a página solicitada, com busca e ordenação executadas no banco (sem Take(1000)). */
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ModalPacientesListar")]
        public async Task<JsonResult> ModalPacientesListar([FromForm] DataTableRequest request)
        {
            try
            {
                int draw = request.Draw;
                int start = Math.Max(request.Start, 0);
                // length -1 ("Todos") não faz sentido no modal de seleção — limita a 100
                int length = request.Length > 0 ? Math.Min(request.Length, 100) : 10;
                string searchValue = request.Search?.Value?.Trim() ?? string.Empty;

                // Whitelist de colunas ordenáveis (evita injeção de nome de coluna)
                string sortColumn = request.Order.Count > 0 && request.Order[0].Column < request.Columns.Count
                    ? (request.Columns[request.Order[0].Column].Data ?? "nomePaciente")
                    : "nomePaciente";
                if (sortColumn is not ("nomePaciente" or "cpf" or "id")) sortColumn = "nomePaciente";
                bool ascending = string.Equals(request.Order.Count > 0 ? request.Order[0].Dir : "asc", "asc", StringComparison.OrdinalIgnoreCase);

                IQueryable<Pacientes> query = _db.Pacientes.AsNoTracking();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    string busca = searchValue.ToUpper();
                    query = query.Where(p => p.NomePaciente.Contains(busca) || (p.CPF != null && p.CPF.Contains(busca)));
                }

                query = (sortColumn, ascending) switch
                {
                    ("id", true) => query.OrderBy(p => p.Id),
                    ("id", false) => query.OrderByDescending(p => p.Id),
                    ("cpf", true) => query.OrderBy(p => p.CPF),
                    ("cpf", false) => query.OrderByDescending(p => p.CPF),
                    (_, true) => query.OrderBy(p => p.NomePaciente),
                    _ => query.OrderByDescending(p => p.NomePaciente)
                };

                int recordsTotal = await query.CountAsync();

                var pageData = await query.Skip(start).Take(length)
                    .Select(p => new { p.Id, p.NomePaciente, p.CPF })
                    .ToListAsync();

                List<object> result = pageData.Select(p => (object)new
                {
                    id = p.Id,
                    nomePaciente = p.NomePaciente ?? string.Empty,
                    cpf = p.CPF ?? string.Empty
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
                _eventLogHelper.LogEventViewer("[Requisitar] ModalPacientesListar - Erro: " + ex.Message, "wError");
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
        [Route("ModalInstituicoes")]
        public async Task<ActionResult> ModalInstituicoes(vmRequisitar vm)
        {
            ICollection<Instituicao> dados = [];

            //Auditado pelo Qoder em 23/08/2026 — Fase 2.3 do plano: Take(1000) mantido como
            //teto de segurança — domínio pequeno, sem problema real de escala.
            dados = await _db.Instituicao.AsNoTracking().Take(1000).ToListAsync();

            vm.ListaInstituicoes = dados;

            ViewBag.TextoMenu = new object[] { "Consulta Instituições", false };
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModalPostos")]
        public async Task<ActionResult> ModalPostos(vmRequisitar vm)
        {
            ICollection<Postos> dados = [];

            //Feito pelo Qoder em 31/05/2026 - lista Postos somente da Instituicao escolhida.
            //Sem Instituicao selecionada, retorna lista vazia (defesa em profundidade — o JS bloqueia a abertura).
            if (vm.InstituicaoId > 0)
            {
                //Auditado pelo Qoder em 23/08/2026 — Fase 2.3 do plano: Take(1000) mantido como
                //teto de segurança — postos por instituição é domínio pequeno.
                dados = await _db.Postos.AsNoTracking()
                    .Where(p => p.InstituicaoId == vm.InstituicaoId)
                    .OrderBy(p => p.SiglaPosto)
                    .Take(1000)
                    .ToListAsync();
            }
            //..Qoder

            vm.ListaPostos = dados;

            ViewBag.TextoMenu = new object[] { "Consulta Postos de Coleta", false };
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModalTabelas")]
        public async Task<ActionResult> ModalTabelas(vmRequisitar vm)
        {
            ICollection<TabelaExames> dados = [];

            //Auditado pelo Qoder em 23/08/2026 — Fase 2.3 do plano: Take(1000) mantido como
            //teto de segurança — tabelas de exames é domínio pequeno.
            dados = await _db.TabelaExames.AsNoTracking().Take(1000).ToListAsync();

            vm.ListaTabelas = dados;

            ViewBag.TextoMenu = new object[] { "Consulta Tabelas de Exames", false };
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ModalMedicos")]
        public async Task<ActionResult> ModalMedicos(vmRequisitar vm)
        {
            List<Medicos> dados = [];

            //Auditado pelo Qoder em 23/08/2026 — Fase 2.3 do plano: Take(1000) mantido como
            //teto de segurança — médicos é domínio pequeno.
            dados = await _db.Medicos.AsNoTracking().Take(1000).ToListAsync();

            vm.ListaMedicos = dados;

            ViewBag.TextoMenu = new object[] { "Consulta Tabelas de Médicos", false };
            return PartialView(vm);
        }

        /* Manipulando as variáveis do Modal para Instituições, Não mostra References mas está sendo utilizado */

        [TypeFilter(typeof(SessionFilter))]
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
                vm.Nascimento = dados.Nascimento.Date > new DateTime(1900, 1, 1) ? dados.Nascimento.ToString("yyyy-MM-dd") : ""; //Feito pelo Qoder em 22/08/2026 — sentinela ≤ 01/01/1900 em branco
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
                vm.Telefone = dados.Telefone.FormataTelefone();
                vm.DUM = dados.DUM != null && dados.DUM.Value.Date > new DateTime(1900, 1, 1) ? dados.DUM.Value.ToString("yyyy-MM-dd") : ""; //Feito pelo Qoder em 22/08/2026 — sentinela ≤ 01/01/1900 em branco
                vm.TempoGestacao = dados.TempoGestacao;
                vm.Observacao = dados.Observacao;

            }
            else return Json(new { success = false, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!

            return Json(new { success = true, vm = vm });   //retornando os dados da vm pela chamada Ajax JSon!
        }
        //..

        /* Manipulando as variáveis do Modal para Instituições, Não mostra References mas está sendo utilizado */
        [TypeFilter(typeof(SessionFilter))]
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
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("RetornoDoModalPostos")]
        public async Task<JsonResult> RetornoDoModalPostos(vmRequisitar vm, string id)
        {
            //Feito pelo Qoder em 31/05/2026 - exige Instituicao escolhida; sem ela, nem consulta o banco.
            if (vm.InstituicaoId <= 0)
                return Json(new { success = false, message = "Selecione uma Instituição antes de escolher o Posto." });
            //..Qoder

            string busca = id.Trim().ToUpper();

            //Feito pelo Qoder em 21/04/2026 - busca limitada à Instituicao escolhida
            var dados = await _db.Postos.AsNoTracking()
                .Where(c => c.InstituicaoId == vm.InstituicaoId)
                .Where(c => c.NomePosto.Contains(busca) || c.SiglaPosto.Contains(busca))
                .FirstOrDefaultAsync();
            //..Qoder

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
        [TypeFilter(typeof(SessionFilter))]
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
        [TypeFilter(typeof(SessionFilter))]
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
        [TypeFilter(typeof(SessionFilter))]
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

        [TypeFilter(typeof(SessionFilter))]
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
        [TypeFilter(typeof(SessionFilter))]
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

            var vmCupom = new vmRequisitar();
            vmCupom.TotalCupom = totalCupom?.ToString("N2");
            vmCupom.ListaCupom = lista;

            return PartialView("Partials/_PartialMontarItensCupom", vmCupom);
        }
        //..Qoder

        //..

        [TypeFilter(typeof(SessionFilter))]
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
                    vm.TotalCupom = "0,00";
                    vm.ListaCupom = ListaAcumulativa.Instancia.ObterCupom(usuarioId);
                    vm.MensagemErro = "Item de exame não encontrado.";
                    return PartialView("Partials/_PartialMontarItensCupom", vm);
                }

                if (itemBuscado.ValorItem == null || itemBuscado.ValorItem <= 0)
                {
                    vm.TotalCupom = "0,00";
                    vm.ListaCupom = ListaAcumulativa.Instancia.ObterCupom(usuarioId);
                    vm.MensagemErro = "Item sem valor definido, não pode ser selecionado.";
                    return PartialView("Partials/_PartialMontarItensCupom", vm);
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
            //Parâmetros auxiliares no ViewModel
            vm.TotalCupom = totalCupom?.ToString("N2");

            return PartialView("Partials/_PartialMontarItensCupom", vm);
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
        [TypeFilter(typeof(SessionFilter))]
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

            // Feito pelo Qoder em 22/08/2026 — DataIni agora é DATE: comparação direta pela data local (sem conversão UTC)
            var dataInicio = dataConsulta.Date;
            var dataFim = dataConsulta.Date;

            //Feito pelo Kiro em 02/05/2026
            //Feito pelo Qoder em 12/08/2026 — substituído query em Requisitar por ExamesRealizados + ItensExamesRealizados
            // Filtra o header (ExamesRealizados) por PacienteId + Data + TabelaExamesId.
            var headerQuery = _db.ExamesRealizados
                .Include(e => e.Pacientes)
                .Include(e => e.Medicos)
                .Include(e => e.Instituicao)
                .Include(e => e.Postos)
                .Include(e => e.TabelaExames)
                .Where(e => e.PacienteId == pacienteId
                         && e.DataIni >= dataInicio
                         && e.DataIni <= dataFim);

            if (tabelaExamesId > 0)
                headerQuery = headerQuery.Where(e => e.TabelaExamesId == tabelaExamesId);

            var header = headerQuery.AsNoTracking().FirstOrDefault();

            if (header == null)
                return Json(new { sucesso = false, mensagem = "Nenhuma requisição encontrada para este paciente na data informada." });

            // Bloqueia edição se o exame já foi enviado para análise
            if (header.Situacao >= 1)
            {
                return Json(new { sucesso = false, mensagem = "Este exame já foi enviado para análise e só pode ser alterado pela tela de Consultar Exames." });
            }

            // Busca os itens vinculados ao header
            var itens = _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ExameRealizadoId == header.Id)
                .OrderBy(i => i.OrdemItem)
                .ToList();

            if (!itens.Any())
                return Json(new { sucesso = false, mensagem = "Nenhuma requisição encontrada para este paciente na data informada." });

            // Verifica se algum item possui resultado lançado (bloqueia edição)
            bool temResultadoItensExames = itens.Any(i => !string.IsNullOrWhiteSpace(i.Resultado));
            
            if (temResultadoItensExames)
            {
                return Json(new { sucesso = false, mensagem = "Esta requisição não pode ser editada pois existem resultados lançados nos itens de exames realizados." });
            }

            // Monta a lista de itens do cupom para recarregar no formulário
            var listaCupom = itens.Select(r => new
            {
                id             = r.Id,
                contaExame     = r.ContaExame,
                descricao      = r.Descricao,
                valorItem      = r.ValorItem,
                refExame       = r.RefExame,
                refItem        = r.RefItem,
                classeExamesId = r.ClasseExamesId,
                etiquetas      = r.Etiquetas
            }).ToList();

            var resultado = new
            {
                sucesso             = true,
                pacienteId          = header.PacienteId,
                nomePaciente        = header.Pacientes?.NomePaciente ?? "",
                nascimento          = header.Pacientes?.Nascimento.ToString("yyyy-MM-dd") ?? "",
                cpfPaciente         = header.Pacientes?.CPF ?? "",
                telefone            = header.Pacientes?.Telefone.FormataTelefone() ?? "",
                email               = header.Pacientes?.Email ?? "",
                nomeMae             = header.Pacientes?.NomeMae ?? "",
                naturalidade        = header.Pacientes?.Naturalidade ?? "",
                nacionalidade       = header.Pacientes?.Nacionalidade ?? "",
                profissao           = header.Pacientes?.Profissao ?? "",
                cep                 = header.Pacientes?.CEP ?? "",
                logradouro          = header.Pacientes?.Logradouro ?? "",
                endereco            = header.Pacientes?.Endereco ?? "",
                numero              = header.Pacientes?.Numero ?? "",
                complemento         = header.Pacientes?.Complemento ?? "",
                bairro              = header.Pacientes?.Bairro ?? "",
                cidade              = header.Pacientes?.Cidade ?? "",
                uf                  = header.Pacientes?.UF ?? "",
                observacao          = header.Pacientes?.Observacao ?? "",
                sexo                = header.Pacientes?.Sexo ?? "",
                estadoCivil         = (int)(header.Pacientes?.EstadoCivil ?? 0),
                tempoGestacao       = (int)(header.Pacientes?.TempoGestacao ?? 0),
                dum                 = header.Pacientes?.DUM?.ToString("yyyy-MM-dd") ?? "",
                medicoId            = header.MedicoId,
                nomeMedico          = header.Medicos?.NomeMedico ?? "",
                crm                 = header.Medicos?.CRM ?? "",
                instituicaoId       = header.InstituicaoId,
                siglaInstituicao    = header.Instituicao?.Sigla ?? "",
                nomeInstituicao     = header.Instituicao?.Nome ?? "",
                postoId             = header.PostoId ?? 0,
                nomePosto           = header.Postos?.NomePosto ?? "",
                tabelaExamesId      = header.TabelaExamesId,
                siglaTabela         = header.TabelaExames?.SiglaTabela ?? "",
                nomeTabela          = header.TabelaExames?.NomeTabela ?? "",
                dataIni             = header.DataIni.ToString("dd/MM/yyyy"),
                exameRealizadoId    = header.Id,
                //Feito pelo Qoder em 15/08/2026 — Laboratório de Apoio (Fase 1 do plano):
                // retorna os campos para uso informativo/futuro no formulário.
                laboratorioApoio    = header.LaboratorioApoio ?? "",
                controleApoio       = header.ControleApoio ?? "",
                //..Qoder
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
        [TypeFilter(typeof(SessionFilter))]
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
                    // Feito pelo Qoder em 22/08/2026 — DataIni agora é DATE: comparação direta pela data local (sem conversão UTC)
                    var dataInicio = dataConsulta.Date;
                    var dataFim = dataConsulta.Date;

                    //Feito pelo Qoder em 12/08/2026 — substituído query em Requisitar por ExamesRealizados + ItensExamesRealizados
                    // Busca o header da requisição do paciente na data
                    var header = await _db.ExamesRealizados
                        .AsNoTracking()
                        .Where(e => e.PacienteId == pacienteId
                                 && e.DataIni >= dataInicio
                                 && e.DataIni <= dataFim)
                        .Select(e => new { e.Id, e.TabelaExamesId })
                        .FirstOrDefaultAsync();

                    if (header != null)
                    {
                        // Busca os itens vinculados ao header
                        var itensRequisicao = await _db.ItensExamesRealizados
                            .AsNoTracking()
                            .Where(i => i.ExameRealizadoId == header.Id)
                            .Select(i => new { i.ContaExame, TabelaExamesId = header.TabelaExamesId })
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
                    } // fim if (header != null)
                }
            }

            // Obtém a lista acumulada e calcula o total
            var lista = ListaAcumulativa.Instancia.ObterCupom(usuarioId);
            foreach (var item in lista)
            {
                totalCupom += item.ValorItem;
            }

            var vm = new vmRequisitar();
            vm.TotalCupom = totalCupom?.ToString("N2");
            vm.ListaCupom = lista;

            return PartialView("Partials/_PartialMontarItensCupom", vm);
        }
        //..Kiro

        //Feito pelo Kiro em 01/05/2026
        /// <summary>
        /// Exclui a requisição (header ExamesRealizados + itens ItensExamesRealizados).
        /// Bloqueia a exclusão se qualquer item já possuir resultado lançado.
        /// Mantém o cadastro do paciente e do médico intactos.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Requisitar/ExcluirRequisicao")]
        public async Task<IActionResult> ExcluirRequisicao([FromBody] CupomRequisicaoViewModel vm)
        {
            if (vm == null || vm.IdPaciente <= 0 || vm.Data == null)
                return Json(new { sucesso = false, mensagem = "Dados inválidos para exclusão." });

            // Feito pelo Qoder em 22/08/2026 — DataIni agora é DATE: comparação direta pela data local (sem conversão UTC)
            var dataInicio = vm.Data.Value.Date;
            var dataFim = vm.Data.Value.Date;

            //Feito pelo Qoder em 12/08/2026 — substituído query em Requisitar por ExamesRealizados
            // Filtro primário: ExameRealizadoId (header da sessão), quando informado.
            // Fallback: filtro por PacienteId + DataIni (compatibilidade).
            var headersQuery = _db.ExamesRealizados.AsQueryable();
            if (vm.ExameRealizadoId.HasValue && vm.ExameRealizadoId.Value > 0)
            {
                headersQuery = headersQuery.Where(e => e.Id == vm.ExameRealizadoId.Value);
            }
            else
            {
                headersQuery = headersQuery.Where(e => e.PacienteId == vm.IdPaciente
                                                    && e.DataIni >= dataInicio
                                                    && e.DataIni <= dataFim);
                if (vm.TabelaExamesId > 0)
                    headersQuery = headersQuery.Where(e => e.TabelaExamesId == vm.TabelaExamesId);
            }

            var headers = await headersQuery.ToListAsync();

            if (!headers.Any())
                return Json(new { sucesso = false, mensagem = "Nenhuma requisição encontrada para exclusão." });

            // Bloqueia exclusão se algum exame já foi enviado para análise
            if (headers.Any(h => h.Situacao >= 1))
            {
                return Json(new { sucesso = false, mensagem = "Este exame já foi enviado para análise e só pode ser excluído pela tela de Consultar Exames." });
            }

            // Verifica se algum item possui resultado lançado (bloqueia exclusão)
            var exameRealizadoIds = headers.Select(h => h.Id).ToList();
            bool temResultadoItensExames = await _db.ItensExamesRealizados
                .AnyAsync(i => exameRealizadoIds.Contains(i.ExameRealizadoId)
                            && !string.IsNullOrWhiteSpace(i.Resultado));
            
            if (temResultadoItensExames)
            {
                return Json(new { sucesso = false, mensagem = "Esta requisição não pode ser excluída pois existem resultados lançados nos itens de exames realizados." });
            }

            try
            {
                using var transaction = await _db.Database.BeginTransactionAsync();

                // Cascata: exclui ItensExamesRealizados vinculados aos headers
                int itensRemovidos = 0;
                var itensParaRemover = await _db.ItensExamesRealizados
                    .Where(i => exameRealizadoIds.Contains(i.ExameRealizadoId))
                    .ToListAsync();
                if (itensParaRemover.Any())
                {
                    itensRemovidos = itensParaRemover.Count;
                    _db.ItensExamesRealizados.RemoveRange(itensParaRemover);
                    await _db.SaveChangesAsync();
                }

                // Remove os headers (ExamesRealizados)
                _db.ExamesRealizados.RemoveRange(headers);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                var msg = $"{itensRemovidos} item(ns) excluído(s), {headers.Count} sessão(ões) de ExamesRealizados.";
                return Json(new { sucesso = true, mensagem = msg });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer($"Erro ao excluir requisição: {ex.Message}", "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao excluir a requisição: " + ex.Message });
            }
        }
        //..Kiro

        //Imprimir Cupom
        [TypeFilter(typeof(SessionFilter))]
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
            var dataConsulta = vm.Data ?? _geralService.ObterDataHoraLocal().Date;

            var paciente = _db.Pacientes.Where(s => s.Id == vm.IdPaciente).FirstOrDefault();
            if (paciente == null)
            {
                _eventLogHelper.LogEventViewer("Paciente não encontrado ::: Dados inválidos na impressão de cupom", "wError");
                return NotFound("Paciente não encontrado.");
            }

            ResultadoImpressao resultado;

            //Filtra por TabelaExamesId quando disponível para evitar incluir
            // itens de outras requisições do mesmo paciente no mesmo dia.
            // Feito pelo Qoder em 22/08/2026 — DataIni agora é DATE: comparação direta pela data local (sem conversão UTC)
            //Feito pelo Qoder em 12/08/2026 — substituído query em Requisitar por ExamesRealizados + ItensExamesRealizados
            var dataInicioUtc = dataConsulta.Date;
            var dataFimUtc = dataConsulta.Date;
            var headerQuery = _db.ExamesRealizados
                         .Where(e => e.PacienteId == vm.IdPaciente
                                  && e.DataIni >= dataInicioUtc
                                  && e.DataIni <= dataFimUtc);

            if (vm.TabelaExamesId > 0)
                headerQuery = headerQuery.Where(e => e.TabelaExamesId == vm.TabelaExamesId);

            var header = headerQuery.FirstOrDefault();

            if (header == null)
                return Content("Nenhuma requisição de exame encontrada para esta data.", "text/plain");

            // Busca os itens vinculados ao header
            var itens = _db.ItensExamesRealizados
                .Where(i => i.ExameRealizadoId == header.Id)
                .OrderBy(i => i.OrdemItem)
                .ToList();

            if (!itens.Any())
                return Content("Nenhuma requisição de exame encontrada para esta data.", "text/plain");

            int codigoPaciente = paciente.Id;
            string nomePaciente = paciente.NomePaciente.ToUpper();

            int instituicaoId = header.InstituicaoId;
            int tabelaExamesId = header.TabelaExamesId;
            // Código do exame = ExameRealizadoId (header.Id)
            string codigoExame = header.Id.ToString();

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
            var dataServidorCupom = _geralService.ObterDataHoraLocal();
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

            foreach (var item in itens)
            {
                contador++;
                AppendTextoQuebrado(sb, $"{contador}) {item.Descricao}");
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

        //Lançamentos no partial Grid de Requisições (não enviados para análise)
        //Feito pelo Kiro em 01/05/2026
        // Otimização de performance: query única com projeção direta + AsNoTracking.
        // Feito pelo Qoder em 15/08/2026 — renomeado de GetLancamentosHoje para
        // GetRequisicoesNaoEnviadas e removido filtro por data. Agora lista todos
        // os exames com Situacao == 0 (Pendente), ou seja, ainda não enviados
        // para análise.
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Requisitar/GetRequisicoesNaoEnviadas")]
        public IActionResult GetRequisicoesNaoEnviadas()
        {
            try
            {
                var lista = _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => e.Situacao == 0)
                    .Select(e => new vmRequisitarSimplificado
                    {
                        Id                 = e.Id,
                        ExameRealizadoId   = e.Id,
                        PacienteId         = e.PacienteId,
                        NomePaciente       = e.Pacientes != null ? e.Pacientes.NomePaciente ?? "N/A" : "N/A",
                        Nascimento         = e.Pacientes != null ? (e.Pacientes.Nascimento > new DateTime(1900, 1, 1) ? e.Pacientes.Nascimento.ToString("dd/MM/yyyy") : "") : "N/A", //Feito pelo Qoder em 22/08/2026 — sentinela ≤ 01/01/1900 em branco
                        NomeInstituicao    = (e.Instituicao != null ? e.Instituicao.Sigla ?? "" : "") + " - " + (e.Instituicao != null ? e.Instituicao.Nome ?? "" : ""),
                        NomePosto          = e.Postos != null ? e.Postos.SiglaPosto ?? "-" : "-",
                        NomeTabela         = (e.TabelaExames != null ? e.TabelaExames.SiglaTabela ?? "" : "") + " - " + (e.TabelaExames != null ? e.TabelaExames.NomeTabela ?? "" : ""),
                        LaboratorioApoio   = e.LaboratorioApoio ?? "-",
                        DataIni            = e.DataIni.ToString("dd/MM/yyyy"),
                        DataEntregaParcial = e.DataEntrega != null ? e.DataEntrega.Value.ToString("dd/MM/yyyy") : "",
                        TabelaExamesId     = e.TabelaExamesId,
                        //Feito pelo Qoder em 15/08/2026 — indicador visual de recebimento no grid.
                        EmCatalogoRecebimentos = e.EmCatalogoRecebimentos
                        //..Qoder
                        ,
                        //Feito pelo Qoder em 16/08/2026 — indicador roxo: catálogo pendente de cobrança à instituição.
                        CobrancaInstituicaoPendente = _db.CatalogoRecebimentosExames.Any(v => v.ExameRealizadoId == e.Id && v.CatalogoRecebimento.Status == 0 && v.CatalogoRecebimento.CobrancaInstituicao)
                        //..Qoder
                    })
                    .OrderByDescending(v => v.ExameRealizadoId)
                    .ToList();
                // ..Qoder

                return Json(new { data = lista });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("GetRequisicoesNaoEnviadas ERRO: " + ex.Message + " | " + ex.StackTrace, "wError");
                return Json(new { data = new List<vmRequisitarSimplificado>(), erro = ex.Message, stack = ex.StackTrace });
            }
        }
        //..Kiro

        // Feito pelo Qoder em 15/08/2026 — envia o exame para análise,
        // alterando a situação de 0 (Pendente) para 1 (Em Análise).
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Requisitar/EnviarParaAnalise")]
        public async Task<IActionResult> EnviarParaAnalise([FromForm] int exameRealizadoId)
        {
            try
            {
                var exame = await _db.ExamesRealizados.FindAsync(exameRealizadoId);
                if (exame == null)
                    return Json(new { sucesso = false, mensagem = "Exame não encontrado." });

                if (exame.Situacao != 0)
                    return Json(new { sucesso = false, mensagem = "Este exame já foi enviado para análise." });

                //Feito pelo Qoder em 16/08/2026
                // Regra de negócio: o envio para análise só é permitido após o
                // recebimento na portaria — imediato do paciente (Status Recebido)
                // ou cobrança à instituição (checkbox, título Pendente). Ambos
                // marcam EmCatalogoRecebimentos = true no ato do lançamento.
                if (!exame.EmCatalogoRecebimentos)
                    return Json(new { sucesso = false, mensagem = "O exame só pode ser enviado para análise após o recebimento na portaria." });
                //..Qoder

                exame.Situacao = 1;
                await _db.SaveChangesAsync();

                _eventLogHelper.LogEventViewer($"Exame {exameRealizadoId} enviado para análise pelo usuário.", "Information");
                return Json(new { sucesso = true, mensagem = "Exame enviado para análise com sucesso." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer($"Erro ao enviar exame {exameRealizadoId} para análise: {ex.Message}", "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao enviar exame para análise: " + ex.Message });
            }
        }

        // Feito pelo Qoder em 15/08/2026 — retorna a quantidade de exames
        // pendentes (Situacao == 0) com mais de 24 horas de lançamento.
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Requisitar/VerificarExamesPendentes")]
        public IActionResult VerificarExamesPendentes()
        {
            try
            {
                // Feito pelo Qoder em 22/08/2026 — DataIni agora é DATE: regra de pendência passa a ser
                // "virada de dia" (passou-se 1 dia), conforme validação do usuário
                var limiteDia = _geralService.ObterDataHoraLocal().Date.AddDays(-1);
                int quantidade = _db.ExamesRealizados
                    .AsNoTracking()
                    .Count(e => e.Situacao == 0 && e.DataIni <= limiteDia);

                return Json(new { quantidade });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("Erro ao verificar exames pendentes: " + ex.Message, "wError");
                return Json(new { quantidade = 0 });
            }
        }

        // Feito pelo Qoder em 12/08/2026 — substituído query em Requisitar por ItensExamesRealizados.
        // Retorna os itens da tabela ItensExamesRealizados vinculados
        // a um ExameRealizadoId. Usado pela expansão master/detail do grid de
        // Requisições.
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Requisitar/GetItensRequisicao")]
        public IActionResult GetItensRequisicao(int exameRealizadoId)
        {
            try
            {
                var itens = _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => i.ExameRealizadoId == exameRealizadoId)
                    .OrderBy(i => i.OrdemItem)
                    .Select(i => new
                    {
                        i.Id,
                        i.ClasseExamesNome,
                        i.ContaExame,
                        i.Descricao,
                        ValorItem = i.ValorItem != null ? i.ValorItem.Value.ToString("N2") : "-",
                        i.Etiquetas,
                        i.OrdemItem
                    })
                    .ToList();

                return Json(new { sucesso = true, itens });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("GetItensRequisicao ERRO: " + ex.Message + " | " + ex.StackTrace, "wError");
                return Json(new { sucesso = false, mensagem = ex.Message });
            }
        }
        // ..Qoder


    }//Fim
}