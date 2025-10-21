using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class MenuController : Controller
    {
        private readonly HttpContext _httpContext;
        private readonly Db _db;
        private readonly IValidadorDeSessao _validador;

        public MenuController(HttpContext httpContext, Db db, IValidadorDeSessao validador)
        {
            _httpContext = httpContext;
            _db = db;
            _validador = validador;
        }

        public IActionResult Menu()
        {
            if (_validador.SessaoValida())
            {
                ViewBag.Menu = MenuLista();
                return View();
            }
            else
                return Json(new { titulo = MensagensError_pt_BR.ErroPagina, mensagem = "A sessão não foi validada", action = "", sucesso = false });
        }

        /*
        * Níveis das opções do Menu:
        * 1.22.33 (Principal, Item, Subitem)
        * 1.00.00
        * 1.01.01
        * 1.01.02
        */

        public string[,] MenuLista()
        {
            /* Estrutura do Menu Lista:
             * Principal --> Item --> SubItem --> Controller --> Action --> Parâmetros
             *
             * Quando não houver Subitem, então o Action e o Controller pertencem ao Item.
             * Quando houver Subitem, o Action e o Controller pertencem a ele SOMENTE.
             *
             */
            return new[,]
            {
            { "Cadastro", "Pacientes", "", "", "", "" },
            { "Cadastro", "Médicos", "", "", "", "" },
            { "Cadastro", "Instituições", "", "", "", "" },
            { "Cadastro", "Postos", "", "", "", "" },
            { "Controle de Acesso", "Criar Usuário", "", "Senhas", "Senhas", "" },
            { "Sobre", "Privacidade" , "", "Home", "Privacy", "" },
            { "Sobre", "Nosso Sistema" , "", "Home", "NossoSistema", "" },
            { "Sobre", "Versão .Net Framework" , "", "Release", "Release", "" }
         };
        }
    }
}