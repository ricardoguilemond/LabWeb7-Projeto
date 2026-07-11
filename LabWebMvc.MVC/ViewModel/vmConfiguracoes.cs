namespace LabWebMvc.MVC.ViewModel
{
    public class vmConfiguracoes
    {
        public int Id { get; set; }
        public string? ImpressoraCupom1 { get; set; }
        public string? ImpressoraCupom2 { get; set; }
        public string? ImpressoraCupom3 { get; set; }
        public int UsarImpressoraCupom1 { get; set; }
        public int UsarImpressoraCupom2 { get; set; }
        public int UsarImpressoraCupom3 { get; set; }
        public string? FonteNome { get; set; }
        public int FonteTamanho { get; set; }
        public int LarguraPapel { get; set; }
        public int AlturaPapel { get; set; }
        public int MargemEsquerda { get; set; }
        public int MargemDireita { get; set; }
        public int MargemSuperior { get; set; }
        public int MargemInferior { get; set; }
        /// <summary>
        /// Tipo de corte ESC/POS no final do cupom:
        /// 0 = Nenhum, 1 = Parcial, 2 = Total
        /// </summary>
        public int TipoCorteCupom { get; set; }
    }
}
