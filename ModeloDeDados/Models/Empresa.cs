using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class Empresa
{
    public int Id { get; set; }

    public int Matriz { get; set; }

    public int Filial { get; set; }

    public string Sigla { get; set; } = null!;

    public string NomeFantasia { get; set; } = null!;

    public string RazaoSocial { get; set; } = null!;

    public string CNPJ { get; set; } = null!;

    public string? Logradouro { get; set; }

    public string? Endereco { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? UF { get; set; }

    public string? CEP { get; set; }

    public string Telefones { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? SiteUrl { get; set; }

    public string? HostLogoMarca { get; set; }

    public string? UnidadeLogoMarca { get; set; }

    public string? CaminhoLogoMarca { get; set; }

    public string? NomeLogoMarca { get; set; }

    public string? TituloEmpresa { get; set; }

    public string? SubTituloEmpresa { get; set; }

    public string? Rodape { get; set; }

    public string? StringConexao { get; set; }

    public string? SmtpServer { get; set; }

    public int SmtpPortSsl { get; set; }

    public bool? SmtpRequerSsl { get; set; }

    public int SmtpPortTls { get; set; }

    public bool? SmtpRequerTls { get; set; }

    public string? SmtpUsername { get; set; }

    public string? SmtpPassword { get; set; }

    public string? SmtpName { get; set; }

    public string? SmtpSenhaApp { get; set; }

    public string? PopServer { get; set; }

    public int PopPortSsl { get; set; }

    public bool? PopRequerSsl { get; set; }

    public string? PopUsername { get; set; }

    public string? PopPassword { get; set; }

    public string? PopName { get; set; }

    public DateTime? DataExpira { get; set; }

    public DateTime DataCadastro { get; set; }
}
