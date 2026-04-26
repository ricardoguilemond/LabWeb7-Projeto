using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.Models;

public partial class ControleConcorrencia
{
    [Key]   //obrigatório aqui, pois precisamos informar ao Entity que a chave primária é a coluna "Processo"
    public string Processo { get; set; } = null!;

    public DateTime DataHora { get; set; }
}