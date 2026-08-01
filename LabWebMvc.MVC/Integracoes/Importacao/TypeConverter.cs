using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.Utils;
using NpgsqlTypes;
using System.Data;
using System.Globalization;
using System.Text;

namespace LabWebMvc.MVC.Integracoes.Importacao
{
    public interface ITypeConverter
    {
        object? Converter(object? valorOrigem, string tipoFirebird, string tipoPostgreSQL, int? tamanhoPostgreSQL, out string? aviso, string nomeColunaDestino = "", string? valorPadrao = null, bool nullable = true);
    }

    public class TypeConverter : ITypeConverter
    {
        private readonly IEventLogHelper _eventLog;

        public TypeConverter(IEventLogHelper eventLog)
        {
            _eventLog = eventLog;
        }

        public object? Converter(object? valorOrigem, string tipoFirebird, string tipoPostgreSQL, int? tamanhoPostgreSQL, out string? aviso, string nomeColunaDestino = "", string? valorPadrao = null, bool nullable = true)
        {
            aviso = null;

            string fb = tipoFirebird.ToUpperInvariant();
            string pg = tipoPostgreSQL.ToUpperInvariant();
            bool ehColunaEmissor = nomeColunaDestino.Equals("Emissor", StringComparison.OrdinalIgnoreCase);

            try
            {
                if (fb.Contains("BLOB SUB_TYPE TEXT"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value)
                        return ValorNulo(nullable, string.Empty, out aviso, "coluna blob text");
                    return ConverterTextBlobParaText(valorOrigem, tamanhoPostgreSQL, out aviso);
                }

                if (fb.Contains("BLOB"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value)
                        return ValorNulo(nullable, Array.Empty<byte>(), out aviso, "coluna blob");
                    return ConverterBlobParaBytea(valorOrigem);
                }

                if (pg.Contains("VARCHAR") || pg.Contains("TEXT"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value || string.IsNullOrWhiteSpace(valorOrigem.ToString()))
                        return ValorNulo(nullable, "Nulo", out aviso, "coluna string");

                    return ConverterParaString(valorOrigem, tamanhoPostgreSQL, out aviso);
                }

                if (pg == "CHAR" || pg.Contains("CHAR("))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value || string.IsNullOrWhiteSpace(valorOrigem.ToString()))
                        return ValorNulo(nullable, " ", out aviso, "coluna char");

                    return ConverterParaChar(valorOrigem, tamanhoPostgreSQL, out aviso);
                }

                if (pg.Contains("INT") || pg == "SMALLINT" || pg == "BIGINT" || pg == "SERIAL" || pg.Contains("SERIAL"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value || string.IsNullOrWhiteSpace(valorOrigem.ToString()))
                    {
                        var padraoInt = ExtrairInteiroDoDefault(valorPadrao) ?? 0;
                        aviso = $"Valor nulo/vazio preenchido com {padraoInt} para coluna numérica inteira";
                        return padraoInt;
                    }

                    // Conversão especial para a coluna Emissor: Firebird guarda o nome do órgão emissor,
                    // enquanto o PostgreSQL guarda o índice numérico correspondente.
                    if (ehColunaEmissor && valorOrigem is string strEmissor)
                    {
                        var indice = Utils.IndicePorOrgaoEmissor(strEmissor);
                        aviso = $"Emissor '{strEmissor}' mapeado para índice {indice}";
                        return indice;
                    }

                    return ConverterParaInteiro(valorOrigem, out aviso);
                }

                if (pg.Contains("NUMERIC") || pg.Contains("DECIMAL") || pg.Contains("DOUBLE") || pg.Contains("REAL") || pg.Contains("FLOAT"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value || string.IsNullOrWhiteSpace(valorOrigem.ToString()))
                    {
                        aviso = "Valor nulo/vazio preenchido com 0 para coluna numérica decimal";
                        return 0m;
                    }

                    return ConverterParaDecimal(valorOrigem);
                }

                if (pg.Contains("TIMESTAMP"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value || string.IsNullOrWhiteSpace(valorOrigem.ToString()))
                    {
                        aviso = "Valor nulo/vazio preenchido com '1900-01-01' para coluna timestamp";
                        return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).ToUniversalTime();
                    }

                    return ConverterParaTimestamp(valorOrigem, out aviso);
                }

                if (pg.Contains("DATE") && !pg.Contains("TIMESTAMP"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value || string.IsNullOrWhiteSpace(valorOrigem.ToString()))
                    {
                        aviso = "Valor nulo/vazio preenchido com '1900-01-01' para coluna date";
                        return new DateTime(1900, 1, 1);
                    }

                    return ConverterParaDate(valorOrigem);
                }

