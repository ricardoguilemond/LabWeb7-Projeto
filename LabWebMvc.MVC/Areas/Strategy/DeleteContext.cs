using LabWebMvc.MVC.Mensagens;
using Microsoft.AspNetCore.Mvc;

namespace LabWebMvc.MVC.Areas.Strategy
{
    public class DeleteContext<T> where T : class
    {
        private readonly IDeleteStrategy<T> _deleteStrategy;

        public DeleteContext(IDeleteStrategy<T> deleteStrategy)
        {
            _deleteStrategy = deleteStrategy;
        }

        public async Task<JsonResult> DeleteRecordAsync(int id, string? nomeTabela = "Tabela não rastreada")
        {
            bool success = await _deleteStrategy.DeleteAsync(id);

            var res = new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Registro foi excluído", action = "", sucesso = true };

            if (!success)
            {
                var eventLog2 = new ExtensionsMethods.EventViewerHelper.EventLogHelper();
                eventLog2.LogEventViewer("Tabela: " + nomeTabela + ", Registro id: " + id.ToString() + " não foi excluído", "wWarning");
                res = new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi excluído", action = "", sucesso = false };
            }
            return new JsonResult(res);
        }

        public async Task<JsonResult> DeleteRecordAsync(string valor, string campo, string? nomeTabela = "Tabela não rastreada")
        {
            bool success = await _deleteStrategy.DeleteAsync(valor, campo);

            var res = new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Registro foi excluído", action = "", sucesso = true };

            if (!success)
            {
                var eventLog2 = new ExtensionsMethods.EventViewerHelper.EventLogHelper();
                eventLog2.LogEventViewer("Tabela: " + nomeTabela + ", Registro valor: " + valor + " não foi excluído", "wWarning");
                res = new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi excluído", action = "", sucesso = false };
            }
            return new JsonResult(res);
        }

        public async Task<JsonResult> DeleteRecordAsync(List<int> ids, string? nomeTabela = "Tabela não rastreada")
        {
            bool success = await _deleteStrategy.DeleteManyAsync(ids);

            string lista = string.Empty;
            foreach (int item in ids)
            {
                lista += item.ToString() + ", ";
            }
            var res = new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Registros foram excluídos", action = "", sucesso = true };

            if (!success)
            {
                var eventLog2 = new ExtensionsMethods.EventViewerHelper.EventLogHelper();
                eventLog2.LogEventViewer("Tabela: " + nomeTabela + ", A lista de registros com Ids: " + lista + " não foi excluída", "wWarning");
                res = new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registros não foram excluídos", action = "", sucesso = false };
            }
            return new JsonResult(res);
        }
    }
}