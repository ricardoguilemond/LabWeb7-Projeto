using Microsoft.AspNetCore.SignalR;

namespace LabWebMvc.MVC.Integracoes.Importacao
{
    public class ImportProgressHub : Hub
    {
        public async Task EnviarProgresso(string connectionId, object progresso)
        {
            await Clients.Client(connectionId).SendAsync("ReceberProgresso", progresso);
        }

        public async Task EnviarErro(string connectionId, object erro)
        {
            await Clients.Client(connectionId).SendAsync("ReceberErro", erro);
        }

        public async Task EnviarConclusao(string connectionId, object resultado)
        {
            await Clients.Client(connectionId).SendAsync("ReceberConclusao", resultado);
        }

        public async Task RequererDecisao(string connectionId, object decisao)
        {
            await Clients.Client(connectionId).SendAsync("RequererDecisao", decisao);
        }
    }
}
