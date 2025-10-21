using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace LabWebMvc.MVC.Areas.Strategy
{
    public class DeleteStrategy<T> : IDeleteStrategy<T> where T : class
    {
        private readonly DbContext _context;

        public DeleteStrategy(DbContext context)
        {
            _context = context;
        }

        //Deleta um registro pelo Id
        public async Task<bool> DeleteAsync(int id)
        {
            using (TransactionScope trans = new(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                bool erro = false;
                int change = 0;

                try
                {
                    T? entity = await _context.Set<T>().FindAsync(id);
                    if (entity != null)
                    {
                        _context.Remove(entity);
                        change = await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    erro = true;
                }

                trans.Complete();
                return !erro && change > 0;
            }
        }

        //Sobrescrito que deleta quando vier string
        public async Task<bool> DeleteAsync(string valor, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor) || string.IsNullOrWhiteSpace(campo))
            {
                //LogEventViewer("DefaultDeleteStrategy/DeleteAsync: O valor e o nome do campo não podem ser nulos ou vazios.", "Error");
                throw new ArgumentException("O valor e o nome do campo não podem ser nulos ou vazios.");
            }

            using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                bool erro = false;
                int change = 0;

                try
                {
                    // Obter a propriedade do tipo T dinamicamente
                    System.Reflection.PropertyInfo? propertyInfo = typeof(T).GetProperty(campo);
                    if (propertyInfo == null)
                        throw new InvalidOperationException($"A propriedade '{campo}' não foi encontrada no tipo '{typeof(T).Name}'.");

                    // Buscar entidades com base no valor e na propriedade dinâmica
                    List<T> entities = await _context.Set<T>()
                        .Where(e => (propertyInfo.GetValue(e) as string) == valor)
                        .ToListAsync();

                    if (entities.Count > 1)
                        throw new InvalidOperationException("Existe mais de um registro com esse identificador.");

                    T? entity = entities.FirstOrDefault();
                    if (entity != null)
                    {
                        _context.Remove(entity);
                        if (_context.Entry(entity).State == EntityState.Deleted)
                        {
                            change = await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    string mens = ex.Message;
                    //LogEventViewer("DefaultDeleteStrategy/DeleteAsync: Erro ao excluir registro pelo campo '" + campo + "' e valor '" + valor + "' ::: " + ex.Message, "wError");
                    erro = true;
                }

                trans.Complete();
                return !erro && change > 0;
            }
        }

        //Deleta uma lista de Ids
        public async Task<bool> DeleteManyAsync(List<int> ids)
        {
            using (TransactionScope trans = new(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                bool erro = false;
                int change = 0;

                try
                {
                    List<T> entities = await _context.Set<T>().Where(e => ids.Contains(EF.Property<int>(e, "Id"))).ToListAsync();
                    if (entities.Any())
                    {
                        _context.RemoveRange(entities);
                        change = await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    erro = true;
                }

                trans.Complete();
                return !erro && change > 0;
            }
        }
    }
}