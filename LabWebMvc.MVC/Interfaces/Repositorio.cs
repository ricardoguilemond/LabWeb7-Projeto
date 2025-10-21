using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LabWebMvc.MVC.Interfaces
{
    public class Repositorio<T> : IRepositorio<T> where T : class
    {
        private readonly Db _context;
        private readonly DbSet<T> _dbSet;

        public Repositorio(Db context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        /*
         * PRIMEIRA CHAMADA DA Implementação
         */

        public IEnumerable<T> Listar()
        {
            return _dbSet.ToList();
        }

        public T Consultar(object id)
        {
            return _dbSet.Find(id)!;
        }

        public void Adicionar(T obj)
        {
            _dbSet.Add(obj);
        }

        public void Atualizar(T obj)
        {
            _dbSet.Attach(obj);
            _context.Entry(obj).State = EntityState.Modified;
        }

        public void Delete(params object[] id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
                Delete(entity);
        }

        public void Delete(T entityToDelete)
        {
            if (_context.Entry(entityToDelete).State == EntityState.Detached)
                _dbSet.Attach(entityToDelete);

            _dbSet.Remove(entityToDelete);
        }

        public void Excluir(object id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
                Delete(entity);
        }

        public void Salvar()
        {
            _context.SaveChanges();
        }

        /*
         * SEGUNDA CHAMADA DA Implementação
         */

        public IQueryable<T> ListarQuery()
        {
            return _dbSet.AsQueryable();
        }

        public IQueryable<T> ConsultarQuery(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate);
        }

        public T ConsultarArray(params object[] keys)
        {
            return _dbSet.Find(keys)!;
        }

        public void AdicionarQuery(T obj)
        {
            _dbSet.Add(obj);
        }

        public void AtualizarQuery(T obj)
        {
            _dbSet.Update(obj);
        }

        public void ExcluirDefault(Expression<Func<T, bool>> predicate)
        {
            var entities = _dbSet.Where(predicate).ToList();
            _dbSet.RemoveRange(entities);
        }

        public bool Existe(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Any(predicate);
        }

        /*
         * Listando pela Classe principal e classes relacionadas
         */

        public List<T> SelectIncludes(Expression<Func<T, bool>> where, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.AsQueryable();

            foreach (var include in includes)
                query = query.Include(include);

            return query.Where(where).ToList();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}