using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Integracoes
{
    public abstract class ServicoIntegracao
    {
        protected readonly Db _db;
        protected static List<string> connectionAndClassAtiva = [];

        protected ServicoIntegracao(Db db)
        {
            _db = db;
        }

        //private string GetCurrentService()
        //{
        //    return GetType().Name;
        //}

        public void ExecutaServico(IntegracaoDadosLayout layout, string nomeServico)
        {
            var eventLog = new ExtensionsMethods.EventViewerHelper.EventLogHelper();

            //Executa a chamada do método em paralelo para evitar problemas na execução.
            Parallel.Invoke(new Action(() =>
            {
                try
                {
                    /* Verifica se na configuração do serviço acionado, consta que se pode gerar mais de um arquivo no mesmo dia  */
                    IntegracaoDadosConfiguracao? config = _db.IntegracaoDadosConfiguracao.Where(w => w.Id == layout.IntegracaoDadosConfiguracaoId).FirstOrDefault();
                    string outputDir = string.IsNullOrEmpty(config?.PastaSaida) ? "C:\\Temp\\Exportacao\\" : config.PastaSaida;

                    IEnumerable<string> files = Directory.GetFiles(outputDir).Where(w => string.Equals(Path.GetExtension(w), ".txt", StringComparison.OrdinalIgnoreCase));

                    /* Se já houver exportado algum arquivo no dia, então não exporta mais.
                     * Para exportar de novo, basta apagar na pasta designada ao arquivo exportado
                     */
                    if (config != null && config.IntegraUmaUnicaVezNoDia)
                    {
                        string[] listaValidacao = new string[] { "PACIENTES", "EXAMES", "MEDICOS", "INSTITUICOES", "PLANO_DE_EXAMES" };

                        foreach (string? item in files)
                        {
                            foreach (string termo in listaValidacao)
                            {
                                if (item.Contains(termo) && item.Contains(DateTime.UtcNow.ToString("yyyyMMdd")))
                                {
                                    return;
                                }
                            }
                        }
                    }

                    IntegracaoDadosExecucao? execucao = null!;

                    //Cria o registro de execução.
                    execucao = new IntegracaoDadosExecucao
                    {
                        IntegracaoDadosLayoutId = layout.Id,
                        Inicio = DateTime.UtcNow,
                        Sucesso = false,
                        Resumo = ""
                    };
                    _db.IntegracaoDadosExecucao.Add(execucao);
                    _db.SaveChanges();

                    try
                    {
                        string restricao = ValidarRestricaoNegocio(layout.TipoServico);

                        if (string.IsNullOrEmpty(restricao.Trim()))
                            RealizaOperacao(layout, execucao);
                        else
                            execucao.Resumo = restricao;
                    }
                    catch (Exception ex)
                    {
                        execucao.Sucesso = false;
                        execucao.Resumo += ex.Message;

                        Exception? innerEx = ex.InnerException;
                        while (innerEx != null)
                        {
                            execucao.Resumo += innerEx.Message;
                            innerEx = innerEx.InnerException;
                        }
                        eventLog.LogEventViewer("[ServicoIntegracao] " + execucao.Resumo + " ::: Message: " + ex.Message, "wError");
                    }
                    finally
                    {
                        if (execucao.NomeArquivo != null)
                        {
                            execucao.Termino = DateTime.UtcNow;

                            //Limita o tamanho do resumo
                            string resumo = execucao.Resumo;
                            if (string.IsNullOrEmpty(resumo))
                            {
                                if (resumo.Length > 3999) //Trata texto do Resumo para tamanho máximo de 4000 (0 a 3999)
                                {
                                    resumo = resumo.Substring(0, 3999);
                                    execucao.Resumo = resumo;
                                }
                            }
                            execucao.NomeServico = nomeServico;
                            _db.Entry(execucao).State = EntityState.Modified;
                            _db.SaveChanges();
                        }
                    }
                }
                finally
                { }
            }));
        }

        protected abstract void RealizaOperacao(IntegracaoDadosLayout layout, IntegracaoDadosExecucao execucao);

        private string ValidarRestricaoNegocio(int tipoServico)
        {
            /* identifica o tipo de serviço de integração que está sendo realizado */
            string TipoMensagem = "serviço";

            string mensagem = string.Format("O {0} não pode ser executado porque não está habilitado.", TipoMensagem);

            /* verificar se na tabela "qualquer" consta algum flag para serviços restritos da lista
             */
            bool habilitado = true;
            if (!habilitado)
                return mensagem;

            return string.Empty;
        }
    }
}