                if (pg.Contains("TIME") && !pg.Contains("TIMESTAMP"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value || string.IsNullOrWhiteSpace(valorOrigem.ToString()))
                    {
                        aviso = "Valor nulo/vazio preenchido com '00:00:00' para coluna time";
                        return TimeSpan.Zero;
                    }

                    return ConverterParaTime(valorOrigem);
                }

                if (pg.Contains("BOOL"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value || string.IsNullOrWhiteSpace(valorOrigem.ToString()))
                    {
                        aviso = "Valor nulo/vazio preenchido com 'false' para coluna boolean";
                        return false;
                    }

                    return ConverterParaBoolean(valorOrigem);
                }

                if (pg.Contains("BYTEA"))
                {
                    if (valorOrigem == null || valorOrigem == DBNull.Value)
                        return ValorNulo(nullable, Array.Empty<byte>(), out aviso, "coluna bytea");
                    return ConverterBlobParaBytea(valorOrigem);
                }

                // Fallback: retorna o valor original
                return valorOrigem;
            }
            catch (Exception ex)
            {
                aviso = $"Erro na conversão de '{tipoFirebird}' -> '{tipoPostgreSQL}': {ex.Message}";
                _eventLog.LogEventViewer($"[CargaDados] {aviso}", "wError");
                return ObterValorPadraoPorTipo(pg, nullable, ref aviso);
            }
        }

        private static object? ValorNulo(bool nullable, object valorPadrao, out string? aviso, string descricaoTipo)
        {
            aviso = null;

            if (nullable)
                return DBNull.Value;

            aviso = $"Valor nulo/vazio preenchido com '{valorPadrao}' para {descricaoTipo} NOT NULL";
            return valorPadrao;
        }

        private static object? ObterValorPadraoPorTipo(string tipoPostgreSQL, bool nullable, ref string? aviso)
        {
            var pg = tipoPostgreSQL.ToUpperInvariant();

            // Para tipos não-string, sempre usa um valor padrão seguro, mesmo em colunas nullable,
            // evitando erros de conversão e violações de NOT NULL.
            if (pg.Contains("INT") || pg == "SMALLINT" || pg == "BIGINT" || pg == "SERIAL" || pg.Contains("SERIAL"))
                return ExtrairInteiroDoDefault(null) ?? 0;

            if (pg.Contains("NUMERIC") || pg.Contains("DECIMAL") || pg.Contains("DOUBLE") || pg.Contains("REAL") || pg.Contains("FLOAT"))
                return 0m;

            if (pg.Contains("TIMESTAMP"))
                return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).ToUniversalTime();

            if (pg.Contains("DATE") && !pg.Contains("TIMESTAMP"))
                return new DateTime(1900, 1, 1);

            if (pg.Contains("TIME") && !pg.Contains("TIMESTAMP"))
                return TimeSpan.Zero;

            if (pg.Contains("BOOL"))
                return false;

            if (nullable)
                return DBNull.Value;

            pg = tipoPostgreSQL.ToUpperInvariant();

            if (pg.Contains("VARCHAR") || pg.Contains("CHAR") || pg.Contains("TEXT"))
                return "Nulo";

            if (pg.Contains("INT") || pg == "SMALLINT" || pg == "BIGINT" || pg == "SERIAL" || pg.Contains("SERIAL"))
                return 0;

            if (pg.Contains("NUMERIC") || pg.Contains("DECIMAL") || pg.Contains("DOUBLE") || pg.Contains("REAL") || pg.Contains("FLOAT"))
                return 0m;

            if (pg.Contains("TIMESTAMP"))
                return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).ToUniversalTime();

            if (pg.Contains("DATE") && !pg.Contains("TIMESTAMP"))
                return new DateTime(1900, 1, 1);

            if (pg.Contains("TIME") && !pg.Contains("TIMESTAMP"))
                return TimeSpan.Zero;

            if (pg.Contains("BOOL"))
                return false;

            return DBNull.Value;
        }

        private static object? ConverterParaString(object valor, int? tamanhoMaximo, out string? aviso)
        {
            aviso = null;
            string texto = valor is byte[] bytes
                ? ConverterBytesWin1252ParaString(bytes)
                : SanitizarStringWin1252((valor.ToString() ?? string.Empty).Replace("\0", string.Empty));

            if (tamanhoMaximo.HasValue && tamanhoMaximo.Value > 0 && texto.Length > tamanhoMaximo.Value)
            {
                aviso = $"Valor truncado de {texto.Length} para {tamanhoMaximo.Value} caracteres";
                texto = texto.Substring(0, tamanhoMaximo.Value);
            }

            return texto;
        }

        private static object? ConverterParaChar(object valor, int? tamanhoMaximo, out string? aviso)
        {
            aviso = null;
            string texto = valor is byte[] bytes
                ? ConverterBytesWin1252ParaString(bytes)
                : SanitizarStringWin1252((valor.ToString() ?? string.Empty).Replace("\0", string.Empty).TrimEnd());

            if (tamanhoMaximo.HasValue && tamanhoMaximo.Value > 0 && texto.Length > tamanhoMaximo.Value)
            {
                aviso = $"Valor truncado de {texto.Length} para {tamanhoMaximo.Value} caracteres";
                texto = texto.Substring(0, tamanhoMaximo.Value);
            }

            if (tamanhoMaximo == 1 && texto.Length > 0)
                return texto[0];

            return texto.PadRight(tamanhoMaximo ?? texto.Length, ' ');
        }

        private static object ConverterParaInteiro(object valor, out string? aviso)
        {
            aviso = null;

            if (valor is int i) return i;
            if (valor is long l) return l;
            if (valor is short s) return s;
            if (valor is decimal dec)
            {
                var truncado = (long)dec;
                if (dec != truncado)
                    aviso = $"Valor decimal {dec} truncado para inteiro {truncado}";
                return truncado;
            }
            if (valor is double d)
            {
                var truncado = (long)d;
                if (Math.Abs(d - truncado) > double.Epsilon)
                    aviso = $"Valor double {d} truncado para inteiro {truncado}";
                return truncado;
            }
            if (valor is float f)
            {
                var truncado = (long)f;
                if (Math.Abs(f - truncado) > float.Epsilon)
                    aviso = $"Valor float {f} truncado para inteiro {truncado}";
                return truncado;
            }
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
                if (str == "1" ||
                    str.Equals("T", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("S", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("YES", StringComparison.OrdinalIgnoreCase) ||
                    str.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    return true;
                return false;
            }
            if (valor is int i) return i != 0;
            if (valor is long l) return l != 0;
            if (valor is short s) return s != 0;
            if (valor is decimal dec) return dec != 0;
            if (valor is double d) return d != 0;
            if (valor is float f) return f != 0;

            return Convert.ToBoolean(valor);
        }

        private static int? ExtrairInteiroDoDefault(string? valorPadrao)
        {
            if (string.IsNullOrWhiteSpace(valorPadrao))
                return null;

            // Defaults PostgreSQL costumam vir como: '1', '0'::bigint, nextval(...), etc.
            // Tenta extrair o primeiro número inteiro encontrado.
            var partes = valorPadrao.Split('\'');
            foreach (var parte in partes)
            {
                var limpo = parte.Trim().Split(':', ' ')[0];
                if (int.TryParse(limpo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resultado))
                    return resultado;
            }

            return null;
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

            // Firebird nao armazena timezone. Datas sem timezone sao tratadas como UTC
            // para evitar deslocamentos causados por conversao Local -> UTC.
            if (data.Kind == DateTimeKind.Unspecified)
                data = DateTime.SpecifyKind(data, DateTimeKind.Utc);

            // Npgsql 8+ exige DateTime com Kind=Utc para timestamptz.
            var resultado = data.ToUniversalTime();

            // Log de diagnostico para datas suspeitas (muito antigas).
            if (resultado.Year < 1800)
            {
                var originalStr = valor?.ToString() ?? "NULL";
                aviso = $"Data suspeita convertida: origem='{originalStr}', Kind='{data.Kind}', resultado='{resultado:O}'";
            }

            return resultado;
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
                // Tenta UTF8 primeiro; se houver bytes inválidos, tenta WIN1252 (Windows-1252)
                // que é o charset usado pelo sistema Delphi no Firebird.
                try
                {
                    texto = Encoding.UTF8.GetString(bytes);
                    // Se houver caractere de substituição, pode ser WIN1252 mal interpretado
                    if (texto.Contains('\uFFFD'))
                        texto = Encoding.GetEncoding("Windows-1252").GetString(bytes);
                }
                catch
                {
                    texto = Encoding.GetEncoding("Windows-1252").GetString(bytes);
                }
            }
            else
            {
                texto = SanitizarStringWin1252(valor?.ToString() ?? string.Empty);
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

        /// <summary>
        /// Converte um array de bytes WIN1252 para string UTF-16 (.NET).
        /// Usado quando a coluna Firebird é lida com CHARACTER SET OCTETS.
        /// </summary>
        private static string ConverterBytesWin1252ParaString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding("Windows-1252").GetString(bytes).Replace("\0", string.Empty);
            }
            catch
            {
                // Fallback: interpreta como ISO-8859-1 se Windows-1252 nao estiver disponivel
                return Encoding.GetEncoding("ISO-8859-1").GetString(bytes).Replace("\0", string.Empty);
            }
        }

        /// <summary>
        /// Sanitiza strings lidas do Firebird com charset NONE.
        /// Com charset NONE, o driver retorna bytes crus como chars Unicode 0-255 (Latin-1/ISO-8859-1).
        /// Este método reconverte os bytes para Windows-1252 (WIN1252), que é o charset usado pelo
        /// sistema Delphi, garantindo que caracteres especiais como ², ³, µ, ±, ½, ç, ã, é, €, ™, etc.
        /// sejam preservados corretamente.
        /// Também substitui caracteres de substituição (U+FFFD) por '?'.
        /// </summary>
        private static string SanitizarStringWin1252(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            // Fast-path: se não há chars no range 0x80-0x9F nem U+FFFD, a string já está correta.
            // (chars 0xA0-0xFF são idênticos em ISO-8859-1 e Windows-1252, não precisam de conversão)
            bool precisaConversao = false;
            foreach (char c in texto)
            {
                if (c == '\uFFFD' || (c >= '\u0080' && c <= '\u009F'))
                {
                    precisaConversao = true;
                    break;
                }
            }

            if (!precisaConversao)
                return texto;

            // Converte a string .NET (chars 0-255 = Latin-1) de volta para bytes,
            // depois interpreta os bytes como Windows-1252.
            // Isso corrige os chars 0x80-0x9F que são diferentes entre ISO-8859-1 e Windows-1252.
            try
            {
                // Registra o provider de code pages para .NET 8 (necessário para Windows-1252 e ISO-8859-1)
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var iso88591 = Encoding.GetEncoding("ISO-8859-1");
                var win1252 = Encoding.GetEncoding("Windows-1252");

                // Separa chars conversíveis (0-255) de chars Unicode altos (> 255)
                var sb = new StringBuilder(texto.Length);
                var buffer = new byte[1];
                foreach (char c in texto)
                {
                    if (c == '\uFFFD')
                    {
                        sb.Append('?');
                    }
                    else if (c <= 255)
                    {
                        // Char Latin-1: converte para byte e reinterpretta como Windows-1252
                        buffer[0] = (byte)c;
                        sb.Append(win1252.GetString(buffer));
                    }
                    else
                    {
                        // Char Unicode alto (já correto): preserva sem conversão
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }
            catch
            {
                // Fallback: se Encoding.GetEncoding não estiver disponível, usa mapeamento manual
                var sb = new StringBuilder(texto.Length);
                foreach (char c in texto)
                {
                    if (c == '\uFFFD')
                        sb.Append('?');
                    else if (c >= '\u0080' && c <= '\u009F')
                        sb.Append(c switch
                        {
                            '\u0080' => '\u20AC', // €
                            '\u0082' => '\u201A', // ‚
                            '\u0083' => '\u0192', // ƒ
                            '\u0084' => '\u201E', // „
                            '\u0085' => '\u2026', // …
                            '\u0086' => '\u2020', // †
                            '\u0087' => '\u2021', // ‡
                            '\u0088' => '\u02C6', // ˆ
                            '\u0089' => '\u2030', // ‰
                            '\u008A' => '\u0160', // Š
                            '\u008B' => '\u2039', // ‹
                            '\u008C' => '\u0152', // Œ
                            '\u008E' => '\u017D', // Ž
                            '\u0091' => '\u2018', // '
                            '\u0092' => '\u2019', // '
                            '\u0093' => '\u201C', // "
                            '\u0094' => '\u201D', // "
                            '\u0095' => '\u2022', // •
                            '\u0096' => '\u2013', // –
                            '\u0097' => '\u2014', // —
                            '\u0098' => '\u02DC', // ˜
                            '\u0099' => '\u2122', // ™
                            '\u009A' => '\u0161', // š
                            '\u009B' => '\u203A', // ›
                            '\u009C' => '\u0153', // œ
                            '\u009E' => '\u017E', // ž
                            '\u009F' => '\u0178', // Ÿ
                            _ => c
                        });
                    else
                        sb.Append(c);
                }
                return sb.ToString();
            }
        }
    }
}
