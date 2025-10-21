using BLL;
using ExtensionsMethods.ValidadorDeSessao;

namespace LabWebMvc.MVC.Areas.ControlGeral
{
    public class GeralService : IGeralService
    {
        private readonly IValidadorDeSessao _validador;
        private readonly ITempoServidorService _tempoService;

        public GeralService(IValidadorDeSessao validador, ITempoServidorService tempoService)
        {
            _validador = validador;
            _tempoService = tempoService;
        }

        public bool SessaoValida()
            => _validador.SessaoValida();

        public string? ObterSessionUF(HttpContext httpContext)
            => httpContext.Session.GetString("SessionUF");

        public string[] GerarBotoes()
            => new[]
            {
            "<input type='submit' name='b1' id='b1' class='subbotao' value='Adicionar' onclick='alert(this.value)'/>",
            "<input type='button' name='b2' id='b2' class='subbotao' value='Excluir' onclick='alert(this.value)' />",
            "<input type='button' name='b3' id='b3' class='subbotao' value='Enviar senha por email' onclick='alert(this.value)'/>",
            "<input type='button' name='b4' id='b4' class='subbotao' value='Ver exames' onclick='alert(this.value)' />",
            "<input type='button' name='b5' id='b5' class='subbotao' value='Arquivar' onclick='alert(this.value)' />"
            };

        public Task<string> ObterDataHoraFormatadoAsync()
            => _tempoService.ObterDataHoraServidorFormatadoAsync();

        public string ObterDataHora(bool iso = false)
            => _tempoService.ObterDataHoraServidor(iso ? "iso" : null);

        public object[] GerarTextoMenu(string titulo)
            => new object[] { titulo, false };
    }
}