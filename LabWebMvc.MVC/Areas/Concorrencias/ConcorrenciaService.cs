using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static LabWebMvc.MVC.Areas.Utils.Utils;

namespace LabWebMvc.MVC.Areas.Concorrencias
{
    public class ConcorrenciaService : IConcorrenciaService
    {
        private readonly Db _db;
        private readonly IEventLogHelper _eventLog;

        public ConcorrenciaService(IConnectionService connectionService, IEventLogHelper eventLogHelper)
        {
            var optionsBuilder = new DbContextOptionsBuilder<Db>()
                .UseNpgsql(connectionService.GetConnectionString());

            _db = new Db(optionsBuilder.Options, connectionService, eventLogHelper);
            _eventLog = eventLogHelper;
        }

        // Verifica e atualiza a concorrência se necessário
        //tempoRetidoMinutos =  60 (1 hora)
        //                     120 (2 horas)
        //                    1440 (24 horas)
        public async Task<bool> ValidarOuAtualizarConcorrenciaAsync(string processo, int tempoRetidoMinutos = 10)
        {
            ControleConcorrencia? concorrencia = await _db.ControleConcorrencia.FirstOrDefaultAsync(c => c.Processo == processo);

            if (concorrencia != null)
            {
                // Se o registro tem mais de 10 minutos, atualizamos e permitimos prosseguir
                if (concorrencia.DataHora.AddMinutes(tempoRetidoMinutos) < DateTime.Now)
                {
                    concorrencia.DataHora = DateTime.Now;
                    _db.ControleConcorrencia.Update(concorrencia);
                    await _db.SaveChangesAsync();
                    return true; // Permite a continuação da operação
                }
                return false; // Bloqueia para não executar a operação!
            }

            // Se não existir concorrência, cria um novo registro
            ControleConcorrencia novoControle = new() { Processo = processo, DataHora = DateTime.Now };
            await _db.ControleConcorrencia.AddAsync(novoControle);
            await _db.SaveChangesAsync();

            return true; // Permite a continuação da operação
        }

