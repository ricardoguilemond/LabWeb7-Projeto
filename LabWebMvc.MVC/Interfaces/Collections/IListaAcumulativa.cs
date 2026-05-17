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
        // Feito pelo Qoder em 21/04/2026 — remove um item específico do cupom pelo Id do PlanoExames
        void RemoverItemCupom(string usuarioId, int planoExamesId);
        //..Qoder
    }

    public class ListaAcumulativa : IListaAcumulativa
    {
        // Singleton
        private static ListaAcumulativa? _instancia;
        public static ListaAcumulativa Instancia => _instancia ??= new ListaAcumulativa();

        // Dicionário thread-safe para armazenar dados por usuário
        private readonly ConcurrentDictionary<string, List<PlanoExames>> _dadosPorUsuario
            = new ConcurrentDictionary<string, List<PlanoExames>>();

        // Lock por usuário para proteger a List<> interna (não thread-safe por si só)
        private readonly ConcurrentDictionary<string, object> _locks
            = new ConcurrentDictionary<string, object>();

        private ListaAcumulativa() { }

        private object GetLock(string usuarioId)
            => _locks.GetOrAdd(usuarioId, _ => new object());

        //Feito pelo Kiro em 02/05/2026
        // Verificação de duplicatas: não adiciona PlanoExames com Id já existente na lista.
        public void AdicionarCupom(string usuarioId, IEnumerable<PlanoExames> dados)
        {
            lock (GetLock(usuarioId))
            {
                var lista = _dadosPorUsuario.GetOrAdd(usuarioId, _ => new List<PlanoExames>());
                foreach (var item in dados)
                {
                    if (!lista.Any(x => x.Id == item.Id))
                    {
                        lista.Add(item);
                    }
                }
            }
        }
        //..Kiro

        public List<PlanoExames> ObterCupom(string usuarioId)
        {
            lock (GetLock(usuarioId))
            {
                // Retorna cópia para evitar InvalidOperationException ao iterar fora do lock
                return _dadosPorUsuario.TryGetValue(usuarioId, out var lista)
                    ? new List<PlanoExames>(lista)
                    : new List<PlanoExames>();
            }
        }

        public void EsvaziarCupom(string usuarioId)
        {
            lock (GetLock(usuarioId))
            {
                _dadosPorUsuario.TryRemove(usuarioId, out _);
            }
        }

        // Feito pelo Qoder em 21/04/2026 — remove um item específico do cupom pelo Id do PlanoExames
        public void RemoverItemCupom(string usuarioId, int planoExamesId)
        {
            lock (GetLock(usuarioId))
            {
                if (_dadosPorUsuario.TryGetValue(usuarioId, out var lista))
                {
                    lista.RemoveAll(x => x.Id == planoExamesId);
                }
            }
        }
        //..Qoder
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