using System.Linq.Expressions;

namespace LabWebMvc.MVC.Interfaces
{
    public interface IRepositorio<T> : IDisposable where T : class
    {
        /*
         * OPERAÇÕES BÁSICAS (Sincronas)
         */

        IEnumerable<T> Listar();

        T Consultar(object id);

        void Adicionar(T entity);

        void Atualizar(T entity);

        void Delete(params object[] id);

        void Delete(T entity);

        void Excluir(object id);

        void Salvar();

        /*
         * OPERAÇÕES COM EXPRESSÕES E QUERIES
         */

        IQueryable<T> ListarQuery();

        IQueryable<T> ConsultarQuery(Expression<Func<T, bool>> predicate);

        T ConsultarArray(params object[] keys);

        void AdicionarQuery(T entity);

        void AtualizarQuery(T entity);

        void ExcluirDefault(Expression<Func<T, bool>> predicate);

        bool Existe(Expression<Func<T, bool>> predicate);

        /*
         * INCLUDES (Carregar entidades relacionadas)
         */

        List<T> SelectIncludes(Expression<Func<T, bool>> where, params Expression<Func<T, object>>[] includes);
    }
}