        // Limpa o controle de concorrência após operação
        public async Task RemoverConcorrenciaAsync(string processo)
        {
            await using IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync();
            List<ControleConcorrencia> registros = await _db.ControleConcorrencia
                                    .Where(x => x.Processo == processo)
                                    .ToListAsync();

            _db.ControleConcorrencia.RemoveRange(registros);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        //Refine o Id de uma Tabela para continuar na sequência incremental da primary key, sem pular o Id
        //É extremamente anti performance o uso em código, mas pode ser bem utilizado em tabelas que sofrem muito pouca concorrência.
        //Se foi o último registro que foi excluído, então vamos recuperar o auto incremento, para evitar perder sequência Id.
        /*
         *  Se optar pela Trigger, dispense o uso deste método!
         *  Seria ideal colocar uma Trigger no Banco de Dados, pois isso evita possíveis concorrências (semáforos) com queda de performance por código:
         *
         *          CREATE TRIGGER ResetarAutoIncrementoClasseExames
         *          ON ClasseExames
         *          AFTER DELETE
         *          AS
         *          BEGIN
         *              DECLARE @UltimoId INT;
         *              SELECT @UltimoId = ISNULL(MAX(Id), 0) FROM ClasseExames;
         *
         *              IF @UltimoId > 0
         *                  DBCC CHECKIDENT ('ClasseExames', RESEED, @UltimoId);
         *              ELSE
         *                  DBCC CHECKIDENT ('ClasseExames', RESEED, 0);
         *          END;
         */

        public async Task RedefinirIncremento(string nomeTabela, Db db, IDbContextTransaction transaction)
        {
            nomeTabela = nomeTabela.Alltrim();

            try
            {
                string sql = $"SELECT TOP 1 Id FROM {nomeTabela} WITH (TABLOCK, HOLDLOCK)";
                int result = await db.Database.ExecuteSqlRawAsync(sql);

                // Buscar a entidade dentro do Db Context
                System.Reflection.PropertyInfo? dbSetProperty = db.GetType().GetProperties()
                    .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                         p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                         p.Name.Equals(nomeTabela, StringComparison.OrdinalIgnoreCase));

                if (dbSetProperty == null)
                {
                    _eventLog.LogEventViewer("Tabela '" + nomeTabela + "' não encontrada no contexto.", "wError");
                    throw new InvalidOperationException($"Tabela '{nomeTabela}' não encontrada no contexto.");
                }

                Type? entityType = dbSetProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                if (entityType == null)
                {
                    _eventLog.LogEventViewer("Não foi possível identificar o tipo da entidade para '" + nomeTabela + ".", "wError");
                    throw new InvalidOperationException($"Não foi possível identificar o tipo da entidade para '{nomeTabela}'.");
                }

                // Obter o DbSet correto
                object? dbSet = dbSetProperty.GetValue(db);
                if (dbSet == null)
                {
                    _eventLog.LogEventViewer("O DbSet para '" + nomeTabela + "' não pode ser acessado", "wError");
                    throw new InvalidOperationException($"O DbSet para '{nomeTabela}' não pode ser acessado.");
                }

                // Criar consulta filtrada para a tabela correta
                sql = $"SELECT TOP 1 * FROM {nomeTabela} ORDER BY Id DESC";

                System.Reflection.MethodInfo? fromSqlMethod = typeof(RelationalQueryableExtensions)
                    .GetMethods()
                    .Where(m => m.Name == "FromSqlRaw" && m.IsGenericMethod)
                    .FirstOrDefault(m => m.GetParameters().Length == 3)
                    ?.MakeGenericMethod(entityType);

                if (fromSqlMethod == null)
                {
                    _eventLog.LogEventViewer("O método fromSqlRaw correto não foi encontrado.", "wError");
                    throw new InvalidOperationException("O método FromSqlRaw correto não foi encontrado.");
                }

                object? queryable = fromSqlMethod.Invoke(null, new object[] { dbSet, sql, new object[] { } });

                if (queryable == null)
                {
                    _eventLog.LogEventViewer("Erro ao executar FromSqlRaw (queryable).", "wError");
                    throw new InvalidOperationException("Erro ao executar FromSqlRaw.");
                }

                System.Reflection.MethodInfo? firstOrDefaultMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .FirstOrDefault(m =>
                        m.Name == "FirstOrDefaultAsync" &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[1].ParameterType == typeof(CancellationToken))
                    ?.MakeGenericMethod(entityType);

                if (firstOrDefaultMethod == null)
                {
                    _eventLog.LogEventViewer("O método firstOrDefaultAsync correto não foi encontrado.", "wError");
                    throw new InvalidOperationException("O método FirstOrDefaultAsync correto não foi encontrado.");
                }
                /*
                 * "ultimoTask" recebe o código que foi gerado para uma instância do Task assíncrono. Cada tarefa assíncrona criada ou
                 * acionada, recebe um código Task para sua identificação. As tarefas são identificadas por um ´codigo para conseguirem
                 * ser executadas com todas as suas propriedades até que a tarefa seja cumprida.
                 * Mesmo que reexecutemos o mesmo método Task ára pegarmos a mesma informação ou executarmos o mesmo processo, esse
                 * código Task seja novo, pois cada processo recebe sua identificação Task.
                 */
                Task? ultimoTask = firstOrDefaultMethod.Invoke(null, new object[] { queryable, CancellationToken.None }) as Task;
                if (ultimoTask == null)
                {
                    _eventLog.LogEventViewer("Erro ao executar FirstOrDefaultAsync.", "wError");
                    throw new InvalidOperationException("Erro ao executar FirstOrDefaultAsync.");
                }
                await ultimoTask;

                object? ultimo = ultimoTask.GetType().GetProperty("Result")?.GetValue(ultimoTask);

                // Pegando o valor do Id corretamente após garantir a tabela correta
                if (ultimo != null)
                {
                    System.Reflection.PropertyInfo? idProperty = entityType.GetProperty("Id");
                    if (idProperty == null)
                    {
                        _eventLog.LogEventViewer("A entidade '" + nomeTabela + "' não possui um campo 'Id' para 'RedefinirIncremento'", "wError");
                        throw new InvalidOperationException($"A entidade '{nomeTabela}' não possui um campo 'Id'.");
                    }
                    int? ultimoId = idProperty?.GetValue(ultimo) as int?;

                    if (ultimoId == null)
                    {
                        _eventLog.LogEventViewer("O campo 'Id' não pôde ser obtido.", "wError");
                        throw new InvalidOperationException("O campo 'Id' não pode ser obtido.");
                    }

                    // Executar redefinição do incremento
                    sql = $"DBCC CHECKIDENT ('{nomeTabela}', RESEED, {ultimoId})";
                    await db.Database.ExecuteSqlRawAsync(sql);

                    _eventLog.LogEventViewer($"Redefiniu o 'Id' para a tabela: {nomeTabela} para o último Id: {ultimoId}", "wInfo");
                }
            }
            catch (Exception ex)
            {
                _eventLog.LogEventViewer($"Falhou a redefinição de Id para a tabela {nomeTabela}.\n\nMessage: {ex.Message}", "wWarning");
            }
        }
    }//Fim
}