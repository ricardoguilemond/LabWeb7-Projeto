using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.Areas.ServicosDatabase
{
    public sealed class ConnectionManager : IDisposable
    {
        private static Db? _instance;
        private static readonly object _lock = new();
        private static bool _disposed = false;

        private ConnectionManager()
        { }

        public static Db GetInstance(string admStringConexao, IConnectionService connectionService, IEventLogHelper eventLogHelper)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ConnectionManager));
                }

                if (_instance == null)
                {
                    _instance = DatabaseContextFactory.CreateDbContextCliente(admStringConexao, connectionService, eventLogHelper);
                }

                return _instance;
            }
        }

        public static void DisposeInstance()
        {
            lock (_lock) // Garantir thread safety na liberação
            {
                if (_instance != null)
                {
                    _instance.Dispose(); // Libera os recursos de conexão
                    _instance = null;
                    _disposed = true;
                }
            }
        }

        public void Dispose()
        {
            DisposeInstance();
        }
    }
}