using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LabWebMvc.MVC.UtilHelper
{
    public static class TrataExcecoes
    {
        public static void TrataExceptionViewer(Exception ex, Db? db = null)
        {
            var eventLog2 = new ExtensionsMethods.EventViewerHelper.EventLogHelper();

            StringBuilder erro = new();

            if (db != null)
            {
                Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry? entry = db.ChangeTracker.Entries().LastOrDefault();
                string nomeTabela = entry != null
                    ? db.Model.FindEntityType(entry.Entity.GetType())?.GetTableName() ?? "Tabela desconhecida"
                    : "Nenhuma entidade rastreada";

                string chavePrimaria = "ID desconhecido";
                if (entry != null)
                {
                    Microsoft.EntityFrameworkCore.Metadata.IProperty? chave = db.Model.FindEntityType(entry.Entity.GetType())?.FindPrimaryKey()?.Properties.FirstOrDefault();
                    string? id = chave != null ? entry.Property(chave.Name).CurrentValue?.ToString() : null;
                    chavePrimaria = id ?? chavePrimaria;
                }

                erro.AppendLine($"Erro na persistência [{DateTime.Now}]");
                erro.AppendLine($"Entidade: {nomeTabela}, ID: {chavePrimaria}");
            }
            else
            {
                erro.AppendLine($"Erro geral [{DateTime.Now}]");
            }

            erro.AppendLine($"Mensagem: {ex.Message}");
            erro.AppendLine($"InnerException: {ex.InnerException?.Message ?? "Nenhuma"}");
            erro.AppendLine($"StackTrace: {ex.StackTrace}");

            eventLog2.LogEventViewer(erro.ToString(), "wError");
        }
    }
}