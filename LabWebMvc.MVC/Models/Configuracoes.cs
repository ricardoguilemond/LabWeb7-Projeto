namespace LabWebMvc.MVC.Models
{
    public partial class Configuracoes
    {
        public int Id { get; set; }    
        public string? ImpressoraCupom1 { get; set; }
        public string? ImpressoraCupom2 { get; set; }
        public string? ImpressoraCupom3 { get; set; }
        public int UsarImpressoraCupom1 { get; set; }
        public int UsarImpressoraCupom2 { get; set; }
        public int UsarImpressoraCupom3 { get; set; }
        public string FonteNome { get; set; } = "Consolas";    
        public int FonteTamanho { get; set; } = 8;
        public int LarguraPapel { get; set; } = 283;    
        public int AlturaPapel { get; set; } = 32767;    //Máximo suportado pela maioria das impressoras térmicas
        public int MargemEsquerda { get; set; } = 5;    
        public int MargemDireita { get; set; } = 5; 
        public int MargemSuperior { get; set; } = 5;    
        public int MargemInferior { get; set; } = 5;    

    }
}
