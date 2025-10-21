   public class ServicoIntegracaoGeraArquivoClientesMapfreGlobal : ServicoIntegracao
   {
      protected override void RealizaOperacao(IntegracaoDadosLayoutPO layout, IntegracaoDadosExecucaoPO execucao, string connectionString, string baseCAP, string baseGERPF)
      {
         using (var db = new DBContext(connectionString))
         {
            using (var dbContextTransaction = db.Database.BeginTransaction())
            {
               try
               {
                  #region Validações / Pré-Requisitos

                  //Valida se o diretório de saída está configurado para o serviço
                  var configServico = new BIntegracoesBO(db).ObterConfiguracoesServico(new ObterConfiguracoesServicoParameter { Tipo = 38 });

                  if (configServico.DiretorioSaida.IsNullOrTrimEmpty())
                     throw new Exception("O diretório de 'saída' não foi configurado para esse serviço.");

                  #endregion

                  //Consulta as agências sucursais
                  var dados = (from loc in db.Locacoes

                               join cta in db.Contas on loc.ContaID equals cta.ID
                               join ctx in db.ContaXStatus on cta.ID equals ctx.ContaID

                               select new
                               {
                                  strCnpj = loc.CNPJ,
                                  strRazaoSocial = loc.RazaoSocial,
                                  strContaStatus = ctx.ContaStatusID == 1 ? "N" : "S",

                               }).Distinct().OrderBy(o => o.strCnpj).AsEnumerable();

                  var totalRegistros = 0;
                  var msgSucesso = "{0} registros processados com sucesso.";
                  var msgSemRegistros = "Nada a ser processado. No momento não existem novos dados disponíveis.";

                  if (dados.Any())
                  {
                     /* o período aqui é composto da "semana passada", ou seja, a extração do arquivo ocorre "hoje" para pegar 
                      * de segunda a domingo da semana anterior!
                      */
                     //DateTime startDate = DateTime.Today;   /* data de hoje, inicio */
                     //DateTime endDate = startDate;
                     
                     string filename = "DIM_CLIENTE_66_".NomeArquivoIncremental(TipoPeriodoExtracao.Semanal, db, execucao);
                     DateTime primeiraData = DateTime.Today.DataCalculada(TipoPeriodoExtracao.Semanal, "inicial");
                     DateTime ultimaData = DateTime.Today.DataCalculada(TipoPeriodoExtracao.Semanal);

                     //Lista com as linhas do arquivo
                     var objRegistrosArquivo = new HashSet<string>();

                     bool primeiraLinha = true;

                     foreach (var dado in dados)
                     {
                        var linhaArquivo = "[COD_CIA_LEGADO]".InsereAspasDuplas() + ";" +
                            "[COD_CIA_CNPJ]".InsereAspasDuplas() + ";" +
                            "[DATA_PROCESSO_COREBI]".InsereAspasDuplas() + ";";

                        if (primeiraLinha)
                        {
                           linhaArquivo = linhaArquivo.Replace("[", "").Replace("]","");  /* nomes dos campos */
                           primeiraLinha = false;
                        }

                        linhaArquivo = linhaArquivo.ExecutaReplaceIncluindoAspasDuplas("[COD_CIA_LEGADO]", "87");
                        linhaArquivo = linhaArquivo.ExecutaReplaceIncluindoAspasDuplas("[COD_CIA_CNPJ]", "87"); //Fixo
                        linhaArquivo = linhaArquivo.ExecutaReplaceIncluindoAspasDuplas("[DATA_PROCESSO_COREBI]", ""); //

                        objRegistrosArquivo.Add(linhaArquivo);

                        //Grava log de processamento do motivo de endosso
                        db.CognosLogArquivos.Add(new CognosLogArquivoPO
                        {
                           CodigoRef = 0,
                           StringRef = string.Format("CNPJ:{0} Razao Social:{1}", dado.strCnpj.RetornaCPFCNPJSomenteNumeros(), dado.strRazaoSocial.LimpaCaracteresEspeciais()),
                           Data = DateTime.Now,
                           NomeArquivo = filename,
                           TipoArquivo = (int)TipoArquivoCognos.MovimentacaoClientesGlobal
                        });

                        totalRegistros++;
                     }

                     totalRegistros = objRegistrosArquivo.Count;  // acerto no total de registros pós HashSet

                     //Grava log de criação de arquivo
                     db.IntegracaoDadosExecucaoArquivos.Add(new IntegracaoDadosExecucaoArquivoPO
                     {
                        IntegracaoDadosExecucaoID = execucao.ID,
                        NomeArquivo = filename,
                        NomeArquivoGerado = filename,
                        Status = (int)IntegracaoExecucaoArquivoStatus.Sucesso,
                        Resumo = String.Format(msgSucesso, totalRegistros)
                     });

                     //Gravação do arquivo na pasta específica da configuração
                     File.WriteAllLines(Path.Combine(configServico.DiretorioSaida, filename), objRegistrosArquivo, new UTF8Encoding(false));

                     db.SaveChanges();

                     //Faz o commit depois que todo o processamento for finalizado
                     dbContextTransaction.Commit();
                  }

                  //Monta o resumo do processamento para posteriormente ser gravado como log na tabela "tblIntegracaoDadosExecucao"
                  if (totalRegistros > 0)
                     execucao.Resumo = String.Format(msgSucesso, totalRegistros);
                  else
                     execucao.Resumo = msgSemRegistros;

                  execucao.Sucesso = true;
               }
               catch (Exception e)
               {
                  dbContextTransaction.Rollback();

                  execucao.Resumo = e.Message;
                  LoggerFile.Write(e);
               }
            }
         }
      }
   }
