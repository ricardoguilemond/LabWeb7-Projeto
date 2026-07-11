using System.Runtime.InteropServices;

namespace LabWebMvc.MVC.Areas.Impressoras
{
    /// <summary>
    /// Helper para envio de comandos RAW (ESC/POS) diretamente para uma impressora
    /// compartilhada no Windows, usando a API winspool.drv.
    /// </summary>
    public static class RawPrinterHelper
    {
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName = string.Empty;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType = string.Empty;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile = string.Empty;
        }

        /// <summary>
        /// Envia bytes RAW para a impressora informada.
        /// </summary>
        public static bool EnviarBytesParaImpressora(string nomeImpressora, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(nomeImpressora))
                throw new ArgumentException("Nome da impressora não informado.", nameof(nomeImpressora));

            if (bytes == null || bytes.Length == 0)
                return true;

            if (!OpenPrinter(nomeImpressora.Normalize(), out var hPrinter, IntPtr.Zero))
                return false;

            try
            {
                var di = new DOCINFOA
                {
                    pDocName = "Comando ESC/POS",
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                    return false;

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        return false;

                    try
                    {
                        var ptr = Marshal.AllocHGlobal(bytes.Length);
                        try
                        {
                            Marshal.Copy(bytes, 0, ptr, bytes.Length);
                            return WritePrinter(hPrinter, ptr, bytes.Length, out _);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                    }
                    finally
                    {
                        EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }

        /// <summary>
        /// Retorna o comando ESC/POS para corte de papel.
        /// </summary>
        /// <param name="tipoCorte">0 = nenhum, 1 = parcial, 2 = total</param>
        public static byte[] ObterComandoCorte(int tipoCorte)
        {
            return tipoCorte switch
            {
                1 => new byte[] { 0x1D, 0x56, 0x41, 0x00 }, // Corte parcial
                2 => new byte[] { 0x1D, 0x56, 0x42, 0x00 }, // Corte total
                _ => Array.Empty<byte>()
            };
        }
    }
}
