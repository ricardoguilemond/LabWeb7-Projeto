using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace BrazilDental.BDConnect.Business
{
    /// <summary>
    /// Classe base para executar serviço independente
    /// </summary>
    public abstract class ServicoIntegracao
    {
        protected static List<String> connectionAndClassAtiva = new List<String>();

        private String GetCurrentService()
        {
            return this.GetType().Name;
        }

        /// <summary>
        /// Função que retorna se uma determinada conexão com banco de dados já está executando esta tarefa. 
        /// </summary>
        /// <param name="stringConexao">Retorna Verdadeiro se já tiver uma conexão ativa</param>
        /// <returns></returns>
        private bool ContainsStringConexao(String stringConexao)
        {
            return connectionAndClassAtiva.Contains(GetCurrentService() + "#" + stringConexao);
        }

        /// <summary>
        /// Adiciona uma determina conexão na lista de conexões a serem listadas
        /// </summary>
        /// <param name="stringConexao"></param>
        private void AddStringConexao(String stringConexao)
        {
            connectionAndClassAtiva.Add(GetCurrentService() + "#" + stringConexao);
        }

        private void RemoveSrtringConexao(String stringConexao)
        {
            connectionAndClassAtiva.Remove(GetCurrentService() + "#" + stringConexao);
        }

        public void ExecutaServico(IntegracaoDadosLayoutPO layout, String connectionString, String baseCAP = "", String baseGERPF = "")
        {
            //Executa a chamada do método em paralelo para evitar problemas na execução.
            Parallel.Invoke(new Action(() =>
            {
                if (!this.ContainsStringConexao(connectionString))
                {
                    try
                    {
                        this.AddStringConexao(connectionString);
                        IntegracaoDadosExecucaoPO execucao = null;
                        
                        //Cria o registro de execução.
                        using (var db = new DBContext(connectionString))
                        {
                            execucao = new IntegracaoDadosExecucaoPO
                            {
                                Inicio = DateTime.Now,
                                IntegracaoDadosLayoutID = layout.ID,
                                Sucesso = false,
                                Resumo = ""
                            };

                            db.IntegracaoDadosExecucoes.Add(execucao);
                            db.SaveChanges();
                        }
                        try
                        {
                            var restricao = this.ValidarRestricaoNegocio(connectionString, layout.Tipo);

                            if (restricao.IsNullOrTrimEmpty())
                                this.RealizaOperacao(layout, execucao, connectionString, baseCAP, baseGERPF);
                            else
                                execucao.Resumo = restricao;
                        }
                        catch (Exception ex)
                        {
                            execucao.Sucesso = false;
                            execucao.Resumo += ex.Message;

                            var innerEx = ex.InnerException;
                            while (innerEx != null)
                            {
                                execucao.Resumo += innerEx.Message;
                                innerEx = innerEx.InnerException;
                            }

                            LoggerFile.Write(ex);
                        }
                        finally
                        {
                            using (var db = new DBContext(connectionString))
                            {
                                execucao.Termino = DateTime.Now;
                                //Limita o tamanho do resumo
                                var resumo = execucao.Resumo;

                                if (String.IsNullOrEmpty(resumo))
                                {
                                    if (resumo.Length > 2000)
                                    {
                                        resumo = resumo.Substring(0, 2000);
                                        execucao.Resumo = resumo;
                                    }
                                }

                                db.Entry(execucao).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                        }
                    }
                    finally
                    {
                        this.RemoveSrtringConexao(connectionString);
                    }
                }
            }));
        }

        /// <summary>
        /// Recupera valor de uma determina sequence de Banco. Utilizada para garantir sequencia de Boleto, Conta, Debito Automatico, etc.
        /// </summary>
        /// <param name="db">Conexão com banco de dados</param>
        /// <param name="sequence">Nome da sequence de banco a ser utilizada.</param>
        /// <returns></returns>
        protected long GetNext(DBContext db, String sequence)
        {

            var rawQuery = db.Database.SqlQuery<long>("SELECT NEXT VALUE FOR dbo." + sequence);
            var task = rawQuery.First();
            return task;

        }

        protected abstract void RealizaOperacao(IntegracaoDadosLayoutPO layout, IntegracaoDadosExecucaoPO execucao, String connectionString, String baseCAP, String baseGERPF);

        private string ValidarRestricaoNegocio(String connectionString, int tipoServico)
        {
            /* identifica o tipo de serviço de integração que está sendo realizado */
            var TipoCPagar = Implementation.Integracoes.IntegracaoUtils.GetTiposServicosImpactoFinanceiroFiscal_CPagar(tipoServico);
            var TipoCReceber = Implementation.Integracoes.IntegracaoUtils.GetTiposServicosImpactoFinanceiroFiscal_CReceber(tipoServico);
            string TipoMensagem = TipoCPagar.Item1 > 0 ? TipoCPagar.Item2 :
                                  TipoCReceber.Item1 > 0 ? TipoCReceber.Item2 : "serviço";

            string mensagem = string.Format("O {0} não pode ser executado quando a integração financeira via ERP estiver habilitada.", TipoMensagem);

            if (TipoCPagar.Item1 > 0 || TipoCReceber.Item1 > 0)  /* se veio o código então tem restrições em algum dos serviços */
            {
                using (var db = new DBContext(connectionString))
                {
                    /* verificar se na tabela de operadora consta habilitado/desabilitado para os serviços restritos da lista
                     * quando habilitado a integração é realizada pelo próprio cliente (Erp do cliente)
                     * quando desabilitado a integração é pelo DBConnect (padrão/nosso Erp)
                     */
                    /* considerando que já estamos com a operadora do serviço (então só haverá uma, ela própria) */
                    var operadora = db.Operadoras.SingleOrDefault();
                    /* TblOperadoras, campos: CPagarIntegracao e CReceberIntegracao 
                     * (1=habilitado cliente/ERP quem faz) (0=desabilitado BDconnect quem faz)
                     */
                    if (TipoCPagar.Item1 > 0 && operadora.CPagarIntegracao == true) return mensagem;
                    else if (TipoCReceber.Item1 > 0 && operadora.CReceberIntegracao == true) return mensagem;
                }
            }
            return string.Empty;
        }
    }
