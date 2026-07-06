namespace LabWebMvc.MVC.Models;

//Feito pelo Kiro em 11/07/2025
public partial class ExameReferencia
{
    public int Id { get; set; }
    public string ContaExame { get; set; } = null!;
    public int TabelaExamesId { get; set; }
    public byte[] ConteudoBinario { get; set; } = null!;
    public string FormatoOrigem { get; set; } = "RTF";
    public int AlinhaLaudo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAlteracao { get; set; }
    public string UsuarioAlteracao { get; set; } = null!;
    public int Versao { get; set; } = 1;

    public virtual TabelaExames TabelaExames { get; set; } = null!;
}
//..Kiro
