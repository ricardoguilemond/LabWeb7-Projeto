using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.Concorrencias
{
    public class ExclusaoService
    {
        private readonly IConcorrenciaService _concorrenciaService;

        public ExclusaoService(IConcorrenciaService concorrenciaService)
        {
            _concorrenciaService = concorrenciaService;
        }

        public async Task<JsonResult> ExcluirEntidadeComConcorrenciaAsync<T>(
                                      Db db,
                                      int id,
                                      string nomeConcorrencia,
                                      Expression<Func<T, bool>> filtro,
                                      Func<Task<bool>> validacaoExtra = null
        ) where T : class
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            if (validacaoExtra != null)
            {
                bool podeExcluir = await validacaoExtra();
                if (!podeExcluir)
                {
                    return new JsonResult(new
                    {
                        titulo = "Erro",
                        mensagem = $"Não é possível excluir o registro {id}, pois há vínculos ativos.",
                        sucesso = false
                    });
                }
            }

            bool concorrenciaOk = await _concorrenciaService.ValidarOuAtualizarConcorrenciaAsync(nomeConcorrencia);
            if (!concorrenciaOk)
            {
                await transaction.RollbackAsync();
                return new JsonResult(new
                {
                    titulo = "Erro",
                    mensagem = "Existe uma operação concorrente em andamento. Aguarde!",
                    sucesso = false
                });
            }

            try
            {
                int excluidos = await db.Set<T>().Where(filtro).ExecuteDeleteAsync();

                if (excluidos == 0)
                {
                    await transaction.RollbackAsync();
                    return new JsonResult(new
                    {
                        titulo = "Erro",
                        mensagem = $"Registro {id} não encontrado ou já excluído.",
                        sucesso = false
                    });
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new JsonResult(new
                {
                    titulo = "Erro",
                    mensagem = "Erro ao excluir registro: " + ex.Message,
                    sucesso = false
                });
            }
            finally
            {
                await _concorrenciaService.RemoverConcorrenciaAsync(nomeConcorrencia);
            }

            return new JsonResult(new
            {
                titulo = "Sucesso",
                mensagem = $"Registro {id} excluído com sucesso!",
                sucesso = true
            });
        }
    }

}
