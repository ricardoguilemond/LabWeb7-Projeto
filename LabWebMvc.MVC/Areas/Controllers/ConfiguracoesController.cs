using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class ConfiguracoesController : BaseController
    {
        public ConfiguracoesController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem)
        {
        }

        // GET: Configuracoes
        public async Task<IActionResult> Index()
        {
            var config = await _db.Configuracoes.FirstOrDefaultAsync();

            var vm = new vmConfiguracoes
            {
                ImpressoraCupom1 = config?.ImpressoraCupom1,
                ImpressoraCupom2 = config?.ImpressoraCupom2,
                ImpressoraCupom3 = config?.ImpressoraCupom3,
                UsarImpressoraCupom1 = config?.UsarImpressoraCupom1 ?? 0,
                UsarImpressoraCupom2 = config?.UsarImpressoraCupom2 ?? 0,
                UsarImpressoraCupom3 = config?.UsarImpressoraCupom3 ?? 0,
                FonteNome = config?.FonteNome,
                FonteTamanho = config?.FonteTamanho ?? 0,
                LarguraPapel = config?.LarguraPapel ?? 0,
                AlturaPapel = config?.AlturaPapel ?? 0,
                MargemEsquerda = config?.MargemEsquerda ?? 0,
                MargemDireita = config?.MargemDireita ?? 0, 
                MargemSuperior = config?.MargemSuperior ?? 0, 
                MargemInferior = config?.MargemInferior ?? 0
            };
            return View(vm);
        }

        // POST: Configuracoes
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Configuracoes/Index")]
        public async Task<IActionResult> Index(vmConfiguracoes vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var config = await _db.Configuracoes.FirstOrDefaultAsync();

            if (config == null)
            {
                // Inserção inicial
                var novaConfig = new Configuracoes
                {
                    Id = 1, // chave fixa
                    ImpressoraCupom1 = vm.ImpressoraCupom1,
                    ImpressoraCupom2 = vm.ImpressoraCupom2,
                    ImpressoraCupom3 = vm.ImpressoraCupom3,
                    UsarImpressoraCupom1 = vm.UsarImpressoraCupom1,
                    UsarImpressoraCupom2 = vm.UsarImpressoraCupom2,
                    UsarImpressoraCupom3 = vm.UsarImpressoraCupom3,

                    FonteNome = vm.FonteNome ?? "Courier New",
                    FonteTamanho = vm.FonteTamanho,
                    LarguraPapel = vm.LarguraPapel,
                    AlturaPapel = vm.AlturaPapel,
                    MargemEsquerda = vm.MargemEsquerda,
                    MargemDireita = vm.MargemDireita,
                    MargemSuperior = vm.MargemSuperior,
                    MargemInferior = vm.MargemInferior
                };

                _db.Configuracoes.Add(novaConfig);
            }
            else
            {
                // Atualização
                config.ImpressoraCupom1 = vm.ImpressoraCupom1;
                config.ImpressoraCupom2 = vm.ImpressoraCupom2;
                config.ImpressoraCupom3 = vm.ImpressoraCupom3;
                config.UsarImpressoraCupom1 = vm.UsarImpressoraCupom1;
                config.UsarImpressoraCupom2 = vm.UsarImpressoraCupom2;
                config.UsarImpressoraCupom3 = vm.UsarImpressoraCupom3;

                config.FonteNome = vm.FonteNome ?? "Courier New";
                config.FonteTamanho = vm.FonteTamanho;
                config.LarguraPapel = vm.LarguraPapel;
                config.AlturaPapel = vm.AlturaPapel;
                config.MargemEsquerda = vm.MargemEsquerda;
                config.MargemDireita = vm.MargemDireita;
                config.MargemSuperior = vm.MargemSuperior;
                config.MargemInferior = vm.MargemInferior;
            }

            await _db.SaveChangesAsync();

            ViewBag.TextoMenu = new object[] { "Configurações", false };

            return Json(new
            {
                sucesso = true,
                titulo = "Sucesso",
                mensagem = "Configurações salvas com sucesso!",
                action = Url.Action("Index")
            });
        }


    }
}
