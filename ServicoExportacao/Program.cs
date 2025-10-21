using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ServicoExportacao;

/*
    1) Para publicar o servi�o no Windows:

       1.1) (Estando na pasta do servi�o, na pr�pria aplica��o (Terminal), vamos criar o Release: F:\Projetos dotNet\Web-Project\LabWeb7-Project\ServicoExportacao>)

            >> dotnet publish --configuration Release

            Automaticamente a linha de comando acima criar� uma pasta "Release" padr�o

       1.2) ATEN��O: O servi�o com suas DLLS estar�o dispon�veis agora em PUBLISH:

            >> F:\Projetos dotNet\Web-Project\LabWeb7-Project\ServicoExportacao\bin\Release\net7.0\publish

            O servi�o pode ser criado pela pasta publish, OU copiar seu conte�do para outra pasta e criar o servi�o de l�.
            No caso eu prefiro sempre levar o publish para a pasta "C:\Servicos\nome do servico\" e criar o servi�o aqui!

       1.3) (Estando agora na pasta desejada com o servi�o) Para criar o servi�o no Windows, proceder:

            >> sc create Lab7ServiceIntegracao binpath="C:\Servicos\ServicoExportacao\ServicoExportacao.exe"

            O resultado ser� esta mensagem: [SC] CreateService �XITO

            Para desinstalar o servi�o, fa�a STOP prineiro no Windows Service e depois aplique:

            >> sc delete Lab7ServiceIntegracao

            O resultado ser� esta mensagem: [SC] DeleteService �XITO
 */

public class Program
{
    public static void Main(string[] args)
    {
        /*
            Observa��es:
            Embora o "Startup" n�o seja mais utilizado no Net6 e Net7,
            eu criei o "Startup" e � l� que coloquei todas as configura��es para a conex�o.
            .UseStartup<Startup>() ::: Aqui eu encapsulo e chamo o "Startup" para chamar a aplica��o.
         ************************************************************************************************************************
            IMPORTANTE: ESTOU DECLARANDO AS CHAMADAS DOS SERVI�OS DE INTEGRA��O IMPORTA��O/EXPORTA��O NO STARTUP !!!!!!
         ************************************************************************************************************************
         */
        try
        {
            CreateWebHost(args).RunAsync();
        }
        catch (Exception ex)
        {
            string mens = "Reveja a configura��o do par�metro Default: Debug, em 'appsettings.json' ::: " + ex.Message;
            Console.WriteLine(mens);
        }
    }

    public static IWebHostBuilder CreateWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>();
}