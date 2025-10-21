using LabWebMvc.MVC.Models;
using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmTabelaExames
    {
        public int Id { get; set; }

        [StringLength(20, ErrorMessage = "<div class='has-error'>Precisa ter o mínimo de 3 e máximo de {1} caracteres</div>", MinimumLength = 3)]
        [Required(ErrorMessage = "<div class='has-error'>Entre com a sigla da tabela de exames</div>"), MaxLength(20)]
        public string SiglaTabela { get; set; } = null!;

        [StringLength(100, ErrorMessage = "<div class='has-error'>Precisa ter o mínimo de 3 e máximo de {1} caracteres</div>", MinimumLength = 3)]
        [Required(ErrorMessage = "<div class='has-error'>Entre com o nome da tabela de exames</div>"), MaxLength(100)]
        public string NomeTabela { get; set; } = null!;

        public int Orcamento { get; set; }

        public int Bloqueado { get; set; }

        //public TabelaExames? TabelaExames { get; set; }

        /* Listas */
        public virtual ICollection<ExamesExportados> ExamesExportados { get; set; } = [];
        public virtual ICollection<ExamesImpressos> ExamesImpressos { get; set; } = [];
        public virtual ICollection<ExamesPendentes> ExamesPendentes { get; set; } = [];
        public virtual ICollection<ExamesRealizados> ExamesRealizados { get; set; } = [];
        public virtual ICollection<FichasLotes> FichasLotes { get; set; } = [];
        public virtual ICollection<FichasPlanilhas> FichasPlanilhas { get; set; } = [];
        public virtual ICollection<ItensExamesRealizados> ItensExamesRealizados { get; set; } = [];
    }
}