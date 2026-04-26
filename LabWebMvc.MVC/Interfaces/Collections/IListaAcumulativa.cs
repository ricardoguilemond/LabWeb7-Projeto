using LabWebMvc.MVC.Models;
using System.Collections.Concurrent;

namespace LabWebMvc.MVC.Interfaces.Collections
{
    /*
         EXEMPLO DE USO:
         IListaAcumulativa acumulador = ListaAcumulativa.Instancia;

          //Adicionando dados
          acumulador.AdicionarDadosCupom(dadosLista);
          acumulador.AdicionarDadosCupom(outraLista);

          //Obtendo dados acumulados
          var listaAcumulada = acumulador.ObterDadosCupom();

          //Pode montar um foreach para a listaAcumulada.... ou qualquer outra forma para exibição.

          //No final ou quando quiser pode esvaziar a lista
          acumulador.EsvaziarListaCupom();   //esvazia a lista
          var listaAcumulada = acumulador.ObterDadosCupom();   //em seguida obtem a lista vazia

     */

    public interface IListaAcumulativa
    {
        void AdicionarCupom(string usuarioId, IEnumerable<PlanoExames> dados);
        List<PlanoExames> ObterCupom(string usuarioId);
        void EsvaziarCupom(string usuarioId);
    }

    public class ListaAcumulativa : IListaAcumulativa
    {
        // Singleton
        private static ListaAcumulativa? _instancia;
        public static ListaAcumulativa Instancia => _instancia ??= new ListaAcumulativa();

        // Dicionário thread-safe para armazenar dados por usuário
        private readonly ConcurrentDictionary<string, List<PlanoExames>> _dadosPorUsuario
            = new ConcurrentDictionary<string, List<PlanoExames>>();

        private ListaAcumulativa() { }

        public void AdicionarCupom(string usuarioId, IEnumerable<PlanoExames> dados)
        {
            var lista = _dadosPorUsuario.GetOrAdd(usuarioId, new List<PlanoExames>());
            lista.AddRange(dados);
        }

        public List<PlanoExames> ObterCupom(string usuarioId)
        {
            return _dadosPorUsuario.TryGetValue(usuarioId, out var lista)
                ? lista
                : new List<PlanoExames>();
        }

        public void EsvaziarCupom(string usuarioId)
        {
            _dadosPorUsuario.TryRemove(usuarioId, out _);
        }
    }

    //public class ListaAcumulativa : IListaAcumulativa
    //{
    //    private static ListaAcumulativa? _instancia;
    //    private ICollection<PlanoExames> listaAcumula;

    //    private ListaAcumulativa()
    //    {
    //        listaAcumula = [];
    //    }

    //    public static ListaAcumulativa Instancia
    //    {
    //        get
    //        {
    //            if (_instancia == null)
    //            {
    //                _instancia = new ListaAcumulativa();
    //            }
    //            return _instancia;
    //        }
    //    }

    //    public void AdicionarCupom(ICollection<PlanoExames> novaLista)
    //    {
    //        foreach (PlanoExames item in novaLista)
    //        {
    //            if (!listaAcumula.Any(x => x.Id == item.Id))   //Para evitar que adicione itens duplicados, mesmo usando HashSet.
    //            {
    //                listaAcumula.Add(item);
    //            }
    //        }
    //    }

    //    public ICollection<PlanoExames> ObterDadosCupom()
    //    {
    //        return new HashSet<PlanoExames>(listaAcumula);
    //    }

    //    public void EsvaziarListaCupom()
    //    {
    //        listaAcumula.Clear();
    //    }
    //}
}