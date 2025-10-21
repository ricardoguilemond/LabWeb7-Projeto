using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class Pacientes
{
    public int Id { get; set; }

    public string? IdPacienteExterno { get; set; }

    public string NomePaciente { get; set; } = null!;

    public string? NomeSocial { get; set; }

    public string? NomePai { get; set; }

    public string? NomeMae { get; set; }

    public DateTime Nascimento { get; set; }

    public string? CPF { get; set; }

    public int TipoDocumento { get; set; }

    public string? Identidade { get; set; }

    public int Emissor { get; set; }

    public string? CarteiraSUS { get; set; }

    public int EstadoCivil { get; set; }

    public string? Sexo { get; set; }

    public string? Cor { get; set; }

    public string? EtniaIndigena { get; set; }

    public string? TipoSanguineo { get; set; }

    public DateTime? DUM { get; set; }

    public int TempoGestacao { get; set; }

    public string? Profissao { get; set; }

    public string? Naturalidade { get; set; }

    public string? Nacionalidade { get; set; }

    public DateTime? DataEntradaBrasil { get; set; }

    public string? Logradouro { get; set; }

    public string? Endereco { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? UF { get; set; }

    public string? CEP { get; set; }

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public string? Observacao { get; set; }

    public DateTime DataEntrada { get; set; }

    public DateTime? DataBaixa { get; set; }

    public int StatusBaixa { get; set; }

    public DateTime DataRegistro { get; set; }

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
