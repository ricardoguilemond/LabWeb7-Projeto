using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace BLL
{
    public static class PaperTypes
    {
        public static readonly string A0 = "A0";
        public static readonly string A1 = "A1";
        public static readonly string A2 = "A2";
        public static readonly string A3 = "A3";
        public static readonly string A4 = "A4";
        public static readonly string A5 = "A5";
        public static readonly string A6 = "A6";
        public static readonly string A7 = "A7";
        public static readonly string A8 = "A8";
        public static readonly string A9 = "A9";

        public static readonly string B0 = "B0";
        public static readonly string B1 = "B1";
        public static readonly string B2 = "B2";
        public static readonly string B3 = "B3";
        public static readonly string B4 = "B4";
        public static readonly string B5 = "B5";
        public static readonly string B6 = "B6";
        public static readonly string B7 = "B7";
        public static readonly string B8 = "B8";
        public static readonly string B9 = "B9";
        public static readonly string B10 = "B10";

        public static readonly string C5E = "C5E";
        public static readonly string Comm10E = "Comm10E";
        public static readonly string DLE = "DLE";
        public static readonly string Executive = "Executive";
        public static readonly string Folio = "Folio";
        public static readonly string Ledger = "Ledger";
        public static readonly string Legal = "Legal";
        public static readonly string Letter = "Letter";
        public static readonly string Tabloid = "Tabloid";
    }

    public class PdfConvertException : Exception
    {
        public PdfConvertException(string msg) : base(msg)
        {
        }
    }

    public class PdfConvertTimeoutException : PdfConvertException
    {
        public PdfConvertTimeoutException() : base("Ocorreu exceção por longo período e não finalizou a conversão de HTML para PDF.")
        {
        }
    }

    public class PdfOutput
    {
        public string? OutputFilePath { get; set; }
        public Stream? OutputStream { get; set; }
        public Action<PdfDocument, byte[]>? OutputCallback { get; set; }
    }

    public class PdfDocument
    {
        public string? PaperType { get; set; }
        public string? Url { get; set; }
        public string? Html { get; set; }
        public string? HeaderUrl { get; set; }
        public string? FooterUrl { get; set; }
        public string? HeaderLeft { get; set; }
        public string? HeaderCenter { get; set; }
        public string? HeaderRight { get; set; }
        public string? FooterLeft { get; set; }
        public string? FooterCenter { get; set; }
        public string? FooterRight { get; set; }
        public object? State { get; set; }
        public Dictionary<string, string>? Cookies { get; set; }
        public Dictionary<string, string>? ExtraParams { get; set; }
        public string? HeaderFontSize { get; set; }
        public string? FooterFontSize { get; set; }
        public string? HeaderFontName { get; set; }
        public string? FooterFontName { get; set; }
    }

    public class PdfConvertEnvironment
    {
        public string? TempFolderPath { get; set; }
        public string? WkHtmlToPdfPath { get; set; }
        public int Timeout { get; set; }
        public bool Debug { get; set; }
    }

    public class PdfConvert
    {
        private static PdfConvertEnvironment? _e;

        public static PdfConvertEnvironment Environment
        {
            get
            {
                if (_e == null)
                    _e = new PdfConvertEnvironment
                    {
                        TempFolderPath = Path.GetTempPath(),
                        WkHtmlToPdfPath = GetWkhtmlToPdfExeLocation(),
                        Timeout = 60000
                    };
                return _e;
            }
        }

        private static string GetWkhtmlToPdfExeLocation()
        {
            string? filePath, customPath = ConfigurationManager.AppSettings["wkhtmltopdf:path"];

            if (customPath != null)
            {
                filePath = Path.Combine(customPath, @"wkhtmltopdf.exe");
                if (File.Exists(filePath)) return filePath;
            }

            string? wwwFilesPath = System.Environment.GetEnvironmentVariable("wwwFiles");
#pragma warning disable CS8604 // Possible null reference argument.
            filePath = Path.Combine(wwwFilesPath, @"rotativa\wkhtmltox\bin\");
#pragma warning restore CS8604 // Possible null reference argument.
            if (File.Exists(filePath)) return filePath;

            string? programFilesPath = System.Environment.GetEnvironmentVariable("ProgramFiles");
#pragma warning disable CS8604 // Possible null reference argument.
            filePath = Path.Combine(programFilesPath, @"wkhtmltopdf\");
#pragma warning restore CS8604 // Possible null reference argument.
            if (File.Exists(filePath)) return filePath;

            string? programFilesx86Path = System.Environment.GetEnvironmentVariable("ProgramFiles(x86)");
#pragma warning disable CS8604 // Possible null reference argument.
            filePath = Path.Combine(programFilesx86Path, @"wkhtmltopdf\");
#pragma warning restore CS8604 // Possible null reference argument.
            if (File.Exists(filePath)) return filePath;

            filePath = Path.Combine(programFilesPath, @"wkhtmltopdf\bin\");
            if (File.Exists(filePath)) return filePath;

            return Path.Combine(wwwFilesPath, @"wkhtmltopdf.exe");
        }

        public static void ConvertHtmlToPdf(PdfDocument document, PdfOutput output)
        {
            ConvertHtmlToPdf(document, null, output);
        }

        public static void ConvertHtmlToPdf(PdfDocument document, PdfConvertEnvironment? environment, PdfOutput woutput)
        {
            if (environment == null)
                environment = Environment;

            if (document.Html != null)
                document.Url = "-";

            string outputPdfFilePath;
            bool delete;
            if (woutput.OutputFilePath != null)
            {
                outputPdfFilePath = woutput.OutputFilePath;
                delete = false;
            }
            else
            {
#pragma warning disable CS8604 // Possible null reference argument.
                outputPdfFilePath = Path.Combine(environment.TempFolderPath, string.Format("{0}.pdf", Guid.NewGuid()));
#pragma warning restore CS8604 // Possible null reference argument.
                delete = true;
            }

            if (!File.Exists(environment.WkHtmlToPdfPath))
                throw new PdfConvertException(string.Format("Arquivo '{0}' não encontrado. Verifique se << wkhtmltopdf >> está instalado.", environment.WkHtmlToPdfPath));

            StringBuilder paramsBuilder = new();

            if (string.IsNullOrEmpty(document.PaperType))
                document.PaperType = PaperTypes.A4;
            paramsBuilder.AppendFormat("--page-size {0} ", document.PaperType);

            if (!string.IsNullOrEmpty(document.HeaderUrl))
            {
                paramsBuilder.AppendFormat("--header-html {0} ", document.HeaderUrl);
                paramsBuilder.Append("--margin-top 25 ");
                paramsBuilder.Append("--header-spacing 5 ");
            }
            if (!string.IsNullOrEmpty(document.FooterUrl))
            {
                paramsBuilder.AppendFormat("--footer-html {0} ", document.FooterUrl);
                paramsBuilder.Append("--margin-bottom 25 ");
                paramsBuilder.Append("--footer-spacing 5 ");
            }
            if (!string.IsNullOrEmpty(document.HeaderLeft))
                paramsBuilder.AppendFormat("--header-left \"{0}\" ", document.HeaderLeft);

            if (!string.IsNullOrEmpty(document.HeaderCenter))
                paramsBuilder.AppendFormat("--header-center \"{0}\" ", document.HeaderCenter);

            if (!string.IsNullOrEmpty(document.HeaderRight))
                paramsBuilder.AppendFormat("--header-right \"{0}\" ", document.HeaderRight);

            if (!string.IsNullOrEmpty(document.FooterLeft))
                paramsBuilder.AppendFormat("--footer-left \"{0}\" ", document.FooterLeft);

            if (!string.IsNullOrEmpty(document.FooterCenter))
                paramsBuilder.AppendFormat("--footer-center \"{0}\" ", document.FooterCenter);

            if (!string.IsNullOrEmpty(document.FooterRight))
                paramsBuilder.AppendFormat("--footer-right \"{0}\" ", document.FooterRight);

            if (!string.IsNullOrEmpty(document.HeaderFontSize))
                paramsBuilder.AppendFormat("--header-font-size \"{0}\" ", document.HeaderFontSize);

            if (!string.IsNullOrEmpty(document.FooterFontSize))
                paramsBuilder.AppendFormat("--footer-font-size \"{0}\" ", document.FooterFontSize);

            if (!string.IsNullOrEmpty(document.HeaderFontName))
                paramsBuilder.AppendFormat("--header-font-name \"{0}\" ", document.HeaderFontName);

            if (!string.IsNullOrEmpty(document.FooterFontName))
                paramsBuilder.AppendFormat("--footer-font-name \"{0}\" ", document.FooterFontName);

            if (document.ExtraParams != null)
                foreach (KeyValuePair<string, string> extraParam in document.ExtraParams)
                    paramsBuilder.AppendFormat("--{0} {1} ", extraParam.Key, extraParam.Value);

            if (document.Cookies != null)
                foreach (KeyValuePair<string, string> cookie in document.Cookies)
                    paramsBuilder.AppendFormat("--cookie {0} {1} ", cookie.Key, cookie.Value);

            paramsBuilder.AppendFormat("\"{0}\" \"{1}\"", document.Url, outputPdfFilePath);

            try
            {
                StringBuilder output = new();
                StringBuilder error = new();

                using (Process process = new())
                {
                    process.StartInfo.FileName = environment.WkHtmlToPdfPath;
                    process.StartInfo.Arguments = paramsBuilder.ToString();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;

                    using (AutoResetEvent outputWaitHandle = new(false))
                    using (AutoResetEvent errorWaitHandle = new(false))
                    {
                        DataReceivedEventHandler outputHandler = (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                output.AppendLine(e.Data);
                            }
                        };

                        DataReceivedEventHandler errorHandler = (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                error.AppendLine(e.Data);
                            }
                        };

                        process.OutputDataReceived += outputHandler;
                        process.ErrorDataReceived += errorHandler;

                        try
                        {
                            process.Start();

                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();

                            if (document.Html != null)
                            {
                                using (StreamWriter stream = process.StandardInput)
                                {
                                    byte[] buffer = Encoding.UTF8.GetBytes(document.Html);
                                    stream.BaseStream.Write(buffer, 0, buffer.Length);
                                    stream.WriteLine();
                                }
                            }

                            if (process.WaitForExit(environment.Timeout) && outputWaitHandle.WaitOne(environment.Timeout) && errorWaitHandle.WaitOne(environment.Timeout))
                            {
                                if (process.ExitCode != 0 && !File.Exists(outputPdfFilePath))
                                {
                                    throw new PdfConvertException(string.Format("Conversão Html to PDF de '{0}' falhou. << Wkhtmltopdf >> saída: \r\n{1}", document.Url, error));
                                }
                            }
                            else
                            {
                                if (!process.HasExited)
                                    process.Kill();

                                throw new PdfConvertTimeoutException();
                            }
                        }
                        finally
                        {
                            process.OutputDataReceived -= outputHandler;
                            process.ErrorDataReceived -= errorHandler;
                        }
                    }
                }

                if (woutput.OutputStream != null)
                {
                    using (Stream fs = new FileStream(outputPdfFilePath, FileMode.Open))
                    {
                        byte[] buffer = new byte[32 * 1024];
                        int read;

                        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                            woutput.OutputStream.Write(buffer, 0, read);
                    }
                }

                if (woutput.OutputCallback != null)
                {
                    byte[] pdfFileBytes = File.ReadAllBytes(outputPdfFilePath);
                    woutput.OutputCallback(document, pdfFileBytes);
                }
            }
            finally
            {
                if (delete && File.Exists(outputPdfFilePath))
                    File.Delete(outputPdfFilePath);
            }
        }

        internal static void ConvertHtmlToPdf(string url, string outputFilePath)
        {
            ConvertHtmlToPdf(new PdfDocument { Url = url }, new PdfOutput { OutputFilePath = outputFilePath });
        }
    }

    //class OSUtil
    //{
    //    public static string GetProgramFilesx86Path()
    //    {
    //        if (8 == IntPtr.Size || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
    //        {
    //            return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
    //        }
    //        return Environment.GetEnvironmentVariable("ProgramFiles");
    //    }
    //}

    //public static class HttpResponseExtensions
    //{
    //    public static void SendFileForDownload(this HttpResponse response, String filename, byte[] content)
    //    {
    //        SetFileDownloadHeaders(response, filename);
    //        response.OutputStream.Write(content, 0, content.Length);
    //        response.Flush();
    //    }

    //    public static void SendFileForDownload(this HttpResponse response, String filename)
    //    {
    //        SetFileDownloadHeaders(response, filename);
    //        response.TransmitFile(filename);
    //        response.Flush();
    //    }

    //    public static void SetFileDownloadHeaders(this HttpResponse response, String filename)
    //    {
    //        FileInfo fi = new FileInfo(filename);
    //        response.ContentType = "application/force-download";
    //        response.AddHeader("Content-Disposition", "attachment; filename=\"" + fi.Name + "\"");
    //    }
    //}
}