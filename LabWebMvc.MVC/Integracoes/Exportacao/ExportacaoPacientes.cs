using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Integracoes.Interfaces.Parameters;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using static LabWebMvc.MVC.Integracoes.Enum;

namespace LabWebMvc.MVC.Integracoes.Exportacao
{
    public class ServicoExportacaoPacientes : ServicoIntegracao
    {
        private readonly IConfiguration _config;
        public ServicoExportacaoPacientes(IConnectionService connectionService,
                                          IConfiguration config,
                                          IEventLogHelper eventLogHelper)
            : base(new LabWebMvc.MVC.Models.Db(
                new DbContextOptionsBuilder<LabWebMvc.MVC.Models.Db>()
                    .UseNpgsql(connectionService.GetConnectionString())
                    .Options,
                connectionService,
                eventLogHelper))
        {
            _config = config;
            // _db já está disponível via base ServicoIntegracao
        }

        protected override void RealizaOperacao(IntegracaoDadosLayout layout, IntegracaoDadosExecucao execucao)
        {
            var eventLog = new ExtensionsMethods.EventViewerHelper.EventLogHelper();

            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction dbContextTransaction = _db.Database.BeginTransaction())
            {
                try
                {
                    #region Validações / Pré-Requisitos

                    //Valida se o diretório de saída está configurado para o serviço
                    Interfaces.Responses.ObterConfiguracoesServicoResponse configServico = new Integracoes(_db).ObterConfiguracoesServico(new ObterConfiguracoesServicoParameter { TipoServico = 1 });
                    if (string.IsNullOrEmpty(configServico.PastaSaida))
                        throw new Exception("A pasta de 'saída' ainda não foi configurada para esse serviço.");

                    #endregion Validações / Pré-Requisitos

                    #region Configurações da Execução

                    IntegracaoDadosConfiguracao? config = _db.IntegracaoDadosConfiguracao.Where(w => w.Id == layout.IntegracaoDadosConfiguracaoId).FirstOrDefault();
                    string outputDir = string.IsNullOrEmpty(config?.PastaSaida) ? "C:\\Temp\\Exportacao\\" : config.PastaSaida;
                    string? inputDirProcessado = config?.PastaEntradaProcessado;
                    string? inputDirProcessadoErro = config?.PastaEntradaProcessadoErro;

                    #endregion Configurações da Execução

                    //Consulta pacientes
                    var dados = (from pac in _db.Pacientes

                                 select new
                                 {
                                     p = pac,   //aqui somente quando pegar todos os campos da tabela sem qualquer tratamento
                                     Id = pac.Id,
                                     DataNascimento = pac.Nascimento.ToString("yyyyMMdd"),
                                     Dum = pac.DUM.HasValue ? pac.DUM.Value.ToString("yyyyMMdd") : string.Empty,
                                     DataEntrada = pac.DataEntrada.ToString("yyyyMMdd"),
                                     DataEntradaBrasil = pac.DataEntradaBrasil.HasValue ? pac.DataEntradaBrasil.Value.ToString("yyyyMMdd") : string.Empty,
                                     DataRegistro = pac.DataRegistro.ToString("yyyyMMdd"),
                                     DataBaixa = pac.DataBaixa.GetValueOrDefault().ToString("yyyyMMdd"),
                                     Observacao = pac.Observacao != null ? pac.Observacao.Substring(0, 3999) : string.Empty
                                 }).Where(l => l.Id > 0)
                                   .AsEnumerable();

                    int totalRegistros = 0;
                    string msgSucesso = "{0} registros 'Pacientes' processados com sucesso";
                    string msgSemRegistros = "Nada a ser processado em 'Pacientes'. No momento não existem novos dados disponíveis";

                    string filename = string.Empty;
                    int quantidade = dados.Count();
                    if (quantidade > 0)  //parece ser mais rápido e menos problemático que dados.Any()
                    {
                        TipoPeriodoExtracao tipoExtracao = TipoPeriodoExtracao.Diario;

                        DateTime primeiraData = DateTime.Today.DataCalculada(tipoExtracao, "inicial");  //primeira data do período
                        DateTime ultimaData = DateTime.Today.DataCalculada(tipoExtracao);               //última data do periodo

                        /* ENTRETANTO, na tabela de Layout, quando os campos DataInicial e DataFinal estiverem preenchidos,
                         * então eles serão prioridade!
                         * Quando os campos DataPeriodoInicial e DataPeriodoFinal forem NULL, então prevalece a data padronizada.
                         */
                        primeiraData = layout.DataInicial.HasValue ? layout.DataInicial.Value : primeiraData;
                        ultimaData = layout.DataFinal.HasValue ? layout.DataFinal.Value : ultimaData;

                        filename = "EXP_PACIENTES_".NomeArquivoIncremental(ultimaData, _db, execucao);

                        //Lista com as linhas do arquivo
                        HashSet<string> objRegistrosArquivo = [];   // importantíssimo para não ter linhas iguais

                        bool primeiraLinha = true;
                        string DataHoraExportacao = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

                        foreach (var dado in dados)
                        {
                            string linhaArquivo = Titulos();

                            if (primeiraLinha)
                            {
                                linhaArquivo = linhaArquivo.Replace("[", "").Replace("]", "");  /* nomes dos campos */
                                primeiraLinha = false;
                                objRegistrosArquivo.Add(linhaArquivo);
                                linhaArquivo = Titulos();
                            }

                            linhaArquivo = linhaArquivo.Replace("[ID]", dado.Id.ToString());
                            linhaArquivo = linhaArquivo.Replace("[NOME_PACIENTE]", dado.p.NomePaciente);
                            linhaArquivo = linhaArquivo.Replace("[DATA_NASCIMENTO]", dado.DataNascimento);
                            linhaArquivo = linhaArquivo.Replace("[NOME_SOCIAL]", dado.p.NomeSocial);
                            linhaArquivo = linhaArquivo.Replace("[NOME_PAI]", dado.p.NomePai);
                            linhaArquivo = linhaArquivo.Replace("[NOME_MAE]", dado.p.NomeMae);
                            linhaArquivo = linhaArquivo.Replace("[CPF]", dado.p.CPF);
                            linhaArquivo = linhaArquivo.Replace("[TIPO_DOCUMENTO]", dado.p.TipoDocumento.ToString());
                            linhaArquivo = linhaArquivo.Replace("[IDENTIDADE]", dado.p.Identidade);
                            linhaArquivo = linhaArquivo.Replace("[EMISSOR]", dado.p.Emissor.ToString());
                            linhaArquivo = linhaArquivo.Replace("[CARTEIRA_SUS]", dado.p.CarteiraSUS);
                            linhaArquivo = linhaArquivo.Replace("[ESTADO_CIVIL]", dado.p.EstadoCivil.ToString());
                            linhaArquivo = linhaArquivo.Replace("[SEXO]", dado.p.Sexo);
                            linhaArquivo = linhaArquivo.Replace("[TIPO_SANGUINEO]", dado.p.TipoSanguineo);
                            linhaArquivo = linhaArquivo.Replace("[DUM]", dado.Dum);
                            linhaArquivo = linhaArquivo.Replace("[TEMPO_GESTACAO]", dado.p.TempoGestacao.ToString());
                            linhaArquivo = linhaArquivo.Replace("[PROFISSAO]", dado.p.Profissao);
                            linhaArquivo = linhaArquivo.Replace("[NATURALIDADE]", dado.p.Naturalidade);
                            linhaArquivo = linhaArquivo.Replace("[NACIONALIDADE]", dado.p.Nacionalidade);
                            linhaArquivo = linhaArquivo.Replace("[DATA_ENTRADA_BRASIL]", dado.DataEntradaBrasil);
                            linhaArquivo = linhaArquivo.Replace("[LOGRADOURO]", dado.p.Logradouro);
                            linhaArquivo = linhaArquivo.Replace("[ENDERECO]", dado.p.Endereco);
                            linhaArquivo = linhaArquivo.Replace("[NUMERO]", dado.p.Numero);
                            linhaArquivo = linhaArquivo.Replace("[COMPLEMENTO]", dado.p.Complemento);
                            linhaArquivo = linhaArquivo.Replace("[BAIRRO]", dado.p.Bairro);
                            linhaArquivo = linhaArquivo.Replace("[CIDADE]", dado.p.Cidade);
                            linhaArquivo = linhaArquivo.Replace("[UF]", dado.p.UF);
                            linhaArquivo = linhaArquivo.Replace("[CEP]", dado.p.CEP);
                            linhaArquivo = linhaArquivo.Replace("[TELEFONE]", dado.p.Telefone);
                            linhaArquivo = linhaArquivo.Replace("[EMAIL]", dado.p.Email);
                            linhaArquivo = linhaArquivo.Replace("[OBSERVACAO]", dado.Observacao);
                            linhaArquivo = linhaArquivo.Replace("[DATA_ENTRADA]", dado.DataEntrada);
                            linhaArquivo = linhaArquivo.Replace("[DATA_BAIXA]", dado.DataBaixa);
                            linhaArquivo = linhaArquivo.Replace("[STATUS_BAIXA]", dado.p.StatusBaixa.ToString());
                            linhaArquivo = linhaArquivo.Replace("[DATA_REGISTRO]", dado.DataRegistro);
                            linhaArquivo = linhaArquivo.Replace("[DATA_HORA_EXPORTACAO]", DataHoraExportacao);

                            objRegistrosArquivo.Add(linhaArquivo);

                            //Grava log de processamento do motivo de endosso
                            _db.LogArquivos.Add(new LogArquivos
                            {
                                StrRef = string.Format("Id Paciente: {0} Nome: {1}", dado.Id.ToString(), dado.p.NomePaciente),
                                Data = DateTime.Now,
                                DataPeriodoInicial = primeiraData,
                                DataPeriodoFinal = ultimaData,
                                IntegracaoDadosLayoutId = layout.Id,
                                NomeArquivo = filename,
                                TipoServico = (int)TipoArquivoIntegracoes.ExportacaoCadastroPacientes
                            });

                            totalRegistros++;
                        }

                        totalRegistros = objRegistrosArquivo.Count;  // acerto no total de registros pós HashSet

                        //Grava log de criação de arquivo
                        _db.IntegracaoDadosExecucaoArquivo.Add(new IntegracaoDadosExecucaoArquivo
                        {
                            IntegracaoDadosExecucaoId = execucao.Id,
                            NomeArquivo = filename,
                            NomeArquivoGerado = filename,
                            Status = (int)IntegracaoExecucaoArquivoStatus.Sucesso,
                            Resumo = string.Format(msgSucesso, totalRegistros)
                        });

                        //Gravação do arquivo na pasta específica da configuração
                        File.WriteAllLines(Path.Combine(configServico.PastaSaida, filename), objRegistrosArquivo, new UTF8Encoding(false));

                        int salvamento = _db.SaveChanges();
                        if (salvamento < 0)
                        {
                            LoggerFile.Write("Erro ao tentar salvar a geração do arquivo Exportação de Pacientes. Tente verificar se há Log em 'LogArquivo'");
                        }
                        //Faz o commit depois que todo o processamento for finalizado
                        dbContextTransaction.Commit();
                    }

                    //Monta o resumo do processamento para posteriormente ser gravado como log na tabela "IntegracaoDadosExecucao"
                    if (totalRegistros > 0)
                    {
                        execucao.Resumo = string.Format(msgSucesso, totalRegistros);
                        execucao.NomeArquivo = filename;
                        execucao.Header = Titulos();
                        execucao.Summary = "N/A";
                    }
                    else
                        execucao.Resumo = msgSemRegistros;

                    execucao.Sucesso = true;
                }
                catch (Exception e)
                {
                    dbContextTransaction.Rollback();

                    execucao.Resumo = e.Message;
                    eventLog.LogEventViewer("[ExportacaoPacientes] Exportação de Pacientes ::: Erro ao tentar gerar o arquivo de Exportação de Pacientes ::: Message: " + e.Message, "wError");
                }
            }
        }

