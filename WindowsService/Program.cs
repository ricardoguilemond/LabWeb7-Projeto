using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using System.ServiceProcess;
using WindowsService;

internal class Program
{
    private static void Main(string[] args)
    {
        if (OperatingSystem.IsWindows())
        {
            // Instancia manual das dependências
            IConnectionService connectionService = new ConnectionService(); // sua implementação real
            IEventLogHelper eventLogHelper = new EventLogHelper();          // sua implementação real
            IDbFactory dbFactory = new DbFactory(connectionService, eventLogHelper);

            using (FileWriteService service = new FileWriteService(dbFactory))
            {
                if (!Environment.UserInteractive)
                {
                    ServiceBase.Run(service); // Executa como serviço no Windows
                }
                else
                {
                    service.OnDebug(args);   // Executa como aplicação interativa para debug
                }
            }
        }
        else
        {
            Console.WriteLine("Este serviço só pode ser executado no Windows.");
        }
    }
}

/*
    1) Para publicar o serviço no Windows:

       1.1) (Estando na pasta do serviço, na própria aplicação (Terminal), vamos criar o Release: F:\Projetos dotNet\Web-Project\LabWeb7-Project\WindowsService>)

            >> dotnet publish --configuration Release

            Automaticamente a linha de comando acima criará uma pasta "Release" padrão

       1.2) ATENÇÃO: O serviço com suas DLLS estarão disponíveis agora em PUBLISH:

            >> F:\Projetos dotNet\Web-Project\LabWeb7-Project\WindowsService\bin\Release\net7.0\publish

            O serviço pode ser criado pela pasta publish, OU copiar seu conteúdo para outra pasta e criar o serviço de lá.
            No caso eu prefiro sempre levar o publish para a pasta "C:\Servicos\nome do servico\" e criar o serviço aqui!

       1.3) (Estando agora na pasta desejada com o serviço) Para criar o serviço no Windows, proceder:

            OBS: O parâmetro 1 passado como argumento para 1 minuto de pausa para cada evento 1..n.

            >> sc create Lab7ServiceIntegracao binpath="C:\Servicos\WindowsService\WindowsService.exe"

            O resultado será esta mensagem: [SC] CreateService ÊXITO

            Para colocar uma descrição no serviço:
            >> sc description Lab7ServiceIntegracao "Serviço de integração de dados do sistema LabWeb7"

            Para desinstalar o serviço, faça STOP prineiro no Windows Service e depois aplique:

            >> sc delete Lab7ServiceIntegracao

            O resultado será esta mensagem: [SC] DeleteService ÊXITO
 */
/*
     PARA SABER EM QUE PORTA DETERMINADO SERVIÇO ESTÁ RODANDO:
     //1433 deveria ser a porta padrão do MSSQL, porém, no meu computador por algum motivo ficou na porta 1434
     >> netstat -aon | findstr 'porta'
     >> netstat -aon | findstr 1433
     >> netstat -aon | findstr 1434

     O resultado do PID acima (LISTENING) aplica-se no comando abaixo e saberemnos qual programa usa a porta:
     >> Tasklist | findstr 'PID'
     >> Tasklist | findstr 5092

 */