using BLL;
using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.Controllers;
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
        //private object _tempoService;
        private ITempoServidorService _tempoService;

        public ConcorrenciaService(IConnectionService connectionService, IEventLogHelper eventLogHelper, ITempoServidorService tempoService)
        {
            var optionsBuilder = new DbContextOptionsBuilder<Db>()
                .UseNpgsql(connectionService.GetConnectionString());

            _db = new Db(optionsBuilder.Options, connectionService, eventLogHelper);
            _eventLog = eventLogHelper;
            _tempoService = tempoService;
        }

        // Verifica e atualiza a concorrência se necessário
        //tempoRetidoMinutos =  60 (1 hora)
        //                     120 (2 horas)
        //                    1440 (24 horas)
        public async Task<bool> ValidarOuAtualizarConcorrenciaAsync(string processo, int tempoRetidoMinutos = 10)
        {
            ControleConcorrencia? concorrencia = await _db.ControleConcorrencia.FirstOrDefaultAsync(c => c.Processo == processo);

            var dataHoraServidor = await _tempoService.ObterDataHoraServidorAsync();

            if (concorrencia != null)
            {
                // Se o registro tem mais de 10 minutos, atualizamos e permitimos prosseguir
                if (concorrencia.DataHora.AddMinutes(tempoRetidoMinutos) < dataHoraServidor.Value)
                {
                    concorrencia.DataHora = dataHoraServidor.Value;
                    _db.ControleConcorrencia.Update(concorrencia);
                    await _db.SaveChangesAsync();
                    return true; // Permite a continuação da operação
                }
                return false; // Bloqueia para não executar a operação!
            }

            // Se não existir concorrência, cria um novo registro
            ControleConcorrencia novoControle = new() { Processo = processo, DataHora = dataHoraServidor.Value };
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

        public async Task RedefinirIncrementoPostgres(string nomeTabela, Db db)
        {
            nomeTabela = nomeTabela.Trim();

            try
            {
                var dbSetProperty = db.GetType().GetProperties()
                    .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                         p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                         p.Name.Equals(nomeTabela, StringComparison.OrdinalIgnoreCase));

                if (dbSetProperty == null)
                    throw new InvalidOperationException($"Tabela '{nomeTabela}' não encontrada no contexto.");

                var entityType = dbSetProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                if (entityType == null)
                    throw new InvalidOperationException($"Tipo da entidade '{nomeTabela}' não identificado.");

                var dbSet = dbSetProperty.GetValue(db) as IQueryable;
                if (dbSet == null)
                    throw new InvalidOperationException($"DbSet para '{nomeTabela}' não acessível.");

                var query = dbSet.Cast<object>().OrderByDescending(e => EF.Property<int>(e, "Id"));
                var ultimo = await query.FirstOrDefaultAsync();

                int reseedValue = ultimo != null ? EF.Property<int>(ultimo, "Id") : 0;

                string sql = $"SELECT SETVAL(pg_get_serial_sequence('{nomeTabela}', 'Id'), {reseedValue})";
                await db.Database.ExecuteSqlRawAsync(sql);

                _eventLog.LogEventViewer($"Redefiniu o 'Id' da tabela '{nomeTabela}' para {reseedValue}.", "wInfo");
            }
            catch (Exception ex)
            {
                _eventLog.LogEventViewer($"Falha ao redefinir Id da tabela '{nomeTabela}': {ex.Message}", "wWarning");
            }
        }


        public async Task RedefinirIncrementoPostgres2(string nomeTabela, Db db)
        {
            nomeTabela = nomeTabela.Trim();

            try
            {
                // Localiza o DbSet correspondente à tabela
                var dbSetProperty = db.GetType().GetProperties()
                    .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                         p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                         p.Name.Equals(nomeTabela, StringComparison.OrdinalIgnoreCase));

                if (dbSetProperty == null)
                    throw new InvalidOperationException($"Tabela '{nomeTabela}' não encontrada no contexto.");

                var entityType = dbSetProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                if (entityType == null)
                    throw new InvalidOperationException($"Tipo da entidade '{nomeTabela}' não identificado.");

                var dbSet = dbSetProperty.GetValue(db) as IQueryable;
                if (dbSet == null)
                    throw new InvalidOperationException($"DbSet para '{nomeTabela}' não acessível.");

                // Obtém o último Id existente
                var query = dbSet.Cast<object>().OrderByDescending(e => EF.Property<int>(e, "Id"));
                var ultimo = await query.FirstOrDefaultAsync();

                int reseedValue = ultimo != null ? EF.Property<int>(ultimo, "Id") : 0;

                // Comando PostgreSQL para redefinir o valor da sequência
                string sql = $"SELECT SETVAL(pg_get_serial_sequence('{nomeTabela}', 'Id'), {reseedValue})";
                await db.Database.ExecuteSqlRawAsync(sql);

                _eventLog.LogEventViewer($"Redefiniu o 'Id' da tabela '{nomeTabela}' para {reseedValue}.", "wInfo");
            }
            catch (Exception ex)
            {
                _eventLog.LogEventViewer($"Falhou a redefinição de Id para a tabela '{nomeTabela}': {ex.Message}", "wWarning");
            }
        }


    }//Fim
}