        private string Titulos()
        {
            return "[ID]".InsereAspasDuplas() + ";" +
                   "[NOME_PACIENTE]".InsereAspasDuplas() + ";" +
                   "[DATA_NASCIMENTO]".InsereAspasDuplas() + ";" +
                   "[NOME_SOCIAL]".InsereAspasDuplas() + ";" +
                   "[NOME_PAI]".InsereAspasDuplas() + ";" +
                   "[NOME_MAE]".InsereAspasDuplas() + ";" +
                   "[CPF]".InsereAspasDuplas() + ";" +
                   "[TIPO_DOCUMENTO]".InsereAspasDuplas() + ";" +
                   "[IDENTIDADE]".InsereAspasDuplas() + ";" +
                   "[EMISSOR]".InsereAspasDuplas() + ";" +
                   "[CARTEIRA_SUS]".InsereAspasDuplas() + ";" +
                   "[ESTADO_CIVIL]".InsereAspasDuplas() + ";" +
                   "[SEXO]".InsereAspasDuplas() + ";" +
                   "[TIPO_SANGUINEO]".InsereAspasDuplas() + ";" +
                   "[DUM]".InsereAspasDuplas() + ";" +
                   "[TEMPO_GESTACAO]".InsereAspasDuplas() + ";" +
                   "[PROFISSAO]".InsereAspasDuplas() + ";" +
                   "[NATURALIDADE]".InsereAspasDuplas() + ";" +
                   "[NACIONALIDADE]".InsereAspasDuplas() + ";" +
                   "[DATA_ENTRADA_BRASIL]".InsereAspasDuplas() + ";" +
                   "[LOGRADOURO]".InsereAspasDuplas() + ";" +
                   "[ENDERECO]".InsereAspasDuplas() + ";" +
                   "[NUMERO]".InsereAspasDuplas() + ";" +
                   "[COMPLEMENTO]".InsereAspasDuplas() + ";" +
                   "[BAIRRO]".InsereAspasDuplas() + ";" +
                   "[CIDADE]".InsereAspasDuplas() + ";" +
                   "[UF]".InsereAspasDuplas() + ";" +
                   "[CEP]".InsereAspasDuplas() + ";" +
                   "[TELEFONE]".InsereAspasDuplas() + ";" +
                   "[EMAIL]".InsereAspasDuplas() + ";" +
                   "[OBSERVACAO]".InsereAspasDuplas() + ";" +
                   "[DATA_ENTRADA]".InsereAspasDuplas() + ";" +
                   "[DATA_BAIXA]".InsereAspasDuplas() + ";" +
                   "[STATUS_BAIXA]".InsereAspasDuplas() + ";" +
                   "[DATA_REGISTRO]".InsereAspasDuplas() + ";" +
                   "[DATA_HORA_EXPORTACAO]".InsereAspasDuplas() + ";";
        }
    }
}