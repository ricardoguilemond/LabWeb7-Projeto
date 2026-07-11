using ExtensionsMethods.EventViewerHelper;
using NpgsqlTypes;
using System.Data;
using System.Globalization;
using System.Text;

namespace LabWebMvc.MVC.Integracoes.Importacao
{
    public interface ITypeConverter
    {
        object? Converter(object? valorOrigem, string tipoFirebird, string tipoPostgreSQL, int? tamanhoPostgreSQL, out string? aviso);
    }

    public class TypeConverter : ITypeConverter
    {
        private readonly IEventLogHelper _eventLog;

        public TypeConverter(IEventLogHelper eventLog)
        {
            _eventLog = eventLog;
        }

        public object? Converter(object? valorOrigem, string tipoFirebird, string tipoPostgreSQL, int? tamanhoPostgreSQL, out string? aviso)
        {
            aviso = null;

            if (valorOrigem == null || valorOrigem == DBNull.Value)
                return DBNull.Value;

            try
            {
                string fb = tipoFirebird.ToUpperInvariant();
                string pg = tipoPostgreSQL.ToUpperInvariant();

                if (fb.Contains("BLOB SUB_TYPE TEXT"))
                    return ConverterTextBlobParaText(valorOrigem, tamanhoPostgreSQL, out aviso);

                if (fb.Contains("BLOB"))
                    return ConverterBlobParaBytea(valorOrigem);

                if (pg.Contains("VARCHAR") || pg.Contains("CHAR") || pg.Contains("TEXT"))
                    return ConverterParaString(valorOrigem, tamanhoPostgreSQL, out aviso);

                if (pg.Contains("INT") || pg == "SMALLINT" || pg == "BIGINT" || pg == "SERIAL" || pg.Contains("SERIAL"))
                    return ConverterParaInteiro(valorOrigem);

                if (pg.Contains("NUMERIC") || pg.Contains("DECIMAL") || pg.Contains("DOUBLE") || pg.Contains("REAL") || pg.Contains("FLOAT"))
                    return ConverterParaDecimal(valorOrigem);

                if (pg.Contains("TIMESTAMP"))
                    return ConverterParaTimestamp(valorOrigem, out aviso);

                if (pg.Contains("DATE") && !pg.Contains("TIMESTAMP"))
                    return ConverterParaDate(valorOrigem);

                if (pg.Contains("TIME") && !pg.Contains("TIMESTAMP"))
                    return ConverterParaTime(valorOrigem);

                if (pg.Contains("BOOL"))
                    return ConverterParaBoolean(valorOrigem);

                if (pg.Contains("BYTEA"))
                    return ConverterBlobParaBytea(valorOrigem);

                // Fallback: retorna o valor original
                return valorOrigem;
            }
            catch (Exception ex)
            {
                aviso = $"Erro na conversão de '{tipoFirebird}' -> '{tipoPostgreSQL}': {ex.Message}";
                _eventLog.LogEventViewer($"[CargaDados] {aviso}", "wError");
                return DBNull.Value;
            }
        }

        private static object? ConverterParaString(object valor, int? tamanhoMaximo, out string? aviso)
        {
            aviso = null;
            string texto = valor.ToString() ?? string.Empty;

            if (tamanhoMaximo.HasValue && tamanhoMaximo.Value > 0 && texto.Length > tamanhoMaximo.Value)
            {
                aviso = $"Valor truncado de {texto.Length} para {tamanhoMaximo.Value} caracteres";
                texto = texto.Substring(0, tamanhoMaximo.Value);
            }

            return texto;
        }

        private static object ConverterParaInteiro(object valor)
        {
            if (valor is int i) return i;
            if (valor is long l) return l;
            if (valor is short s) return s;
            if (valor is decimal dec) return (long)dec;
            if (valor is double d) return (long)d;
            if (valor is float f) return (long)f;
            if (valor is string str && long.TryParse(str, out var parsed)) return parsed;
            if (valor is bool b) return b ? 1 : 0;

            return Convert.ToInt64(valor);
        }

        private static object ConverterParaDecimal(object valor)
        {
            if (valor is decimal dec) return dec;
            if (valor is double d) return (decimal)d;
            if (valor is float f) return (decimal)f;
            if (valor is int i) return (decimal)i;
            if (valor is long l) return (decimal)l;
            if (valor is string str && decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;

            return Convert.ToDecimal(valor);
        }

        private static object ConverterParaBoolean(object valor)
        {
            if (valor is bool b) return b;
            if (valor is string str)
            {
                if (str == "1" || str.Equals("T", StringComparison.OrdinalIgnoreCase) || str.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || str.Equals("S", StringComparison.OrdinalIgnoreCase))
                    return true;
                return false;
            }
            if (valor is int i) return i != 0;

            return Convert.ToBoolean(valor);
        }

        private static object? ConverterParaTimestamp(object valor, out string? aviso)
        {
            aviso = null;
            DateTime data;

            if (valor is DateTime dt)
                data = dt;
            else if (valor is string str && DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                data = parsed;
            else
                data = Convert.ToDateTime(valor);

            if (data.Kind == DateTimeKind.Unspecified)
                data = DateTime.SpecifyKind(data, DateTimeKind.Local);

            // Npgsql 8+ exige DateTime com Kind=Utc para timestamptz
            return data.ToUniversalTime();
        }

        private static object ConverterParaDate(object valor)
        {
            if (valor is DateTime dt)
                return dt.Date;
            if (valor is string str && DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed.Date;

            return Convert.ToDateTime(valor).Date;
        }

        private static object ConverterParaTime(object valor)
        {
            if (valor is TimeSpan ts) return ts;
            if (valor is DateTime dt) return dt.TimeOfDay;
            if (valor is string str && TimeSpan.TryParse(str, out var parsed)) return parsed;

            return Convert.ToDateTime(valor).TimeOfDay;
        }

        private static object? ConverterTextBlobParaText(object valor, int? tamanhoMaximo, out string? aviso)
        {
            aviso = null;
            string texto;

            if (valor is byte[] bytes)
            {
                texto = Encoding.UTF8.GetString(bytes);
            }
            else
            {
                texto = valor.ToString() ?? string.Empty;
            }

            if (tamanhoMaximo.HasValue && tamanhoMaximo.Value > 0 && texto.Length > tamanhoMaximo.Value)
            {
                aviso = $"Texto BLOB truncado de {texto.Length} para {tamanhoMaximo.Value} caracteres";
                texto = texto.Substring(0, tamanhoMaximo.Value);
            }

            return texto;
        }

        private static object? ConverterBlobParaBytea(object valor)
        {
            if (valor is byte[] bytes) return bytes;
            if (valor is string str) return Encoding.UTF8.GetBytes(str);

            return DBNull.Value;
        }
    }
}
