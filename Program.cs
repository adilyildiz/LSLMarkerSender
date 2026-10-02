using System;
using System.Windows.Forms;

namespace LSLMarkerSender
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Extract embedded lsl.dll and register the native library resolver
            // MUST be called before any LSL types are referenced!
            NativeLibraryLoader.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new MainForm());
        }
    }
}
