using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmClasseExames
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string? RefExame { get; set; }

        [Required(ErrorMessage = "<div class='has-error'>Quantidade de etiquetas exigidas para o exame 0 a 8</div>")]
        public int Etiquetas { get; set; }

        public string? TipoMapa { get; set; }
        public int Assinatura1 { get; set; }
        public int Assinatura2 { get; set; }
        public int Assinatura3 { get; set; }
        public int Assinatura4 { get; set; }

        [Required]
        public byte[]? ImgAss1 { get; set; }

        public byte[]? ImgAss2 { get; set; }
        public byte[]? ImgAss3 { get; set; }
        public byte[]? ImgAss4 { get; set; }
        public string? NomeAss1 { get; set; }
        public string? NomeAss2 { get; set; }
        public string? NomeAss3 { get; set; }
        public string? NomeAss4 { get; set; }
        public int Marcado { get; set; }
        public int Planilha { get; set; }
        public int MHI { get; set; }   //índice que define a ordem das folhas no mapa horizontal (mapa de folha deitada/paisagem/landscape

        [StringLength(20)]
        public string? LaboratorioExterno { get; set; }  //Sigla da Instituição exclusiva da Folha criada ou vazio para uso de todas

        /*
         * Campos auxiliares
         */

        public string GetMapaTrabalho
        {
            get => (TipoMapa == "E") ? "Eletrônico/Computador" : "Ficha/Convencional";
        }

        public string GetAssinatura1
        {
            //se == 1 (=Sim)
            get => (Assinatura1 == 1) ? "S" : "N";
        }

        public string GetAssinatura2
        {
            //se == 1 (=Sim)
            get => (Assinatura2 == 1) ? "S" : "N";
        }

        public string GetAssinatura3
        {
            //se == 1 (=Sim)
            get => (Assinatura3 == 1) ? "S" : "N";
        }

        public string GetAssinatura4
        {
            //se == 1 (=Sim)
            get => (Assinatura4 == 1) ? "S" : "N";
        }

        public string? CaminhoImgAss1 { get; set; }
        public string? CaminhoImgAss2 { get; set; }
        public string? CaminhoImgAss3 { get; set; }
        public string? CaminhoImgAss4 { get; set; }
    }
}