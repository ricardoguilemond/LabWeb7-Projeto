using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ExpressionCombiner;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Strategy;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Transactions;
using static BLL.UtilBLL;
using static LabWebMvc.MVC.Areas.Utils.Utils;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class InstituicoesController : BaseController
    {
        private readonly IPathHelper _pathHelper;

        public InstituicoesController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IPathHelper pathHelper)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService)
        {
            _pathHelper = pathHelper;
        }

        private void MontaControllers(string action, string controller, string parametros = "")
        {
            PartialFiltro.Action = action;
            PartialFiltro.Controller = controller;
            PartialFiltro.ActionButton = action + parametros;
            PartialFiltro.ControllerButton = controller;
            PartialFiltro.Esconde = false;
            ViewBag.TextoMenu = action.MensagemStartUp();
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Instituicoes")]
        public async Task<IActionResult> Index(string? Conteudo, int registros = 50)
        {
            MontaControllers("IncluirInstituicao", "Instituicoes");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            ICollection<dynamic> listaGrid = [];
            List<Instituicao> dados = [];

            int totalTabela = 0;
            int totalRegistros = 0;
            if (string.IsNullOrEmpty(Conteudo)) registros = 100; //quando não tem dados para filtrar

            totalTabela = _db.Instituicao.AsNoTracking().AsEnumerable().Count();

            if (!string.IsNullOrEmpty(Conteudo))
            {
                dados = await _db.Instituicao.AsNoTracking()
                          .FiltrarPorConteudo(Conteudo, x => x.Nome!, x => x.CNPJ, x => x.Endereco, x => x.Bairro, x => x.Cidade, x => x.Id.ToString())
                          .OrderByDescending(x => x.Id)
                          .ToListAsync();
            }
            else
                dados = await _db.Instituicao.AsNoTracking().OrderByDescending(o => o.Id).Take(registros).ToListAsync();

            foreach (Instituicao item in dados)
            {
                totalRegistros++;
                vmInstituicao resultado = new()
                {
                    Id = item.Id,
                    Sigla = item.Sigla,
                    Nome = item.Nome,
                    CNPJ = item.CNPJ.FormatarCNPJNotNull(),
                    Sequencial = item.Sequencial,
                    Email = item.Email,
                    TituloTimbre = item.TituloTimbre,
                    SubTituloTimbre = item.SubTituloTimbre,
                    CarimboSN = item.CarimboSN,
                    TimbreSN = item.TimbreSN,
                    Logradouro = item.Logradouro,
                    Endereco = item.Endereco,
                    Numero = item.Numero,
                    Complemento = item.Complemento,
                    Bairro = item.Bairro,
                    Cidade = item.Cidade,
                    UF = item.UF,
                    CEP = item.CEP,
                    Contato = item.Contato,
                    Telefone = item.Telefone.FormataTelefoneNotNull(),
                    Celular = item.Celular.FormataTelefone(),
                    UsuarioCaminhoFTP = item.UsuarioCaminhoFTP,
                    UsuarioEmailFTP = item.UsuarioEmailFTP,
                    UsuarioPortaFTP = item.UsuarioPortaFTP,
                    UsuarioSenhaFTP = item.UsuarioSenhaFTP,
                    ValorExameCitologia = item.ValorExameCitologia,
                    Propaganda = item.Propaganda,
                    AvisoRodape1 = item.AvisoRodape1,
                    AvisoRodape2 = item.AvisoRodape2,
                    /*
                     * Imagens
                     */
                    Timbre = item.Timbre,
                    Logomarca = item.Logomarca
                };
                listaGrid.Add(resultado);
            }

            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = listaGrid;

            //Finalização da View
            return _geralController.Validacao("Index", "Cadastro de Instituições", totalRegistros, totalTabela, listaGrid);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirInstituicao")]
        public IActionResult IncluirInstituicao()
        {
            ViewBag.PathImages = Utils.Utils.GetLocalPathImagens();
            //Finalização da View
            return _geralController.Validacao("IncluirInstituicao", "Cadastro de Instituições");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirInstituicao")]
        public async Task<IActionResult> SalvarInstituicao(vmInstituicao vm)
        {
            string redirecionaUrl = "Instituicoes".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.Nome))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios ou não havia nada para ser salvo" });

            Instituicao? Instituicoes = await _db.Instituicao.Where(s => s.Nome == vm.Nome || s.Sigla == vm.Sigla || s.CNPJ == vm.CNPJ).SingleOrDefaultAsync();
            if (Instituicoes != null)
            {
                if (Instituicoes.Nome == vm.Nome.ToUpper())
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição já cadastrada com este nome", action = "", sucesso = false });
                else if (Instituicoes.Sigla == vm.Sigla.ToUpper())
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição já cadastrada com esta sigla", action = "", sucesso = false });
                else if (Instituicoes.CNPJ == vm.CNPJ)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição já cadastrada com este CNPJ", action = "", sucesso = false });
                else
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição já cadastrada", action = "", sucesso = false });
            }
            try
            {
                //capturando arquivos/upload ou arquivos de imagens em bytes[]
                GetImagemTimbre(vm);
                GetImagemLogomarca(vm);

                using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
                {
                    await _db.Instituicao.AddAsync(new Instituicao()
                    {
                        //Colunas NÃO nulas:
                        Nome = vm.Nome.ToUpper(),
                        Sigla = vm.Sigla.ToUpper(),
                        CNPJ = vm.CNPJ.CNPJSemFormatacao(),
                        Email = vm.Email.ToLower(),
                        Telefone = vm.Telefone,
                        Contato = vm.Contato,
                        CarimboSN = vm.CarimboSN,
                        TimbreSN = vm.TimbreSN,

                        //Colunas que aceitam nulas:
                        Endereco = vm.Endereco.ToCapitalize(),
                        Logradouro = vm.Logradouro.ToCapitalize(),
                        Numero = vm.Numero,
                        Bairro = vm.Bairro.ToCapitalize(),
                        Complemento = vm.Complemento,
                        Cidade = vm.Cidade.ToCapitalize(),
                        UF = vm.vmGeral.TipoUF,
                        CEP = vm.CEP,
                        Celular = vm.Celular,
                        Sequencial = vm.Sequencial,
                        TituloTimbre = vm.TituloTimbre != null ? vm.TituloTimbre.ToUpper() : string.Empty,
                        SubTituloTimbre = vm.SubTituloTimbre.ToCapitalize(),
                        UsuarioCaminhoFTP = vm.UsuarioCaminhoFTP,
                        UsuarioEmailFTP = vm.UsuarioEmailFTP,
                        UsuarioPortaFTP = vm.UsuarioPortaFTP,
                        UsuarioSenhaFTP = vm.UsuarioSenhaFTP,
                        ValorExameCitologia = vm.ValorExameCitologia,
                        Propaganda = vm.Propaganda,
                        AvisoRodape1 = vm.AvisoRodape1,
                        AvisoRodape2 = vm.AvisoRodape2,

                        /*
                         * Gravando as imagens em bytes[] e nomes das imagens
                         */
                        Timbre = vm.Timbre,
                        Logomarca = vm.Logomarca,
                        NomeTimbre = vm.NomeTimbre,
                        NomeLogomarca = vm.NomeLogomarca
                    });

                    int salvo = _db.SaveChanges();
                    if (salvo <= 0)
                    {
                        LoggerFile.Write("ERRO: Instituição não foi salva na inclusão - CNPJ:" + vm.CNPJ);
                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição NÃO foi salva", action = "", sucesso = false });
                    }
                    trans.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                _eventLogHelper.LogEventViewer("[Instituicoes] Inclusão - TransactionAbortedException Message: {0} ::: " + ex.Message, "wError");
            }

            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Instituição foi salva", action = "", sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarInstituicao")]
        public async Task<IActionResult> AlterarInstituicao(vmInstituicao vm, int id)
        {
            string pathImages = Utils.Utils.GetLocalPathImagens();

            Instituicao dados = await _db.Instituicao.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.Nome = dados.Nome.ToUpper();
                vm.Sigla = dados.Sigla;
                vm.CNPJ = dados.CNPJ.CNPJSemFormatacao();
                vm.Email = dados.Email;
                vm.Endereco = dados.Endereco.ToCapitalize();
                vm.Logradouro = dados.Logradouro.ToCapitalize();
                vm.Numero = dados.Numero;
                vm.Bairro = dados.Bairro.ToCapitalize();
                vm.Complemento = dados.Complemento;
                vm.Cidade = dados.Cidade.ToCapitalize();
                vm.UF = dados.UF;
                vm.CEP = dados.CEP;
                vm.Telefone = dados.Telefone;
                vm.Celular = dados.Celular;
                vm.Sequencial = dados.Sequencial;
                vm.TituloTimbre = dados.TituloTimbre;
                vm.SubTituloTimbre = dados.SubTituloTimbre;
                vm.CarimboSN = dados.CarimboSN;
                vm.TimbreSN = dados.TimbreSN;
                vm.Contato = dados.Contato;
                vm.UsuarioCaminhoFTP = dados.UsuarioCaminhoFTP;
                vm.UsuarioEmailFTP = dados.UsuarioEmailFTP;
                vm.UsuarioPortaFTP = dados.UsuarioPortaFTP;
                vm.UsuarioSenhaFTP = dados.UsuarioSenhaFTP;
                vm.ValorExameCitologia = dados.ValorExameCitologia;
                vm.Propaganda = dados.Propaganda;
                vm.AvisoRodape1 = dados.AvisoRodape1;
                vm.AvisoRodape2 = dados.AvisoRodape2;
                /*
                 * Imagens
                 */
                vm.Timbre = dados.Timbre;
                vm.Logomarca = dados.Logomarca;
                vm.NomeTimbre = dados.NomeTimbre;
                vm.NomeLogomarca = dados.NomeLogomarca;
                vm.CaminhoImagemTimbre = pathImages;      //pasta que contém imagens para upload
                vm.CaminhoImagemLogomarca = pathImages;   //pasta que contém imagens para upload
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new()
                {
                    TipoUF = dados.UF
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis para uso em comparações que facilitam ir por ViewBag!
                 */
                ViewBag.SessionUF = dados.UF;
                ViewBag.TimbreSN = dados.TimbreSN;
                ViewBag.CarimboSN = dados.CarimboSN;
                ViewBag.PropagandaSN = dados.Propaganda;
                ViewBag.NomeTimbre = dados.NomeTimbre;
                ViewBag.NomeLogomarca = dados.NomeLogomarca;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Alterar Cadastro de Instituições", false };
            ViewBag.PathImages = pathImages;
            //Finalização da View
            _geralController.Validacao("AlterarInstituicao,Instituicoes", ViewBag.TextoMenu[0]);
            return View(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarInstituicao")]
        public async Task<IActionResult> SalvarAlteracaoInstituicao(vmInstituicao vm, int id)
        {
            string redirecionaUrl = "Instituicoes".MontaUrl(base.HttpContext.Request);

            if (vm == null || string.IsNullOrEmpty(vm.Nome) || string.IsNullOrEmpty(vm.CNPJ) || string.IsNullOrEmpty(vm.Sigla) || 
                string.IsNullOrEmpty(vm.Telefone) || string.IsNullOrEmpty(vm.Contato))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios (*) ou não havia nada para ser salvo" });

            /* ATENÇÃO:
             * Como os navegadores atuais possuem segurança que impossibilitam pegar o path completo, o nome do path será sempre "fakepath",
             * ENTÃO, SOMENTE AQUI (GetImagem...) CAPTURAMOS O ARQUIVO E GUARDAMOS A PASTA COMPLETA E CORRETA DE ONDE FOI FEITO O UPLOAD, E TAMBÉM GUARDANOS OS bytes[] do arquivo.
             */
            GetImagemTimbre(vm);
            GetImagemLogomarca(vm);

            Instituicao? Instituicoes = await _db.Instituicao.Where(s => s.Id == id).SingleOrDefaultAsync();
            if (Instituicoes == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Não foi possível salvar o registro neste momento", action = "", sucesso = false });

            try
            {
                using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
                {
                    //Colunas NÃO nulas:
                    Instituicoes.Nome = vm.Nome.ToUpper();
                    Instituicoes.Sigla = vm.Sigla.ToUpper();
                    Instituicoes.CNPJ = vm.CNPJ.CNPJSemFormatacao();
                    Instituicoes.Email = vm.Email;
                    Instituicoes.Telefone = vm.Telefone;
                    Instituicoes.Contato = vm.Contato;
                    Instituicoes.CarimboSN = vm.CarimboSN;
                    Instituicoes.TimbreSN = vm.TimbreSN;

                    //Colunas que aceitam nulo:
                    Instituicoes.Logradouro = vm.Logradouro.ToCapitalize();
                    Instituicoes.Endereco = vm.Endereco.ToCapitalize();
                    Instituicoes.Numero = vm.Numero;
                    Instituicoes.Complemento = vm.Complemento;
                    Instituicoes.Bairro = vm.Bairro.ToCapitalize();
                    Instituicoes.Cidade = vm.Cidade.ToCapitalize();
                    Instituicoes.UF = vm.vmGeral.TipoUF;
                    Instituicoes.CEP = vm.CEP;
                    Instituicoes.Celular = vm.Celular;
                    Instituicoes.Sequencial = vm.Sequencial;
                    Instituicoes.TituloTimbre = vm.TituloTimbre;
                    Instituicoes.SubTituloTimbre = vm.SubTituloTimbre;
                    Instituicoes.UsuarioCaminhoFTP = vm.UsuarioCaminhoFTP;
                    Instituicoes.UsuarioEmailFTP = vm.UsuarioEmailFTP;
                    Instituicoes.UsuarioPortaFTP = vm.UsuarioPortaFTP;
                    Instituicoes.UsuarioSenhaFTP = vm.UsuarioSenhaFTP;
                    Instituicoes.ValorExameCitologia = vm.ValorExameCitologia;
                    Instituicoes.Propaganda = vm.Propaganda;
                    Instituicoes.AvisoRodape1 = vm.AvisoRodape1;
                    Instituicoes.AvisoRodape2 = vm.AvisoRodape2;

                    /*
                     * Gravando as imagens em bytes[]
                     * Obs: o caminho de origem da imagem não é salvo, por questões de privacidade.
                     */
                    if (vm.Timbre != null)  //se for nulo, nada faz e evita de apagar o que já pode estar na base
                        Instituicoes.Timbre = vm.Timbre;

                    if (vm.NomeTimbre != null)
                        Instituicoes.NomeTimbre = vm.NomeTimbre;

                    if (vm.Logomarca != null)   //se for nulo, nada faz e evita de apagar o que já pode estar na base
                        Instituicoes.Logomarca = vm.Logomarca;

                    if (vm.NomeLogomarca != null)
                        Instituicoes.NomeLogomarca = vm.NomeLogomarca;

                    int salvo = _db.SaveChanges();
                    if (salvo == 0)
                    {
                        LoggerFile.Write("Instituição não foi atualizada por possível falha - Id:" + id.ToString());
                        return Json(new { titulo = Mensagens_pt_BR.Ok, mensagem = "Instituição não foi atualizada ou não havia nada para ser atualizado", action = "", sucesso = false });
                    }
                    else if (salvo < 0)
                    {
                        LoggerFile.Write("ERRO: Instituição não foi atualizada - Id:" + id.ToString());
                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição NÃO foi atualizada", action = "", sucesso = false });
                    }
                    trans.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                _eventLogHelper.LogEventViewer("[Instituicoes] Não foi atualizada - TransactionAbortedException Message: " + ex.Message, "wError");
            }
            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Instituição foi atualizada", action = "", sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirInstituicao")]
        public async Task<IActionResult> ExcluirInstituicao(int id)
        {
            // Excluindo um registro da tabela
            DeleteContext<Instituicao> context = new DeleteContext<Instituicao>(new DeleteStrategy<Instituicao>(_db));
            JsonResult result = await context.DeleteRecordAsync(id, "Instituicao");
            return result;
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarInstituicao")]
        public async Task<ActionResult> ConsultarInstituicao(vmInstituicao vm, int id)
        {
            Instituicao dados = await _db.Instituicao.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.Nome = dados.Nome;
                vm.Sigla = dados.Sigla;
                vm.CNPJ = dados.CNPJ.FormatarCNPJNotNull();
                vm.Email = dados.Email;
                vm.Logradouro = dados.Logradouro;
                vm.Endereco = dados.Endereco;
                vm.Numero = dados.Numero;
                vm.Complemento = dados.Complemento;
                vm.Bairro = dados.Bairro;
                vm.Cidade = dados.Cidade;
                vm.UF = dados.UF;
                vm.CEP = dados.CEP;
                vm.Telefone = dados.Telefone.FormataTelefoneNotNull();
                vm.Celular = dados.Celular;
                vm.Sequencial = dados.Sequencial;
                vm.TituloTimbre = dados.TituloTimbre;
                vm.SubTituloTimbre = dados.SubTituloTimbre;
                vm.CarimboSN = dados.CarimboSN;
                vm.TimbreSN = dados.TimbreSN;
                vm.Contato = dados.Contato;
                vm.UsuarioCaminhoFTP = dados.UsuarioCaminhoFTP;
                vm.UsuarioEmailFTP = dados.UsuarioEmailFTP;
                vm.UsuarioPortaFTP = dados.UsuarioPortaFTP;
                vm.UsuarioSenhaFTP = dados.UsuarioSenhaFTP;
                vm.ValorExameCitologia = dados.ValorExameCitologia;
                vm.Propaganda = dados.Propaganda;
                vm.AvisoRodape1 = dados.AvisoRodape1;
                vm.AvisoRodape2 = dados.AvisoRodape2;
                /*
                 * Imagens
                 */
                vm.Timbre = dados.Timbre;
                vm.Logomarca = dados.Logomarca;
                vm.NomeTimbre = dados.NomeTimbre;
                vm.NomeLogomarca = dados.NomeLogomarca;
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new()
                {
                    TipoUF = dados.UF
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis para uso em comparações que facilitam ir por ViewBag!
                 */
                ViewBag.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Consulta de Instituição", false };
            ViewBag.NomeTimbre = vm.NomeTimbre;
            ViewBag.NomeLogomarca = vm.NomeLogomarca;
            //Finalização para a View
            _geralController.Validacao("ConsultarInstituicao,Instituicoes", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirImagemTimbre")]
        public async Task<IActionResult> ExcluirImagemTimbre(string sigla)
        {
            using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
            {
                bool erro = false;
                int salvo = 0;

                try
                {
                    Instituicao registro = await _db.Instituicao.FirstAsync(s => s.Sigla == sigla);
                    if (registro != null && registro.Sigla == sigla)
                    {
                        registro.NomeTimbre = "";  //limpa o nome da imagem
                        registro.Timbre = null;    //limpa o byte[] da imagem

                        salvo = _db.SaveChanges();
                        if (salvo == 0)
                        {
                            LoggerFile.Write("Imagem Timbre da Instituição não foi atualizada por possível falha - Sigla: " + sigla);
                            return Json(new { titulo = Mensagens_pt_BR.Ok, mensagem = "Imagem não foi excluída", action = "", sucesso = false });
                        }
                        else if (salvo < 0)
                        {
                            LoggerFile.Write("ERRO: Imagem da Instituição não foi excluída - Sigla: " + sigla);
                            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Imagem não foi excluida", action = "", sucesso = false });
                        }
                    }
                }
                catch
                {
                    erro = true;
                }
                finally
                {
                    trans.Complete();
                }
                if (erro || salvo < 1)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Imagem não foi excluída", action = "", sucesso = false });
            }
            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Imagem foi excluída da instituição", action = "", sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirImagemLogomarca")]
        public async Task<IActionResult> ExcluirImagemLogomarca(string sigla)
        {
            //Na verdade, não é uma exclusão de registro e sim LIMPEZA do campo!
            using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
            {
                bool erro = false;
                int salvo = 0;
                try
                {
                    Instituicao registro = await _db.Instituicao.FirstAsync(s => s.Sigla == sigla);
                    if (registro != null && registro.Sigla == sigla)
                    {
                        registro.NomeLogomarca = "";   //limpa o nome da imagem
                        registro.Logomarca = null;     //limpa o byte[] da imagem

                        salvo = _db.SaveChanges();
                        if (salvo == 0)
                        {
                            LoggerFile.Write("Imagem Logomarca da Instituição não foi atualizada por possível falha - Sigla: " + sigla);
                            return Json(new { titulo = Mensagens_pt_BR.Ok, mensagem = "Imagem não foi excluída", action = "", sucesso = false });
                        }
                        else if (salvo < 0)
                        {
                            LoggerFile.Write("ERRO: Imagem da Instituição não foi excluída - Sigla: " + sigla);
                            return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Imagem não foi excluida", action = "", sucesso = false });
                        }
                    }
                }
                catch
                {
                    erro = true;
                }
                finally
                {
                    trans.Complete();
                }
                if (erro || salvo < 1)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Imagem não foi excluída", action = "", sucesso = false });
            }
            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Imagem foi excluída da instituição", action = "", sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConverterPdf")]
        public IActionResult ConverterPdf()
        {
            try
            {
                //ConversoresPdf pdf = new ConversoresPdf();
                //pdf.ConverteHtmlToPdf(@"F:\Temp2\Arquivo.html");

                //pdf.ConverteHtmlToPdf();

                return Json(new { success = true, responseText = "Salvou com sucesso" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Instituicoes] Falha ao converter PDF: " + ex.Message, "wError");
                return Json(new { success = false, responseText = string.Format("{0} {1}", "Falha:", ex.Message) });
            }
        }

        /*
         * Salva uma imagem em bytes[] já pronta para ser exibida em outro momento
         */

        private void GetImagemTimbre(vmInstituicao vm)
        {
            if (NaoExistePath(vm.CaminhoImagemTimbre))
            {
                /* Na inclusão nunca teremos o path correto via JQuery, por isso manobramos aqui para pegar C:\Images\ no computador Local */
                vm.CaminhoImagemTimbre = Utils.Utils.GetLocalPathImagens();
            }
            vm.CaminhoImagemTimbre = _pathHelper.GetPathTrue(vm.CaminhoImagemTimbre, vm.NomeTimbre);

            if (!string.IsNullOrEmpty(vm.CaminhoImagemTimbre) && !string.IsNullOrEmpty(vm.NomeTimbre))
            {
                string path = Path.Combine(vm.CaminhoImagemTimbre, vm.NomeTimbre);
                if (path != null)
                {
                    if (System.IO.File.Exists(path))
                    {
                        FileStream oFileStream = new(path, FileMode.Open, FileAccess.Read);
                        // Create a byte array of file size.
                        byte[] FileByteArrayData = new byte[oFileStream.Length];
                        //Read file in bytes from stream into the byte array
                        oFileStream.Read(FileByteArrayData, 0, System.Convert.ToInt32(oFileStream.Length));
                        //Close the File Stream
                        oFileStream.Close();

                        vm.Timbre = FileByteArrayData; //return the byte data
                    }
                }
            }
        }

        /*
         * Salva uma imagem em bytes[] já pronta para ser exibida em outro momento
         */

        private void GetImagemLogomarca(vmInstituicao vm)
        {
            if (NaoExistePath(vm.CaminhoImagemLogomarca))
            {
                /* Na inclusão nunca teremos o path correto via JQuery, por isso manobramos aqui para pegar C:\Images\ no computador Local */
                vm.CaminhoImagemLogomarca = Utils.Utils.GetLocalPathImagens();
            }
            vm.CaminhoImagemLogomarca = _pathHelper.GetPathTrue(vm.CaminhoImagemLogomarca, vm.NomeLogomarca);

            if (!string.IsNullOrEmpty(vm.CaminhoImagemLogomarca) && !string.IsNullOrEmpty(vm.NomeLogomarca))
            {
                string path = Path.Combine(vm.CaminhoImagemLogomarca, vm.NomeLogomarca);
                if (path != null)
                {
                    if (System.IO.File.Exists(path))
                    {
                        FileStream oFileStream = new(path, FileMode.Open, FileAccess.Read);
                        // Create a byte array of file size.
                        byte[] FileByteArrayData = new byte[oFileStream.Length];
                        //Read file in bytes from stream into the byte array
                        oFileStream.Read(FileByteArrayData, 0, System.Convert.ToInt32(oFileStream.Length));
                        //Close the File Stream
                        oFileStream.Close();

                        vm.Logomarca = FileByteArrayData; //return the byte data
                    }
                }
            }
        }
    }

    //internal class CustomErrorModel
    //{
    //    private string v;

    //    public CustomErrorModel(string v)
    //    {
    //        this.v = v;
    //    }
    //}
}