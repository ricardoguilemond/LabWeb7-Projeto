using System.Linq.Expressions;

namespace LabWebMvc.MVC.Interfaces.DAL
{
    public interface IPersistenceDAL<T> where T : class
    {
        bool RegistroExiste(params T[] items);

        void Adiciona(params T[] items);

        void Atualiza(params T[] items);

        void Remove(params T[] items);

        IList<T> Consulta(params Expression<Func<T, object>>[] navigationProperties);

        IList<T> Lista(Func<T, bool> where, params Expression<Func<T, object>>[] navigationProperties);

        T ListaSimples(Func<T, bool> where, params Expression<Func<T, object>>[] navigationProperties);
    }
}