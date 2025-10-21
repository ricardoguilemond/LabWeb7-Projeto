using System.Runtime.InteropServices;

namespace LabWebMvc.MVC
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IHostBuilder builder = CreateHostBuilder(args);

            // Condição para executar como serviço no Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                builder = builder.UseWindowsService(); // Requer Microsoft.Extensions.Hosting.WindowsServices
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // No Linux, você pode usar logs no console, systemd, etc.
                Console.WriteLine("Executando como serviço no Linux...");
            }

            builder.Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            string osSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
                       : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
                       : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
                       : "Default";

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{osSuffix}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
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

            >> sc create Lab7ServiceIntegracao binpath="C:\Servicos\WindowsService\WindowsService.exe"

            O resultado será esta mensagem: [SC] CreateService ÊXITO

            Para colocar uma descrição no serviço:
            >> sc description Lab7ServiceIntegracao "Serviço de integração de dados do sistema LabWeb7"

            Para desinstalar o serviço, faça STOP prineiro no Windows Service e depois aplique:

            >> sc delete Lab7ServiceIntegracao

            O resultado será esta mensagem: [SC] DeleteService ÊXITO
 */

/*
 * IMPORTANTE:
 * PARA PERMITIR QUE O EVENT VIEWER DO WINDOWS PASSE A REGISTRAR OS EVENTOS DO SERVIÇO, É PRECISO REGISTRAR O EVENTO NO WINDOWS VIA POWER SHELL:
 *
 * PARA QUE ESTA ROTINA DE REGISTROS NO EVENT VIEWER DO WINDOWS FUNCIONE, É PRECISO REGISTRAR ANTES, UMA ÚNICA VEZ O COMANDO ABAIXO NO POWER SHELL COMO ADMINiSTRADOR:
 *      New-EventLog -LogName "LabWebMvcLog" -Source "LabWebMvc"
 *
 * PARA DESFAZER O REGISTRO NO POWER SHELL:
 *      Remove-EventLog -LogName "LabWebMvcLog"
 *
 * Em seguida, fechar e reabrir o EventViewer do Windows.
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

     ---------------------------------------------------
     AVISOS:
     ---------------------------------------------------
     1) Quando for publicar o sistema, não esquecer de descomentar em HomeController a linha que contém o "validacaoReCaptcha" do Google.

 */