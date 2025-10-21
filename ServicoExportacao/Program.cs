using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ServicoExportacao;

/*
    1) Para publicar o serviço no Windows:

       1.1) (Estando na pasta do serviço, na própria aplicação (Terminal), vamos criar o Release: F:\Projetos dotNet\Web-Project\LabWeb7-Project\ServicoExportacao>)

            >> dotnet publish --configuration Release

            Automaticamente a linha de comando acima criará uma pasta "Release" padrão

       1.2) ATENÇÃO: O serviço com suas DLLS estarão disponíveis agora em PUBLISH:

            >> F:\Projetos dotNet\Web-Project\LabWeb7-Project\ServicoExportacao\bin\Release\net7.0\publish

            O serviço pode ser criado pela pasta publish, OU copiar seu conteúdo para outra pasta e criar o serviço de lá.
            No caso eu prefiro sempre levar o publish para a pasta "C:\Servicos\nome do servico\" e criar o serviço aqui!

       1.3) (Estando agora na pasta desejada com o serviço) Para criar o serviço no Windows, proceder:

            >> sc create Lab7ServiceIntegracao binpath="C:\Servicos\ServicoExportacao\ServicoExportacao.exe"

            O resultado será esta mensagem: [SC] CreateService ÊXITO

            Para desinstalar o serviço, faça STOP prineiro no Windows Service e depois aplique:

            >> sc delete Lab7ServiceIntegracao

            O resultado será esta mensagem: [SC] DeleteService ÊXITO
 */

public class Program
{
    public static void Main(string[] args)
    {
        /*
            Observações:
            Embora o "Startup" não seja mais utilizado no Net6 e Net7,
            eu criei o "Startup" e é lá que coloquei todas as configurações para a conexão.
            .UseStartup<Startup>() ::: Aqui eu encapsulo e chamo o "Startup" para chamar a aplicação.
         ************************************************************************************************************************
            IMPORTANTE: ESTOU DECLARANDO AS CHAMADAS DOS SERVIÇOS DE INTEGRAÇÃO IMPORTAÇÃO/EXPORTAÇÃO NO STARTUP !!!!!!
         ************************************************************************************************************************
         */
        try
        {
            CreateWebHost(args).RunAsync();
        }
        catch (Exception ex)
        {
            string mens = "Reveja a configuração do parâmetro Default: Debug, em 'appsettings.json' ::: " + ex.Message;
            Console.WriteLine(mens);
        }
    }

    public static IWebHostBuilder CreateWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>();
}