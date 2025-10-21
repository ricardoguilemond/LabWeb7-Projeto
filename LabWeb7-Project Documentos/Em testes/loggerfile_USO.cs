// usando o logger
try {
...
}
catch (Exception e)
               {
                  //dbContextTransaction.Rollback();

                  execucao.Resumo = e.Message;
                  LoggerFile.Write(e);
               }