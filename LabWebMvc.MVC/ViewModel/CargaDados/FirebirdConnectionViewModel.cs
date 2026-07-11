using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel.CargaDados
{
    public class FirebirdConnectionViewModel
    {
        [Required(ErrorMessage = "Servidor é obrigatório")]
        [Display(Name = "Servidor")]
        public string Servidor { get; set; } = "localhost";

        [Display(Name = "Porta")]
        public int Porta { get; set; } = 3051;

        [Required(ErrorMessage = "Caminho do banco é obrigatório")]
        [Display(Name = "Caminho do banco (.FDB)")]
        public string CaminhoBanco { get; set; } = string.Empty;

        [Required(ErrorMessage = "Usuário é obrigatório")]
        [Display(Name = "Usuário")]
        public string Usuario { get; set; } = "SYSDBA";

        [Required(ErrorMessage = "Senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Display(Name = "Tamanho do lote")]
        [Range(100, 10000, ErrorMessage = "O lote deve estar entre 100 e 10.000 registros")]
        public int TamanhoLote { get; set; } = 1000;

        [Display(Name = "Charset")]
        public string Charset { get; set; } = "NONE";

        [Display(Name = "Usar conexão ODBC (igual ao Delphi)")]
        public bool UsarODBC { get; set; }

        [Display(Name = "Nome do DSN")]
        public string NomeDSN { get; set; } = "DSN FIREBIRD Lab-Web7";

        [Display(Name = "Modo simulação (apenas valida, não grava)")]
        public bool ModoSimulacao { get; set; } = true;
    }
}
