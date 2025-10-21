using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LabWebMvc.MVC.Interfaces.DAL
{
    public class PersistenceDAL<T> : IPersistenceDAL<T> where T : class
    {
        private readonly Db _db;

        public PersistenceDAL(Db db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db), "DbContext não pode ser nulo.");
        }

        /*
         *
         * PERSISTÊNCIAS DESTE REPOSITÓRIO (Data Access Layer)
         *
         */

        public bool RegistroExiste(params T[] items)
        {
            bool retorno = false;

            foreach (T item in items)
            {  /* caso encontre apenas um único true, então retornará true, mesmo havendo "n" false */
                retorno = _db.Entry(items).IsKeySet ? true : retorno;
            }

            return retorno;
        }

        public void Adiciona(params T[] items)
        {
            foreach (T item in items)
            {
                _db.Entry(item).State = EntityState.Added;
            }
            _db.SaveChanges();
        }

        public void Atualiza(params T[] items)
        {
            foreach (T item in items)
            {
                _db.Entry(item).State = EntityState.Modified;
            }
            _db.SaveChanges();
        }

        public void AdicionaOuAtualiza(params T[] items)
        {
            foreach (var item in items)
            {
                var entry = _db.Entry(item);

                if (entry.IsKeySet) // significa que a chave primária está preenchida
                {
                    // Tenta carregar a entidade do banco
                    var existing = _db.Set<T>().Find(entry.Property("Id").CurrentValue);

                    if (existing == null)
                    {
                        // Não existe no banco → adicionar
                        _db.Entry(item).State = EntityState.Added;
                    }
                    else
                    {
                        // Já existe → atualizar
                        _db.Entry(existing).CurrentValues.SetValues(item);
                    }
                }
                else
                {
                    // Sem chave definida → sempre adiciona
                    _db.Entry(item).State = EntityState.Added;
                }
            }
            _db.SaveChanges();
        }

        public void Remove(params T[] items)
        {
            foreach (T item in items)
            {
                _db.Entry(item).State = EntityState.Deleted;
            }
            _db.SaveChanges();
        }

        /* Pesquisa através de objeto(s), e trará uma lista */

        public virtual IList<T> Consulta(params Expression<Func<T, object>>[] navigationProperties)
        {
            List<T> list;
            IQueryable<T> dbQuery = _db.Set<T>();

            // aplicando carregamento antecipado!
            foreach (Expression<Func<T, object>> navigationProperty in navigationProperties)
                dbQuery = dbQuery.Include<T, object>(navigationProperty);

            list = dbQuery.AsNoTracking().ToList<T>();

            /* Utilizar o AsNoTracking significa que as entidades serão lidas da origem de dados,
             * mas não serão mantidas no contexto. Isto é bom, porque nada será armazenado em cache.
             * Fonte: https://imasters.com.br/back-end/nao-cometa-esses-erros-ao-utilizar-o-entity-framework
             */

            return list;
        }

        /* Pesquisa que retorna uma lista  */

        public virtual IList<T> Lista(Func<T, bool> where, params Expression<Func<T, object>>[] navigationProperties)
        {
            List<T> list;
            IQueryable<T> dbQuery = _db.Set<T>();

            // aplicar carregamento antecipado!
            foreach (Expression<Func<T, object>> navigationProperty in navigationProperties)
                dbQuery = dbQuery.Include<T, object>(navigationProperty);

            list = dbQuery.AsNoTracking().Where(where).ToList<T>();

            /* Utilizar o AsNoTracking significa que as entidades serão lidas da origem de dados,
             * mas não serão mantidas no contexto. Isto é bom, porque nada será armazenado em cache.
             * Fonte: https://imasters.com.br/back-end/nao-cometa-esses-erros-ao-utilizar-o-entity-framework
             */

            return list;
        }

        /* Listar APENAS UM ITEM */

        public virtual T ListaSimples(Func<T, bool> where, params Expression<Func<T, object>>[] navigationProperties)
        {
            T item;
            IQueryable<T> dbQuery = _db.Set<T>();

            // aplicar carregamento antecipado!
            foreach (Expression<Func<T, object>> navigationProperty in navigationProperties)
                dbQuery = dbQuery.Include<T, object>(navigationProperty);

            item = dbQuery.AsNoTracking().First(where);

            /* Utilizar o AsNoTracking significa que as entidades serão lidas da origem de dados,
             * mas não serão mantidas no contexto. Isto é bom, porque nada será armazenado em cache.
             * Fonte: https://imasters.com.br/back-end/nao-cometa-esses-erros-ao-utilizar-o-entity-framework
             */

            return item;
        }
    }
}