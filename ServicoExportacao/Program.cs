using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ServicoExportacao;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            string mens = "Reveja a configuração do parâmetro Default: Debug, em 'appsettings.json' ::: " + ex.Message;
            Console.WriteLine(mens);
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}