using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmPlanoExames
    {
        public int Id { get; set; }
        public int ExameId { get; set; }   //Código na Folha de Exames
        public int CitoInstituicao { get; set; }

        [MaxLength(60)]
        public string? CitoTituloFolha { get; set; }

        public int CitoTituloExame { get; set; }

        [MaxLength(100)]
        public string? CitoParteDescricao { get; set; }

        public byte[]? CitoDescricao { get; set; }

        [MaxLength(50)]
        [Required(ErrorMessage = "<div class='has-error'>Nome da Folha de Exames é obrigatório</div>")]
        public string RefExame { get; set; } = null!;

        [MaxLength(50)]
        [Required(ErrorMessage = "<div class='has-error'>A descrição do exame principal é obrigatória</div>")]
        public string RefItem { get; set; } = null!;

        public int TabelaExamesId { get; set; }
        public string ContaExame { get; set; } = null!;

        [MaxLength(50)]
        [Required(ErrorMessage = "<div class='has-error'>A descrição do item de exame é necessária</div>")]
        public string Descricao { get; set; } = null!;

        //[DataType(DataType.Currency)]
        //[DisplayFormat(DataFormatString = "{0:n0}")]
        public decimal? ValorCusto { get; set; }

        //[DataType(DataType.Currency)]
        //[DisplayFormat(DataFormatString = "{0:n0}")]
        public decimal? ValorItem { get; set; }

        public string? TABELACH { get; set; }
        public int QCH { get; set; }
        public decimal? ICH { get; set; }

        [MaxLength(20)]
        public string? UnidadeMedida { get; set; }

        [MaxLength(60)]
        public string? Referencia { get; set; }

        public int Etiqueta { get; set; }
        public int Etiquetas { get; set; }
        public byte[]? Laudo { get; set; }
        public int AlinhaLaudo { get; set; }
        public int Seleciona { get; set; }             //se é exame de rotina (1=rotina, 0=não é rotina)
        public int NaoMostrar { get; set; }            //se mostra ou não para lançamento na recepção

        [MaxLength(6)]
        public string? MapaHorizontal { get; set; }    //Sinonímia/sigla do exame para o mapa

        public decimal? ResultadoMinimo { get; set; }
        public decimal? ResultadoMaximo { get; set; }

        [MaxLength(20)]
        public string? LaboratorioExterno { get; set; }
        public int? PrazoResultadoDias { get; set; }

        /*
         * Campos Auxiliares
         */
        public int TipoConta { get; set; }
        public int TipoContaExame { get; set; }
        public int ItemFolhaExame { get; set; }

        /*
         * vm externos
         */
        public virtual vmGeral vmGeral { get; set; } = null!;

        public virtual vmPlanoExamesSumario VmPlanoExamesSumario { get; set; } = null!;

        //Para itens de select list
        public List<SelectListItem> Item1 { get; set; } = new();
        public List<SelectListItem> Item2 { get; set; } = new();
        public List<SelectListItem> Item3 { get; set; } = new();

        public List<SelectListItem> FolhaIdList { get; set; } = new();
        public List<SelectListItem> FolhaNomeList { get; set; } = new();

        public List<SelectListItem> TabelaIdList { get; set; } = new();
        public List<SelectListItem> TabelaNomeList { get; set; } = new();

    }
}