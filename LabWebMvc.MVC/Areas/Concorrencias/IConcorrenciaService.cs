using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace LabWebMvc.MVC.Areas.Concorrencias
{
    public interface IConcorrenciaService
    {
        Task<bool> ValidarOuAtualizarConcorrenciaAsync(string processo, int tempoRetidoMinutos = 10);
        Task RemoverConcorrenciaAsync(string processo);
        Task RedefinirIncremento(string nomeTabela, Db db, IDbContextTransaction transaction);
    }
}
