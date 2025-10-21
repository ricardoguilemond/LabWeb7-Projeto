using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Integracoes.Exportacao;
using LabWebMvc.MVC.Integracoes.Interfaces;
using LabWebMvc.MVC.Integracoes.Interfaces.Parameters;
using LabWebMvc.MVC.Integracoes.Interfaces.Responses;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using static LabWebMvc.MVC.Integracoes.Enum;

namespace LabWebMvc.MVC.Integracoes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AttributeEnumType : Attribute
    {
        public Type ServiceType { get; }

        public AttributeEnumType(Type serviceType)
        {
            ServiceType = serviceType;
        }
    }

    public partial class Integracoes : IIntegracoes
    {
        #region Members

        private Db db;

        #endregion Members

        //class CampoExportacaoTexto
        //{
        //    public string NomeCampo;
        //    public int Tamanho;
        //    public string ValorFixo;
        //    public TipoCampo Tipo;
        //}

        /*
         * Nomes dos Layouts dos Serviços de Integração / Carga de Dados
         *
         */

        #region Enumerables dos Serviços

        public enum LayoutExecucao
        {
            [Description("Gera Arquivo Exportação de Pacientes")]
            [AttributeEnumType(typeof(ServicoExportacaoPacientes))]
            GERA_ARQUIVO_EXPORTACAO_PACIENTES = 1,

            //[Description("Gera Arquivo Exportação de Médicos"), AttributeEnumType(typeof(ServicoExportacaoMedicos))]
            //GERA_ARQUIVO_EXPORTACAO_MEDICOS = 2,

            //[Description("Gera Arquivo Exportação de Instituicoes"), AttributeEnumType(typeof(ServicoExportacaoInstituicoes))]
            //GERA_ARQUIVO_EXPORTACAO_INSTITUICOES = 3,

            //[Description("Gera Arquivo Exportação de Plano de Exames"), AttributeEnumType(typeof(ServicoExportacaoPlanoDeExames))]
            //GERA_ARQUIVO_EXPORTACAO_PLANO_DE_EXAMES = 4
        }

        #endregion Enumerables dos Serviços

        #region Constructors

        public Integracoes(Db db)
        {
            this.db = db;
        }

        #endregion Constructors

        #region Private Methods

        /// <summary>
        /// Função que retorna a nova data baseada na quantidade de dias que deverá ser alterada.
        /// </summary>
        /// <param name="dataBase">Data base, normalmente a data atual</param>
        /// <param name="qtdDias">Quantidade de dias a ser alterado da data</param>
        /// <returns></returns>
        private DateTime GetNovaData(DateTime dataBase, int qtdDias)
        {
            if (qtdDias % 365 == 0)
            {
                return dataBase.AddYears(qtdDias / 365);
            }
            else
            if (qtdDias % 30 == 0)
            {
                return dataBase.AddMonths(qtdDias / 30);
            }
            else
            {
                return dataBase.AddDays(qtdDias);
            }
        }

        private DateTime GetNovaDataMes(DateTime dataBase, int qtdMeses)
        {
            {
                return dataBase.AddMonths(qtdMeses);
            }
        }

        #endregion Private Methods

        #region Public Methods

        public virtual ObterConfiguracoesServicoResponse ObterConfiguracoesServico(ObterConfiguracoesServicoParameter parameter)
        {
            /* Retorna com a configuração dos serviços
             */
            ObterConfiguracoesServicoResponse response = new();

            IntegracaoDadosLayout? configuracoes = db.IntegracaoDadosLayout.Include(a => a.IntegracaoDadosConfiguracao).Where(a => a.TipoServico == parameter.TipoServico && a.Habilitado).FirstOrDefault();

            if (configuracoes != null)
            {
                response.PastaEntrada = configuracoes.IntegracaoDadosConfiguracao.PastaEntrada;
                response.PastaSaida = configuracoes.IntegracaoDadosConfiguracao.PastaSaida;
                response.PastaEntradaProcessado = configuracoes.IntegracaoDadosConfiguracao.PastaEntradaProcessado;
                response.PastaEntradaProcessadoErro = configuracoes.IntegracaoDadosConfiguracao.PastaEntradaProcessadoErro;
            }
            return response;
        }

        public virtual GravarIntegracaoDadosExecucaoResponse GravarIntegracaoDadosExecucao(GravarIntegracaoDadosExecucaoParameter parameter)
        {
            var eventLog = new ExtensionsMethods.EventViewerHelper.EventLogHelper();

            /* Registra os dados de execução dos serviços realizados
             */
            GravarIntegracaoDadosExecucaoResponse response = new();
            try
            {
                IntegracaoDadosLayout? tipoServico = db.IntegracaoDadosLayout.Where(a => a.TipoServico == parameter.TipoServico).SingleOrDefault();

                if (tipoServico == null)
                {
                    response.Sucesso = false;
                    response.Mensagem = "Tipo de serviço não localizado na tabela: IntegracaoDadosLayout";
                }
                else
                {
                    IntegracaoDadosExecucao dadosExecucao = new();
                    db.IntegracaoDadosExecucao.Add(dadosExecucao);

                    dadosExecucao.NomeArquivo = parameter.NomeArquivo;
                    dadosExecucao.NomeServico = parameter.NomeServico;
                    dadosExecucao.Header = parameter.Header;
                    dadosExecucao.Inicio = new DateTime(parameter.AnoInicio, parameter.MesInicio, parameter.DiaInicio, parameter.HoraInicio, parameter.MinutoInicio, 0);
                    dadosExecucao.IntegracaoDadosLayoutId = tipoServico.Id;
                    dadosExecucao.Resumo = parameter.Resumo;
                    dadosExecucao.Summary = parameter.Summary;
                    dadosExecucao.Sucesso = parameter.Sucesso;
                    dadosExecucao.Termino = DateTime.Now;

                    if (parameter != null && (parameter.NomeServico != null))
                    {
                        if (db.SaveChanges() <= 0)
                        {
                            response.Sucesso = false;
                            response.Mensagem = "Não foi possível salvar os dados de Integração Dados Execução (IntegracaoDadosExecucao)";
                        }
                        else
                        {
                            IntegracaoDadosExecucaoArquivo dadosExecucaoArquivo = new();
                            db.IntegracaoDadosExecucaoArquivo.Add(dadosExecucaoArquivo);

                            dadosExecucaoArquivo.IntegracaoDadosExecucaoId = dadosExecucao.Id;
                            dadosExecucaoArquivo.NomeArquivo = parameter.NomeArquivo;

                            if (!string.IsNullOrEmpty(tipoServico.IntegracaoDadosConfiguracao.PastaSaida))
                                dadosExecucaoArquivo.NomeArquivoGerado = parameter.NomeArquivo;

                            if (!string.IsNullOrEmpty(tipoServico.IntegracaoDadosConfiguracao.PastaEntrada))
                                dadosExecucaoArquivo.NomeArquivoProcessado = parameter.NomeArquivo;

                            dadosExecucaoArquivo.Resumo = parameter.Resumo;
                            dadosExecucaoArquivo.Status = parameter.Sucesso ? 1 : 2;

                            if (db.SaveChanges() <= 0)
                            {
                                response.Sucesso = false;
                                response.Mensagem = "Não foi possível salvar os dados de Integração Dados Execução Arquivo (IntegracaoDadosExecucaoArquivo)";
                            }
                            else
                            {
                                response.Sucesso = true;
                                response.Mensagem = "Sucesso";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Sucesso = false;
                response.Mensagem = ex.Message;
                eventLog.LogEventViewer("[GravarIntegracaoDadosExecucao] Exception: " + ex.Message, "wError");
            }

            return response;
        }

        public virtual RodarIntegracaoAgendadaResponse RodarIntegracaoAgendada()
        {
            RodarIntegracaoAgendadaResponse response = new()
            {
                Log = []
            };

            var configuracoesLayouts = (from conf in db.IntegracaoDadosConfiguracao
                                        join layout in db.IntegracaoDadosLayout on conf.Id equals layout.IntegracaoDadosConfiguracaoId
                                        join sistema in db.IntegracaoDadosArmazenamento on conf.IntegracaoDadosArmazenamentoId equals sistema.Id
                                        select new { conf, layout, sistema }).ToList();

            if (configuracoesLayouts != null)
            {
                string? Sistema = configuracoesLayouts.Select(s => s.sistema.UsuarioLogin).Distinct().FirstOrDefault();
                IntegracaoDadosLayout? Servico = configuracoesLayouts.Select(s => s.layout).Distinct().FirstOrDefault();
                string nomeServico = Servico != null ? Servico.IntegracaoDadosConfiguracao.NomeArquivo : "";
                string descricaoServico = Servico != null ? Servico.Descricao : "Serviço não identificado";

                if (configuracoesLayouts != null && Servico != null && Servico.Habilitado == true)
                {
                    foreach (IntegracaoDadosConfiguracao? configuracao in configuracoesLayouts.Select(s => s.conf).Distinct())
                    {
                        List<IntegracaoDadosLayout>? layouts = configuracoesLayouts?.Where(a => a.conf.Id == configuracao.Id).Select(s => s.layout).ToList();
                        if (layouts != null)
                        {
                            foreach (IntegracaoDadosLayout? layout in layouts)
                            {
                                //Verifica login do usuário padrão do sistema, antes de tentar o rodar o serviço!
                                if (configuracoesLayouts != null && Sistema != null)
                                {
                                    string? login = Utils.GetValorSetupDoServico("LoginPadraoSistema", "Sistema");
                                    string? senha = Utils.GetValorSetupDoServico("LoginPadraoSistema", "Senha");
                                    string? host = Utils.GetValorSetupDoServico("LoginPadraoSistema", "Host");

                                    if (configuracoesLayouts.Select(s => s.sistema.UsuarioLogin != login && s.sistema.Senha != senha).ToList()[0])
                                        response.Log.Add(string.Format("A conta de usuário do sistema para o {0}: {1} NÃO EXISTE ou é falsa. Tabela 'IntegracaoDadosAramazenamento'", nomeServico, descricaoServico));
                                }
                                else
                                {
                                    response.Log.Add(string.Format("Login e senha divergentes ou faltante no arquivo de configuração 'appsettings.json' do serviço {0} : {1}", nomeServico, descricaoServico));
                                }
                                //Adaptado para rodar sempre o serviço
                                LayoutExecucao layoutType = (LayoutExecucao)layout.TipoServico;
                                if (layout.TipoServico == (int)layoutType)
                                {
                                    //Valida o Horário de iniciar o serviço...
                                    if (!string.IsNullOrEmpty(configuracao.HoraExecucao))
                                    {
                                        string[] s = configuracao.HoraExecucao.Split(':');
                                        if (s.Length > 0)
                                        {
                                            string hora = s[0];
                                            string minuto = s.Length > 1 ? s[1] : "0";
                                            string segundo = s.Length > 2 ? s[2] : "0";
                                            TimeSpan horaConfigurada = new(Convert.ToInt32(hora), Convert.ToInt32(minuto), Convert.ToInt32(segundo));
                                            if (new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0) < horaConfigurada)
                                            {
                                                //ainda não deu o horario para executar
                                                continue;
                                            }
                                        }
                                    }
                                    //Valida a Hora de terminar o serviço e não executá-lo mais no dia...
                                    if (!string.IsNullOrEmpty(configuracao.HoraEncerramento))
                                    {
                                        string[] s = configuracao.HoraEncerramento.Split(':');
                                        if (s.Length > 0)
                                        {
                                            string hora = s[0];
                                            string minuto = s.Length > 1 ? s[1] : "0";
                                            string segundo = s.Length > 2 ? s[2] : "0";
                                            TimeSpan horaConfigurada = new(Convert.ToInt32(hora), Convert.ToInt32(minuto), Convert.ToInt32(segundo));
                                            if (new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0) > horaConfigurada)
                                            {
                                                //já passou do horário de executar o serviço
                                                continue;
                                            }
                                        }
                                    }
                                    DateTime dataHoje = DateTime.Now.Date;
                                    if (configuracao.Periodicidade == (int)TipoPeriodoExtracao.Diario)//diario=1
                                    {
                                        //verifica se ja rodou anteriormente
                                        if (db.IntegracaoDadosExecucao.Any(a => a.IntegracaoDadosLayoutId == layout.Id && (DateTime.Compare(a.Inicio, dataHoje) == 0) && (a.Termino > DateTime.MinValue) && a.Sucesso))
                                        {
                                            //ja executado anteriormente com sucesso. Ignora
                                            continue;
                                        }
                                    }
                                    if (configuracao.Periodicidade == (int)TipoPeriodoExtracao.Mensal)//mensal=3
                                    {
                                        if (configuracao.DiaExecucao == DateTime.Now.Day)
                                        {
                                            if (db.IntegracaoDadosExecucao.Any(a => a.IntegracaoDadosLayoutId == layout.Id && (DateTime.Compare(a.Inicio, dataHoje) == 0) && (a.Termino > DateTime.MinValue) && a.Sucesso))
                                            {
                                                //ja executado anteriormente com sucesso. Ignora
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            //não é o dia de execução
                                            continue;
                                        }
                                    }
                                }
                                //******************************************************************************************************
                                // Rodar Efetivamente o Serviço
                                // Roda o mesmo serviço da lista de servicos configurados nas tabelas, a cada vez que passar aqui...
                                //******************************************************************************************************
                                response.Log.Add(string.Format("Iniciou a execução efetiva do {0}: {1}", nomeServico, descricaoServico));

                                if (System.Enum.GetValues(typeof(LayoutExecucao)).Cast<int>().Contains<int>(layout.TipoServico))
                                {
                                    layoutType = (LayoutExecucao)layout.TipoServico;
                                    AttributeEnumType? attrType = layoutType.GetAttribute<AttributeEnumType>();
                                    if (attrType != null)
                                    {
                                        if (attrType.ServiceType.IsSubclassOf(typeof(ServicoIntegracao)))
                                        {
                                            ServicoIntegracao? servico = Activator.CreateInstance(attrType.ServiceType) as ServicoIntegracao;
                                            if (servico != null)
                                                servico.ExecutaServico(layout, nomeServico);
                                            else response.Log.Add(string.Format("Mesmo depois de carregado, não conseguiu rodar o serviço {0}: {1}", nomeServico, descricaoServico));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            response.Log.Add(string.Format("Não consegui ler a tabela de 'IntegracaoDadosLayouts' do serviço {0} : {1}", nomeServico, descricaoServico));
                        }
                        //Deve ainda disparar e-mail para o administrador sobre exportação acontecendo...
                        //EnviaEmail(start, "Início", "Exportação do Cadastro de Pacientes");
                        //EnviaEmail(start, "Fim", "Exportação do Cadastro de Pacientes");

                        response.Log.Add(string.Format("Terminou a execução efetiva do {0}: {1}", nomeServico, descricaoServico));
                    }
                }
                else
                {
                    response.Log.Add(string.Format("O '{0}: {1}' não está habilitado na tabela 'IntegracaoDadosLayout' para funcionar neste sistema", nomeServico, descricaoServico));
                }
            }
            else
            {
                response.Log.Add("Não consegui ler as tabelas de 'IntegracaoDados(..)' para poder prosseguir com o serviço de integração");
            }
            return response;
        }

        /*
         * Deixar assim mesmo.
         * Era para set com o próprio parameter, mas eu sobrescrevi acima em outro método com outro tipo de parâmetro
         * Este poderá ser utilizado para outras razões.
         */

        public RodarIntegracaoAgendadaResponse RodarIntegracaoAgendada(RodarIntegracaoAgendadaParameter parameter)
        {
            throw new NotImplementedException();
        }

        //public virtual FiltrarIntegracoesResponse FiltrarIntegracao(FiltrarIntegracoesParameter parameter)
        //{
        //    FiltrarIntegracoesResponse response = new FiltrarIntegracoesResponse();

        //    var integracoes = from exec in db.IntegracaoDadosExecucoes
        //                      join layout in db.IntegracaoDadosLayouts on exec.IntegracaoDadosLayoutID equals layout.ID
        //                      join conf in db.IntegracaoDadosConfiguracoes on layout.IntegracaoDadosConfiguracaoID equals conf.ID
        //                      select new { exec, layout, conf };
        //    if (parameter.DataInicio == null)
        //    {
        //        response.Errors.Add(new ErrorTO { Description = "Data de ínicio não pode ser nula" });
        //        return response;
        //    }
        //    integracoes = integracoes.Where(w => w.exec.Inicio >= parameter.DataInicio);
        //    if (parameter.DataFim != null)
        //    {
        //        var data = (parameter.DataFim ?? DateTime.Now);
        //        data = new DateTime(data.Year, data.Month, data.Day, 23, 59, 59);
        //        integracoes = integracoes.Where(w => w.exec.Inicio <= data);
        //    }
        //    if (parameter.Layout != null && parameter.Layout > 0)
        //    {
        //        integracoes = integracoes.Where(w => w.layout.Tipo == parameter.Layout);
        //    }
        //    if (parameter.Status != null && parameter.Status > 0)
        //    {
        //        integracoes = integracoes.Where(w => w.exec.Sucesso == (parameter.Status == 2));
        //    }
        //    response.Total = integracoes.Count();
        //    integracoes = integracoes.OrderByDescending(d => d.exec.Inicio);
        //    if (parameter.Take > 0)
        //    {
        //        integracoes = integracoes.Skip(parameter.Skip).Take(parameter.Take);
        //    }

        //    response.Integracoes = integracoes.ToList()
        //        .Select(s => new IntegracoesFiltrarIntegracoesTO
        //        {
        //            Arquivo = s.exec.Arquivo,
        //            Descricao = s.layout.Descricao,
        //            DiretorioEntrada = s.conf.DiretorioEntrada,
        //            DiretorioSaida = s.conf.DiretorioSaida,
        //            Inicio = s.exec.Inicio.ToString("dd/MM/yyyy HH:mm:ss"),
        //            Resumo = s.exec.Resumo,
        //            Termino = s.exec.Termino != null ? (s.exec.Termino ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm:ss") : "",
        //            Tipo = Enum.GetValues(typeof(LayoutExecucao)).Cast<int>()
        //                    .Contains<int>(s.layout.Tipo) ? ((LayoutExecucao)s.layout.Tipo).Description() : "",
        //            Periodicidade = ((PeriodicidadeExecucao)s.conf.Periodicidade).ToString(),
        //            Status = s.exec.Sucesso ? "Processado" : "Não processado",
        //            IDExecucao = s.exec.ID

        //        }).ToList();

        //    return response;
        //}

        //public virtual RecuperarListaFiltrarIntegracaoResponse RecuperarListaFiltrarIntegracao(RecuperarListaFiltrarIntegracaoParameter parameter)
        //{
        //    RecuperarListaFiltrarIntegracaoResponse response = new RecuperarListaFiltrarIntegracaoResponse();
        //    var tipos = Enum.GetValues(typeof(LayoutExecucao)).Cast<LayoutExecucao>();
        //    response.Tipo = new List<ListaValorFiltraIntegracaoTO>();

        //    response.Tipo.Add(new ListaValorFiltraIntegracaoTO { Id = 0, Descricao = "Todos" });
        //    response.Tipo.AddRange(tipos.Select(S => new ListaValorFiltraIntegracaoTO { Id = (int)S, Descricao = S.Description() }));
        //    response.Status = new List<ListaValorFiltraIntegracaoTO>();
        //    response.Status.Add(new ListaValorFiltraIntegracaoTO { Id = 0, Descricao = "Todos" });
        //    response.Status.Add(new ListaValorFiltraIntegracaoTO { Id = 1, Descricao = "Não processado" });
        //    response.Status.Add(new ListaValorFiltraIntegracaoTO { Id = 2, Descricao = "Processado" });

        //    return response;
        //}

        //public virtual BuscarArquivosExecucaoResponse BuscarArquivosExecucao(BuscarArquivosExecucaoParameter parameter)
        //{
        //    BuscarArquivosExecucaoResponse response = new BuscarArquivosExecucaoResponse();

        //    //response.Errors.Add(new NotImplementedException("Método não implementado.").Error());
        //    var arquivos = db.IntegracaoDadosExecucaoArquivos
        //        .Include(i => i.IntegracaoDadosExecucao.IntegracaoDadosLayout.IntegracaoDadosConfiguracao)
        //        .Where(w => w.IntegracaoDadosExecucaoID == parameter.IDExecucao)
        //        .OrderBy(o => o.NomeArquivo).ToList();
        //    response.Total = arquivos.Count();
        //    response.Arquivos = arquivos.Select(s => new ArquivoExecucaoTO
        //    {
        //        CodigoStatusArquivo = s.Status,
        //        IDArquivoExecucao = s.ID,
        //        NomeArquivo = s.NomeArquivo,
        //        CaminhoGeracao = s.IntegracaoDadosExecucao.IntegracaoDadosLayout.IntegracaoDadosConfiguracao.DiretorioSaida,
        //        StatusArquivo = ((IntegracaoExecucaoArquivoStatus)s.Status).Description(),
        //        Resumo = s.Resumo

        //    }).ToList();

        //    return response;
        //}

        //public virtual ExecutarServicoIntegracaoResponse ExecutarServicoIntegracao(ExecutarServicoIntegracaoParameter parameter)
        //{
        //    ExecutarServicoIntegracaoResponse response = new ExecutarServicoIntegracaoResponse();

        //    //Recupera a conexão com banco de dados.
        //    var connectionString = ConfigurationManager.ConnectionStrings["Context"].ConnectionString;
        //    var baseCAP = ConfigurationManager.AppSettings["DatabaseCAP_" + parameter.UnityID];
        //    var baseGERPF = ConfigurationManager.AppSettings["DatabaseGERPF_" + parameter.UnityID];

        //    if (parameter != null && parameter.IdLayoutExecucao > 0)
        //    {
        //        var layout = db.IntegracaoDadosLayouts.FirstOrDefault(f => f.Tipo == parameter.IdLayoutExecucao);
        //        if (layout != null)
        //        {
        //            if (db.IntegracaoDadosExecucoes.Any(a => a.Termino == null && a.IntegracaoDadosLayoutID == layout.ID))
        //            {
        //                response.Erro = true;
        //                response.MsgResposta = "Este serviço já está sendo executado no momento, favor tentar novamente mais tarde.";
        //                return response;
        //            }
        //            if (Enum.GetValues(typeof(LayoutExecucao)).Cast<int>()
        //                   .Contains<int>(layout.Tipo))
        //            {
        //                var layoutType = (LayoutExecucao)layout.Tipo;
        //                var attrType = layoutType.GetAttribute<AttributeEnumType>();
        //                if (attrType != null)
        //                {
        //                    if (attrType.type.IsSubclassOf(typeof(ServicoIntegracao)))
        //                    {
        //                        var servico = (ServicoIntegracao)Activator.CreateInstance(attrType.type);
        //                        servico.ExecutaServico(layout, connectionString, baseCAP, baseGERPF);
        //                    }
        //                }
        //                response.Erro = false;
        //                response.MsgResposta = "Serviço sendo executado. Atualize a página de Log de Execução para verificar Status.";
        //            }
        //            else
        //            {
        //                response.Erro = true;
        //                response.MsgResposta = "Não foi possível encontrar nenhum Serviço com esse layout configurado para esta operadora";
        //            }
        //        }
        //        else
        //        {
        //            response.Erro = true;
        //            response.MsgResposta = "Não foi possível encontrar nenhum Serviço com esse layout configurado para esta operadora";
        //        }
        //    }
        //    return response;
        //}

        //public virtual RecuperaDadosFiltroArquivosResponse RecuperaDadosFiltroArquivos(RecuperaDadosFiltroArquivosParameter parameter)
        //{
        //    RecuperaDadosFiltroArquivosResponse response = new RecuperaDadosFiltroArquivosResponse();

        //    var statusArquivos = Enum.GetValues(typeof(FatFaturaRemessaArquivoStatus)).Cast<FatFaturaRemessaArquivoStatus>().Select(s =>
        //      new ItemGenericoTO
        //      {
        //          ID = (int)s,
        //          Descricao = s.Description()
        //      }
        //    ).ToList();
        //    statusArquivos.Add(new ItemGenericoTO
        //    {
        //        ID = 0,
        //        Descricao = "Todos"
        //    });
        //    statusArquivos = statusArquivos.OrderBy(o => o.ID).ToList();
        //    response.StatusArquivo = statusArquivos;

        //    var tiposArquivos = Enum.GetValues(typeof(FatFaturaRemessaTipoArquivo)).Cast<FatFaturaRemessaTipoArquivo>().Select(s =>
        //     new ItemGenericoTO
        //     {
        //         ID = (int)s,
        //         Descricao = s.Description()
        //     }
        //   ).ToList();
        //    tiposArquivos.Add(new ItemGenericoTO
        //    {
        //        ID = 0,
        //        Descricao = "Todos"
        //    });
        //    tiposArquivos = tiposArquivos.OrderBy(o => o.ID).ToList();
        //    response.TiposArquivo = tiposArquivos;

        //    return response;
        //}

        #endregion Public Methods
    }
}