using BLL;

namespace LabWebMvc.MVC.Areas.Servicos
{
    /* Feito pelo Qoder em 22/08/2026 — implementação do IGeralService (Dívida Técnica §1, opção A).
     * Serviço puro: mesma semântica dos métodos que existiam no GeralController, sem herdar de Controller. */
    public class GeralService : IGeralService
    {
        private readonly ITempoServidorService _tempoService;

        public GeralService(ITempoServidorService tempoService)
        {
            _tempoService = tempoService;
        }

        public string ObterDataHoraServidor(bool iso = false)
        {
            if (iso)
                return _tempoService.ObterDataHoraServidor("iso");  //yyyy/mm/ddTHH:mm:ss.fffZ
            else
                return _tempoService.ObterDataHoraServidor();       //dd/mm/yyyy HH:mm:ss
        }

        public DateTime ObterDataHoraUtc()
        {
            return _tempoService.ObterDataHoraUtc();
        }

        public DateTime ObterDataHoraLocal()
        {
            var utc = _tempoService.ObterDataHoraUtc();
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        }

        public (DateTime inicioUtc, DateTime fimUtc) ObterRangeDiaUtc()
        {
            var utcAgora = _tempoService.ObterDataHoraUtc();
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var hojeLocal = TimeZoneInfo.ConvertTimeFromUtc(utcAgora, tz).Date; // meia-noite local
            return ConverterDataLocalParaRangeUtc(hojeLocal);
        }

        public (DateTime inicioUtc, DateTime fimUtc) ConverterDataLocalParaRangeUtc(DateTime dataLocal)
        {
            // Se já é UTC, extrai a data e calcula o range diretamente
            if (dataLocal.Kind == DateTimeKind.Utc)
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                var dataLocalBr = TimeZoneInfo.ConvertTimeFromUtc(dataLocal, tz).Date;
                var offset = tz.GetUtcOffset(dataLocalBr);
                var inicioUtc = new DateTimeOffset(dataLocalBr, offset).UtcDateTime;
                var fimUtc = new DateTimeOffset(dataLocalBr.AddDays(1).AddTicks(-1), offset).UtcDateTime;
                return (inicioUtc, fimUtc);
            }

            // Kind=Unspecified ou Kind=Local: trata como horário de Brasília
            var tz2 = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var data = dataLocal.Date; // garante meia-noite
            var offset2 = tz2.GetUtcOffset(data);
            var inicioUtc2 = new DateTimeOffset(data, offset2).UtcDateTime;
            var fimUtc2 = new DateTimeOffset(data.AddDays(1).AddTicks(-1), offset2).UtcDateTime;
            return (inicioUtc2, fimUtc2);
        }

        public DateTime ConverterLocalParaUtc(DateTime dataLocal)
        {
            // Se já é UTC, retorna sem conversão (evita dobrar o offset)
            if (dataLocal.Kind == DateTimeKind.Utc)
                return dataLocal;

            // Kind=Unspecified ou Kind=Local: trata como horário de Brasília e converte para UTC
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var offset = tz.GetUtcOffset(dataLocal);
            return new DateTimeOffset(dataLocal, offset).UtcDateTime;
        }
    }
}
