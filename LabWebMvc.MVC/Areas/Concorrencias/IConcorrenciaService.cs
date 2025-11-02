using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace LabWebMvc.MVC.Areas.Concorrencias
{
    public interface IConcorrenciaService
    {
        /// <summary>
        /// Valida se há concorrência ativa para o processo e atualiza o registro se possível.
        /// </summary>
        /// <param name="processo">Identificador único do processo.</param>
        /// <param name="tempoRetidoMinutos">Tempo de retenção da concorrência em minutos (padrão: 10).</param>
        Task<bool> ValidarOuAtualizarConcorrenciaAsync(string processo, int tempoRetidoMinutos = 10);

        /// <summary>
        /// Remove o controle de concorrência para o processo informado.
        /// </summary>
        /// <param name="processo">Identificador único do processo.</param>
        Task RemoverConcorrenciaAsync(string processo);



    }
}
