using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using System.Runtime.Versioning;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class ReleaseController : Controller
    {
        [HttpGet]
        [Route("Release")]
        public IActionResult Release()
        {
            object[] colecao = { "Sobre" + Getbolinha + "Versão do .Net Framework", false };
            ViewBag.TextoMenu = colecao;

            if (OperatingSystem.IsWindows())
            {
                ViewBag.Mensagem = Get45PlusFromRegistry() ?? Array.Empty<string>();
            }
            else
            {
                ViewBag.Mensagem = new[] { "No momento só tenho como verificar a versão em sistemas Windows." };
            }

            return View();
        }

        /// <summary>
        [SupportedOSPlatform("windows")]
        private static string[]? Get45PlusFromRegistry()
        {
            // Checking the version using >= enables forward compatibility.
            static string CheckFor45PlusVersion(int releaseKey)
            {
                if (releaseKey >= 528040)
                    return "4.8 ou superior (ótimo!)";
                if (releaseKey >= 461808)
                    return "4.7.2 (satisfatório)";
                if (releaseKey >= 461308)
                    return "4.7.1 (satisfatório)";
                if (releaseKey >= 460798)
                    return "4.7 (satisfatório)";
                if (releaseKey >= 394802)
                    return "4.6.2 (satisfatório)";
                if (releaseKey >= 394254)
                    return "4.6.1 (satisfatório)";
                if (releaseKey >= 393295)
                    return "4.6 (satisfatório)";
                if (releaseKey >= 379893)
                    return "4.5.2 (satisfatório)";
                if (releaseKey >= 378675)
                    return "4.5.1 (satisfatório)";
                if (releaseKey >= 378389)
                    return "4.5 (satisfatório, versão mínima)";
                // This code should never execute. A non-null release key should mean
                // that 4.5 or later is installed.
                return " ::: Não há versão .Net Framework 4.8 ou superior detectada neste Sistema Operacional.";
            }

            if (!OperatingSystem.IsWindows())
                return new[] { "Este método só é compatível com Windows." };

            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
            RegistryKey registryKey = Environment.Is64BitOperatingSystem
                ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                : RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            using RegistryKey? ndpKey = registryKey.OpenSubKey(subkey);
            if (ndpKey != null)
            {
                int release = ndpKey.GetValue("Release") is int value ? value : 0;
                string mens = release == 0 ? "" : CheckFor45PlusVersion(release);

                if (release < 378389)
                {
                    return new[] { mens };
                }
                else if (release >= 528040)
                {
                    return new[] { $"Você está utilizando <b>.NET Framework {mens}</b>" };
                }
            }
            return new string[]
            {
                "Atenção: Este Sistema precisa do .Net Framework 4.8 ou superior ::: " +
                "E qual versão .Net eu estou utilizando? " +
                "Não foi possível detectar a versão do .Net Framework instalada neste Sistema Operacional."
            };
        }
    }
}