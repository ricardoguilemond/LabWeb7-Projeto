using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class Instituicao
{
    public int Id { get; set; }

    public string Sigla { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public string CNPJ { get; set; } = null!;

    public int Sequencial { get; set; }

    public string Email { get; set; } = null!;

    public string? TituloTimbre { get; set; }

    public string? SubTituloTimbre { get; set; }

    public byte[]? Timbre { get; set; }

    public byte[]? Logomarca { get; set; }

    public string? NomeTimbre { get; set; }

    public string? NomeLogomarca { get; set; }

    public int CarimboSN { get; set; }

    public int TimbreSN { get; set; }

    public string? Logradouro { get; set; }

    public string? Endereco { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? UF { get; set; }

    public string? CEP { get; set; }

    public string Contato { get; set; } = null!;

    public string Telefone { get; set; } = null!;

    public string? Celular { get; set; }

    public string? UsuarioCaminhoFTP { get; set; }

    public string? UsuarioEmailFTP { get; set; }

    public int? UsuarioPortaFTP { get; set; }

    public string? UsuarioSenhaFTP { get; set; }

    public decimal? ValorExameCitologia { get; set; }

    public int? Propaganda { get; set; }

    public string? AvisoRodape1 { get; set; }

    public string? AvisoRodape2 { get; set; }

    public virtual ICollection<ExamesExportados> ExamesExportados { get; set; } = new List<ExamesExportados>();

    public virtual ICollection<ExamesImpressos> ExamesImpressos { get; set; } = new List<ExamesImpressos>();

    public virtual ICollection<ExamesPendentes> ExamesPendentes { get; set; } = new List<ExamesPendentes>();

    public virtual ICollection<ExamesRealizados> ExamesRealizados { get; set; } = new List<ExamesRealizados>();

    public virtual ICollection<ExamesRealizadosAM> ExamesRealizadosAM { get; set; } = new List<ExamesRealizadosAM>();

    public virtual ICollection<FichasInternas> FichasInternas { get; set; } = new List<FichasInternas>();

    public virtual ICollection<FichasLotes> FichasLotes { get; set; } = new List<FichasLotes>();

    public virtual ICollection<FichasPlanilhas> FichasPlanilhas { get; set; } = new List<FichasPlanilhas>();

    public virtual ICollection<ItensExamesRealizados> ItensExamesRealizados { get; set; } = new List<ItensExamesRealizados>();

    public virtual ICollection<ItensExamesRealizadosAM> ItensExamesRealizadosAM { get; set; } = new List<ItensExamesRealizadosAM>();

    public virtual ICollection<Requisitar> Requisitar { get; set; } = new List<Requisitar>();
}
