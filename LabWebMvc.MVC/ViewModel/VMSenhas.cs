using LabWebMvc.MVC.Models;
using System.ComponentModel.DataAnnotations;
using static ExtensionsMethods.Genericos.Enumeradores;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmSenhas
    {
        public int Id { get; set; }
        public string LoginUsuario { get; set; } = null!;
        public string NomeUsuario { get; set; } = null!;
        public string NomeCompleto { get; set; } = null!;
        public string SenhaUsuario { get; set; } = null!;

        [Compare(nameof(SenhaUsuario), ErrorMessage = "Comparação: as senhas digitadas precisam ser iguais")]
        public string SenhaRepete { get; set; } = null!;

        public DateTime DataCadastro { get; set; }
        public DateTime? DataExpira { get; set; }
        public string? DataExpiraStr { get; set; }
        public byte[]? Assinatura { get; set; }
        public int? UsarAssinatura { get; set; }
        public string? NomeAssinatura { get; set; }
        public int? Bloqueado { get; set; }
        public int? Administrador { get; set; }
        public string? UsarAssinaturaStr { get; set; }
        public string? BloqueadoStr { get; set; }
        public string? Funcao { get; set; }
        public string Email { get; set; } = null!;
        public int EmailConfirmado { get; set; }

        /*
         * Variáveis de Extensão do Modelo
         */
        public string? EmailConfirmadoStr { get; set; }
        public Senhas? Senhas { get; set; }
        public int? Count { get; set; }
        public int SituacaoLogin { get; set; } = (int)TipoSituacaoLogin.SemVerificacao;
        public bool RecuperacaoDeSenha { get; set; } = false;
        public string? CPF { get; set; }
        public DateTime DataNascimento { get; set; }
        public bool BoxEnviarEmail { get; set; }
        public bool BoxGerarSenhaAutomatica { get; set; }

        /*
         * Listas de Extensão do Modelo
         */
        public ICollection<Senhas> vmListaSenhas { get; set; } = [];
        public ICollection<object> vmListaDados { get; set; } = [];

        /*
         * Variáveis Auxiliares do Login
         */
        public string CNPJEmpresa { get; set; } = null!;
        public string NomeEmpresa { get; set; } = null!;
        public string StringDeConexao { get; set; } = null!;

        /* Campos auxiliares de Imagens */
        public string? CaminhoImagemAssinatura { get; set; }
    }
}