using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ExpressionCombiner;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Threading;
using static BLL.UtilBLL;
using static LabWebMvc.MVC.UtilHelper.TrataExcecoes;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class ClasseExamesController : BaseController
    {
        private readonly IConcorrenciaService _concorrenciaService;
        public ClasseExamesController(IDbFactory dbFactory, 
                                      IValidadorDeSessao validador, 
                                      GeralController geralController, 
                                      IEventLogHelper eventLogHelper, 
                                      Imagem imagem,
                                      IConcorrenciaService concorrenciaService)
               : base(dbFactory, validador, geralController, eventLogHelper, imagem)
        { 
           _concorrenciaService = concorrenciaService;  
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

        /* Insere ou Atualiza a Folha de Exames no Plano de Exames */

        private async void AtualizaFolhaNoPlanoDeExames(vmClasseExames vm)
        {
            //vm.Id é o código da Conta Folha em "ClasseExames"
            string contaFolha = Utils.Utils.RetornaCodigoFolhaExame(_db, vm.Id);

            //ATUALIZAÇÃO DO MHI NA FOLHA DE EXAMES COM O ID DA FOLHA CRIADO
            ClasseExames? folha = await _db.ClasseExames.Where(s => s.Id == vm.Id).FirstOrDefaultAsync();
            if (folha != null)
            {
                Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            folha.MHI = vm.Id;

                            await _db.SaveChangesAsync();  //só atualização do registro

                            await transaction.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();

                            _eventLogHelper.LogEventViewer("ERRO: Tentando salvar Folha de Exame padrão em PlanoExames", "wWarning");

                            TrataExceptionViewer(ex, _db);
                        }
                        finally
                        { }
                    }
                });
            }

            //ATUALIZAÇÃO DO PLANO DE EXAMES
            List<TabelaExames> listaTabelaExames = await _db.TabelaExames.Where(s => s.Bloqueado == 0).OrderBy(o => o.Id).ToListAsync();  //Tabela de Exames, Header/Identificadores das Tabelas de Preços dos Exames.
            foreach (TabelaExames? item in listaTabelaExames)
            {
                string idTabela = item.Id.ToString().PadLeft(2, '0');   //Código que identifica a Tabela de Preço

                PlanoExames? planoExames = await _db.PlanoExames.Where(s => s.ContaExame == contaFolha && s.ExameId == vm.Id && s.TabelaExamesId == item.Id).FirstOrDefaultAsync();
                if (planoExames == null && vm.RefExame != null)
                {
                    //Caso não exista, cria o registro com nome relativo ao FOLHA DE EXAME (e este registro deverá ser único agora)
                    //E se já existir não vai fazer nada...

                    Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                PlanoExames plano = new()
                                {
                                    ExameId = vm.Id,
                                    RefExame = vm.RefExame.ToUpper(),
                                    RefItem = vm.RefExame.ToUpper(),
                                    Descricao = vm.RefExame.ToUpper(),
                                    TabelaExamesId = item.Id,   //identifica a unidade de saúde na TabelaExames
                                    ContaExame = contaFolha,
                                    QCH = 0,
                                    Etiqueta = 0,
                                    Etiquetas = 0,
                                    AlinhaLaudo = 0,
                                    Seleciona = 0,
                                    NaoMostrar = 0
                                };

                                await _db.PlanoExames.AddAsync(plano);

                                await _db.SaveChangesAsync();

                                await transaction.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();

                                _eventLogHelper.LogEventViewer("ERRO: Tentando salvar Folha de Exame padrão em PlanoExames, mas parece que já existia uma.", "wWarning");

                                TrataExceptionViewer(ex, _db);
                            }
                            finally
                            { }
                        }
                    });
                }
                else  //Alteração do registro existente no plano de exames
                {
                    Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                if (planoExames != null)
                                {
                                    planoExames.RefExame = (vm.RefExame ?? throw new ArgumentNullException(nameof(vm.RefExame))).ToUpper();   //não pode vir nulo!
                                    planoExames.RefItem = vm.RefExame.ToUpper();    //tratado acima, pois não pode vir nulo
                                    planoExames.Descricao = vm.RefExame.ToUpper();  //tratado acima, pois não pode vir nulo
                                    planoExames.QCH = 0;
                                    planoExames.Etiqueta = 0;
                                    planoExames.Etiquetas = 0;
                                    planoExames.AlinhaLaudo = 0;
                                    planoExames.Seleciona = 0;
                                    planoExames.NaoMostrar = 0;

                                    await _db.SaveChangesAsync();

                                    await transaction.CommitAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                await transaction.RollbackAsync();

                                _eventLogHelper.LogEventViewer("Falhou ao tentar salvar Folha de Exame padrão em PlanoExames.", "wWarning");

                                TrataExceptionViewer(ex, _db);
                            }
                            finally
                            { }
                        }
                    });
                }
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ClasseExames")]
        public async Task<IActionResult> Index(string? Conteudo, int registros = 50)
        {
            MontaControllers("IncluirClasseExames", "ClasseExames");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            ICollection<dynamic> listaGrid = [];
            List<ClasseExames> dados = [];

            int totalTabela = 0;
            int totalRegistros = 0;
            if (string.IsNullOrEmpty(Conteudo)) registros = 100; //quando não tem dados para filtrar

            totalTabela = _db.ClasseExames.AsNoTracking().AsEnumerable().Count();

            if (!string.IsNullOrEmpty(Conteudo))
            {
                dados = await _db.ClasseExames.AsNoTracking()
                                              .FiltrarPorConteudo(Conteudo, x => x.RefExame!, x => x.Id.ToString())
                                              .OrderByDescending(x => x.Id)
                                              .ToListAsync();
            }
            else
                dados = await _db.ClasseExames.AsNoTracking().OrderByDescending(o => o.Id).Take(registros).ToListAsync();

            foreach (ClasseExames item in dados)
            {
                totalRegistros++;
                vmClasseExames resultado = new()
                {
                    Id = item.Id,
                    RefExame = item.RefExame,
                    LaboratorioExterno = item.LaboratorioExterno == null || item.LaboratorioExterno.ToString() == "NULL" ? string.Empty : item.LaboratorioExterno,
                    Etiquetas = item.Etiquetas,
                    TipoMapa = item.TipoMapa,
                    Planilha = item.Planilha,
                    Assinatura1 = item.Assinatura1,
                    Assinatura2 = item.Assinatura2,
                    Assinatura3 = item.Assinatura3,
                    Assinatura4 = item.Assinatura4,
                    MHI = item.MHI,    //índice que define a ordem das folhas no mapa horizontal (mapa de folha deitada/paisagem/landscape
                    /*
                     * Imagens
                     */
                    ImgAss1 = item.ImgAss1,
                    ImgAss2 = item.ImgAss2,
                    ImgAss3 = item.ImgAss3,
                    ImgAss4 = item.ImgAss4
                };
                listaGrid.Add(resultado);
            }

            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = listaGrid;

            //Finalização da View
            return _geralController.Validacao("Index", "Cadastro de Folhas de Exames", totalRegistros, totalTabela, listaGrid);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirClasseExames")]
        public IActionResult IncluirClasseExames()
        {
            ViewBag.PathImages = Areas.Utils.Utils.GetLocalPathImagens();
            //Finalização da View
            return _geralController.Validacao("IncluirClasseExames", "Cadastro de Folha de Exames");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirClasseExames")]
        public async Task<IActionResult> SalvarClasseExames(vmClasseExames vm)
        {
            string redirecionaUrl = "ClasseExames".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.RefExame))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios" });

            /* ATENÇÃO:
             * Como os navegadores atuais possuem segurança que impossibilitam pegar o path completo, o nome do path será sempre "fakepath",
             * ENTÃO, SOMENTE AQUI (GetImagem...) CAPTURAMOS O ARQUIVO E GUARDAMOS A PASTA COMPLETA E CORRETA DE ONDE FOI FEITO O UPLOAD, E TAMBÉM GUARDANOS OS bytes[] do arquivo.
             */
            GetImagemAss1(vm);
            GetImagemAss2(vm);
            GetImagemAss3(vm);
            GetImagemAss4(vm);

            ClasseExames? ClasseExames = await _db.ClasseExames.Where(s => s.RefExame == vm.RefExame && s.LaboratorioExterno == vm.LaboratorioExterno).SingleOrDefaultAsync();
            if (ClasseExames != null)
            {
                if (ClasseExames.RefExame == vm.RefExame && ClasseExames.LaboratorioExterno == vm.LaboratorioExterno)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Já existe Folha de Exame nesta Instituição cadastrada com este nome", action = "", sucesso = false });
                else
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Já existe esta Folha de Exame cadastrada", action = "", sucesso = false });
            }

            int novoId = 0;

            var res = new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Folha de Exame foi salva", action = "", sucesso = true };

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        ClasseExames folha = new()
                        {
                            //Colunas NÃO nulas:
                            RefExame = vm.RefExame.ToUpper(),
                            Marcado = vm.Marcado,
                            Etiquetas = vm.Etiquetas,
                            Planilha = vm.Planilha,
                            MHI = vm.MHI,

                            //Colunas que aceitam nulas:
                            LaboratorioExterno = vm.LaboratorioExterno?.ToUpper(),
                            TipoMapa = string.IsNullOrEmpty(vm.TipoMapa) ? "C" : vm.TipoMapa,
                            //Se mostra ou não as imagens das assinaturas nos exames (S/N)
                            Assinatura1 = vm.Assinatura1,
                            Assinatura2 = vm.Assinatura2,
                            Assinatura3 = vm.Assinatura3,
                            Assinatura4 = vm.Assinatura4,
                            /*
                             * Gravando as Imagens
                             */
                            ImgAss1 = vm.ImgAss1,
                            ImgAss2 = vm.ImgAss2,
                            ImgAss3 = vm.ImgAss3,
                            ImgAss4 = vm.ImgAss4,
                            //Nome das imagens
                            NomeAss1 = vm.NomeAss1,
                            NomeAss2 = vm.NomeAss2,
                            NomeAss3 = vm.NomeAss3,
                            NomeAss4 = vm.NomeAss4
                        };

                        await _db.ClasseExames.AddAsync(folha);

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        novoId = folha.Id;   //obtém o Id recém-gerado

                        vm.Id = novoId;
                        vm.MHI = novoId;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("ERRO: Folha de exame não foi salva", "wWarning");

                        TrataExceptionViewer(ex, _db);

                        res = new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Folha de exame não foi salva", action = "", sucesso = false };
                    }
                }
            });

            if (novoId > 0) //Se gerou a folha, vamos atualizar a Folha no Plano de Exames (Header da Folha)
            {
                AtualizaFolhaNoPlanoDeExames(vm);
            }

            return new JsonResult(res);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarClasseExames")]
        public async Task<IActionResult> AlterarClasseExames(vmClasseExames vm, int id)
        {
            string pathImages = Areas.Utils.Utils.GetLocalPathImagens();

            ClasseExames dados = await _db.ClasseExames.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.RefExame = dados.RefExame;
                vm.LaboratorioExterno = dados.LaboratorioExterno?.ToUpper();
                vm.Etiquetas = dados.Etiquetas;
                vm.TipoMapa = string.IsNullOrEmpty(dados.TipoMapa) ? "C" : dados.TipoMapa;
                vm.Planilha = dados.Planilha;
                /*
                 * Imagens
                 */
                vm.ImgAss1 = dados.ImgAss1;
                vm.ImgAss2 = dados.ImgAss2;
                vm.ImgAss3 = dados.ImgAss3;
                vm.ImgAss4 = dados.ImgAss4;
                vm.NomeAss1 = dados.NomeAss1;
                vm.NomeAss2 = dados.NomeAss2;
                vm.NomeAss3 = dados.NomeAss3;
                vm.NomeAss4 = dados.NomeAss4;
                vm.CaminhoImgAss1 = pathImages;      //pasta que contém imagens para upload
                vm.CaminhoImgAss2 = pathImages;      //pasta que contém imagens para upload
                vm.CaminhoImgAss3 = pathImages;      //pasta que contém imagens para upload
                vm.CaminhoImgAss4 = pathImages;      //pasta que contém imagens para upload
                /*
                 * Campos auxiliares
                 */
                ViewBag.TipoMapa = dados.TipoMapa;
                ViewBag.ass1SN = dados.Assinatura1;
                ViewBag.ass2SN = dados.Assinatura2;
                ViewBag.ass3SN = dados.Assinatura3;
                ViewBag.ass4SN = dados.Assinatura4;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Alterar Cadastro de Folha de Exames", false };
            ViewBag.PathImages = pathImages;
            //Finalização da View
            _geralController.Validacao("AlterarClasseExames", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarClasseExames")]
        public async Task<IActionResult> SalvarAlteracaoClasseExames(vmClasseExames vm, int id)
        {
            string redirecionaUrl = "ClasseExames".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.RefExame))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios" });

            /* ATENÇÃO:
             * Como os navegadores atuais possuem segurança que impossibilitam pegar o path completo, o nome do path será sempre "fakepath",
             * ENTÃO, SOMENTE AQUI (GetImagem...) CAPTURAMOS O ARQUIVO E GUARDAMOS A PASTA COMPLETA E CORRETA DE ONDE FOI FEITO O UPLOAD, E TAMBÉM GUARDAMOS OS bytes[] do arquivo.
             */
            GetImagemAss1(vm);
            GetImagemAss2(vm);
            GetImagemAss3(vm);
            GetImagemAss4(vm);

            ClasseExames? ClasseExames = await _db.ClasseExames.Where(s => s.Id == id).SingleOrDefaultAsync();
            if (ClasseExames == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Não foi possível salvar o registro neste momento", action = "", sucesso = false });

            var res = new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Folha de Exame foi atualizada", action = "", sucesso = true };

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        ClasseExames.MHI = id;
                        ClasseExames.RefExame = vm.RefExame.ToUpper();
                        ClasseExames.LaboratorioExterno = vm.LaboratorioExterno?.ToUpper();
                        ClasseExames.Etiquetas = vm.Etiquetas;
                        ClasseExames.TipoMapa = vm.TipoMapa;
                        ClasseExames.Planilha = vm.Planilha;
                        ClasseExames.Assinatura1 = vm.Assinatura1;
                        ClasseExames.Assinatura2 = vm.Assinatura2;
                        ClasseExames.Assinatura3 = vm.Assinatura3;
                        ClasseExames.Assinatura4 = vm.Assinatura4;
                        ClasseExames.ImgAss1 = vm.ImgAss1;
                        ClasseExames.ImgAss2 = vm.ImgAss2;
                        ClasseExames.ImgAss3 = vm.ImgAss3;
                        ClasseExames.ImgAss4 = vm.ImgAss4;
                        ClasseExames.NomeAss1 = vm.NomeAss1;
                        ClasseExames.NomeAss2 = vm.NomeAss2;
                        ClasseExames.NomeAss3 = vm.NomeAss3;
                        ClasseExames.NomeAss4 = vm.NomeAss4;

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("ERRO: Folha de Exames não foi atualizada - Id:" + id.ToString(), "wError");

                        TrataExceptionViewer(ex, _db);

                        res = new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = $"Paciente NÃO foi salvo", action = "", sucesso = false };
                    }
                }
            });

            /* Vamos acionar a atualização da conta folha no Plano de Exames  */
            AtualizaFolhaNoPlanoDeExames(vm);

            return new JsonResult(res);
        }

        /* Atenção: a exclusão com "ExecuteDeleteAsync" não pode ter um TransactionScope, porque ela fica executando async mas o método é imediatamente liberado  */

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirClasseExames")]
        public async Task<IActionResult> ExcluirClasseExames(int id)
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync();
            var consultaExames = await
            (from ple in _db.PlanoExames

             join era in _db.ItensExamesRealizados on ple.ExameId equals era.ClasseExamesId into groupItensExames
             from subgroup1 in groupItensExames.DefaultIfEmpty() //left join

             join eram in _db.ItensExamesRealizadosAM on ple.ExameId equals eram.ClasseExamesId into groupItensExamesAM
             from subgroup2 in groupItensExamesAM.DefaultIfEmpty() //left join

                 //não vale as contas de Folha de Exame, pois elas serão excluídas mais adiante.
             where ple.ExameId == id && ple.ContaExame.Substring(4, 7) != "0000000"

             select new
             {
                 id = ple.Id,
                 descricao = ple.Descricao
             }
                ).FirstOrDefaultAsync();

            if (consultaExames != null)
                return Json(new { titulo = "Erro", mensagem = $"Não posso excluir a Folha Nº {id}, pois há exames em uso.", sucesso = false });

            //Controle de concorrências para a exclusão
            bool podeExcluir = await _concorrenciaService.ValidarOuAtualizarConcorrenciaAsync("Exclusao_De_Folha_De_Exame");

            if (!podeExcluir)
            {
                await transaction.RollbackAsync();
                return Json(new { titulo = "Erro", mensagem = "Existe uma operação concorrente em andamento. Aguarde!", sucesso = false });
            }

            try
            {
                // Excluir registros
                int exclusaoPlanoExames = await _db.PlanoExames.Where(pe => pe.ExameId == id).ExecuteDeleteAsync();

                if (exclusaoPlanoExames > 0)
                {
                    await _db.ClasseExames.Where(ce => ce.Id == id).ExecuteDeleteAsync();
                }

                await _concorrenciaService.RedefinirIncremento("ClasseExames", _db, transaction); // Agora usa a mesma transação

                await transaction.CommitAsync();

                await _concorrenciaService.RedefinirIncremento("ClasseExames", _db, transaction); // Agora usa a mesma transação

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { titulo = "Erro", mensagem = "Erro ao excluir registro: " + ex.Message, sucesso = false });
            }
            finally
            {
                await _concorrenciaService.RemoverConcorrenciaAsync("Exclusao_De_Folha_De_Exame");

                //Desta tabela, se for o último registro que foi excluído, então vamos recuperar o auto incremento, para evitar perder sequência até 99.
                //await _concorrenciaService.RedefinirIncremento("ClasseExames");
            }

            return Json(new { titulo = "Sucesso", mensagem = $"Folha Nº {id} excluída!", sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarClasseExames")]
        public async Task<ActionResult> ConsultarClasseExames(vmClasseExames vm, int id)
        {
            ClasseExames dados = await _db.ClasseExames.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.RefExame = dados.RefExame?.ToUpper();
                vm.LaboratorioExterno = dados.LaboratorioExterno == null || dados.LaboratorioExterno.ToString() == "NULL" ? string.Empty : dados.LaboratorioExterno;
                vm.Etiquetas = dados.Etiquetas;
                vm.TipoMapa = dados.TipoMapa;
                vm.Planilha = dados.Planilha;
                vm.Assinatura1 = dados.Assinatura1;
                vm.Assinatura2 = dados.Assinatura2;
                vm.Assinatura3 = dados.Assinatura3;
                vm.Assinatura4 = dados.Assinatura4;
                vm.ImgAss1 = dados.ImgAss1;
                vm.ImgAss2 = dados.ImgAss2;
                vm.ImgAss3 = dados.ImgAss3;
                vm.ImgAss4 = dados.ImgAss4;
                vm.NomeAss1 = dados.NomeAss1;
                vm.NomeAss2 = dados.NomeAss2;
                vm.NomeAss3 = dados.NomeAss3;
                vm.NomeAss4 = dados.NomeAss4;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Consulta Folha de Exames", false };
            ViewBag.Assinatura1 = vm.Assinatura1;
            ViewBag.Assinatura2 = vm.Assinatura2;
            ViewBag.Assinatura3 = vm.Assinatura3;
            ViewBag.Assinatura4 = vm.Assinatura4;

            //Finalização para a View
            _geralController.Validacao("ConsultarClasseExames", ViewBag.TextoMenu[0]);
            return PartialView(vm);
        }

        /*
         * Salva uma imagem em bytes[] já pronta para ser exibida em outro momento
         */

        private void GetImagemAss1(vmClasseExames vm)
        {
            if (vm.Assinatura1 == 0 && vm.GetAssinatura1 == "N") vm.NomeAss1 = string.Empty;

            if (NaoExistePath(vm.CaminhoImgAss1))
            {
                /* Na inclusão nunca teremos o path correto via JQuery, por isso manobramos aqui para pegar C:\Images\ no computador Local */
                vm.CaminhoImgAss1 = Areas.Utils.Utils.GetLocalPathImagens();
            }
            vm.CaminhoImgAss1 = string.IsNullOrEmpty(vm.NomeAss1) ? string.Empty : _imagem.GetPathTrue(vm!.CaminhoImgAss1, vm.NomeAss1);

            vm.ImgAss1 = null;   // new byte[] { }; usar new byte[] {} somente se o campo de imagem não aceitar nulo!

            if (!string.IsNullOrEmpty(vm.CaminhoImgAss1) && !string.IsNullOrEmpty(vm.NomeAss1))
            {
                string path = Path.Combine(vm.CaminhoImgAss1, vm.NomeAss1);

                if (System.IO.File.Exists(path))
                {
                    vm.ImgAss1 = System.IO.File.ReadAllBytes(path);
                }
            }
            if (vm.Assinatura1 == 0 && vm.GetAssinatura1 == "N") vm.CaminhoImgAss1 = string.Empty;
        }

        private void GetImagemAss2(vmClasseExames vm)
        {
            if (vm.Assinatura2 == 0 && vm.GetAssinatura2 == "N") vm.NomeAss2 = string.Empty;

            if (NaoExistePath(vm.CaminhoImgAss2))
            {
                /* Na inclusão nunca teremos o path correto via JQuery, por isso manobramos aqui para pegar C:\Images\ no computador Local */
                vm.CaminhoImgAss2 = Areas.Utils.Utils.GetLocalPathImagens();
            }
            vm.CaminhoImgAss2 = string.IsNullOrEmpty(vm.NomeAss2) ? string.Empty : _imagem.GetPathTrue(vm!.CaminhoImgAss2, vm.NomeAss2);
            vm.ImgAss2 = null;   // new byte[] { }; usar new byte[] {} somente se o campo de imagem não aceitar nulo!

            if (!string.IsNullOrEmpty(vm.CaminhoImgAss2) && !string.IsNullOrEmpty(vm.NomeAss2))
            {
                string path = Path.Combine(vm.CaminhoImgAss2, vm.NomeAss2);

                if (System.IO.File.Exists(path))
                {
                    vm.ImgAss2 = System.IO.File.ReadAllBytes(path);
                }
            }
            if (vm.Assinatura2 == 0 && vm.GetAssinatura2 == "N") vm.CaminhoImgAss2 = string.Empty;
        }

        private void GetImagemAss3(vmClasseExames vm)
        {
            if (vm.Assinatura3 == 0 && vm.GetAssinatura3 == "N") vm.NomeAss3 = string.Empty;

            if (NaoExistePath(vm.CaminhoImgAss3))
            {
                /* Na inclusão nunca teremos o path correto via JQuery, por isso manobramos aqui para pegar C:\Images\ no computador Local */
                vm.CaminhoImgAss3 = Areas.Utils.Utils.GetLocalPathImagens();
            }
            vm.CaminhoImgAss3 = string.IsNullOrEmpty(vm.NomeAss3) ? string.Empty : _imagem.GetPathTrue(vm!.CaminhoImgAss3, vm.NomeAss3);
            vm.ImgAss3 = null;   // new byte[] { }; usar new byte[] {} somente se o campo de imagem não aceitar nulo!

            if (!string.IsNullOrEmpty(vm.CaminhoImgAss3) && !string.IsNullOrEmpty(vm.NomeAss3))
            {
                string path = Path.Combine(vm.CaminhoImgAss3, vm.NomeAss3);

                if (System.IO.File.Exists(path))
                {
                    vm.ImgAss3 = System.IO.File.ReadAllBytes(path);
                }
            }
            if (vm.Assinatura3 == 0 && vm.GetAssinatura3 == "N") vm.CaminhoImgAss3 = string.Empty;
        }

        private void GetImagemAss4(vmClasseExames vm)
        {
            if (vm.Assinatura4 == 0 && vm.GetAssinatura4 == "N") vm.NomeAss4 = string.Empty;

            if (NaoExistePath(vm.CaminhoImgAss4))
            {
                /* Na inclusão nunca teremos o path correto via JQuery, por isso manobramos aqui para pegar C:\Images\ no computador Local */
                vm.CaminhoImgAss4 = Areas.Utils.Utils.GetLocalPathImagens();
            }
            vm.CaminhoImgAss4 = string.IsNullOrEmpty(vm.NomeAss4) ? string.Empty : _imagem.GetPathTrue(vm!.CaminhoImgAss4, vm.NomeAss4);
            vm.ImgAss4 = null;   // new byte[] { }; usar new byte[] {} somente se o campo de imagem não aceitar nulo!

            if (!string.IsNullOrEmpty(vm.CaminhoImgAss4) && !string.IsNullOrEmpty(vm.NomeAss4))
            {
                string path = Path.Combine(vm.CaminhoImgAss4, vm.NomeAss4);

                if (System.IO.File.Exists(path))
                {
                    vm.ImgAss4 = System.IO.File.ReadAllBytes(path);
                }
            }
            if (vm.Assinatura4 == 0 && vm.GetAssinatura4 == "N") vm.CaminhoImgAss4 = string.Empty;
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