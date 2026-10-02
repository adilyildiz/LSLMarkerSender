using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LSLMarkerSender
{
    /// <summary>
    /// Extracts the embedded lsl.dll native library from assembly resources at startup
    /// and registers a DllImportResolver so that P/Invoke calls find it automatically.
    /// This eliminates the need to ship lsl.dll as a separate file alongside the exe.
    /// </summary>
    public static class NativeLibraryLoader
    {
        private static string _extractedDllPath;
        private static IntPtr _loadedLibHandle = IntPtr.Zero;

        /// <summary>
        /// Call this once at application startup, BEFORE any LSL types are used.
        /// Extracts lsl.dll from embedded resources to a temp directory and
        /// registers a DllImportResolver for the "lsl" library name.
        /// </summary>
        public static void Initialize()
        {
            ExtractNativeDll();
            NativeLibrary.SetDllImportResolver(typeof(LSL.StreamInfo).Assembly, DllImportResolver);
        }

        private static void ExtractNativeDll()
        {
            // Use a versioned subfolder in LocalAppData so multiple versions don't clash
            var asm = Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version?.ToString() ?? "0";
            var targetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LSLMarkerSender", "native", version);

            Directory.CreateDirectory(targetDir);
            _extractedDllPath = Path.Combine(targetDir, "lsl.dll");

            // Only extract if the file doesn't already exist (or is corrupt/zero-length)
            if (!File.Exists(_extractedDllPath) || new FileInfo(_extractedDllPath).Length == 0)
            {
                using var stream = asm.GetManifestResourceStream("lsl.dll");
                if (stream == null)
                    throw new FileNotFoundException(
                        "Embedded lsl.dll resource not found. Ensure lsl.dll is set as EmbeddedResource in the project.");

                using var fs = new FileStream(_extractedDllPath, FileMode.Create, FileAccess.Write, FileShare.None);
                stream.CopyTo(fs);
            }
        }

        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // Only intercept the "lsl" library that LSL.cs imports
            if (libraryName == "lsl")
            {
                if (_loadedLibHandle == IntPtr.Zero)
                {
                    // First try: load from extracted path
                    if (!string.IsNullOrEmpty(_extractedDllPath) && File.Exists(_extractedDllPath))
                    {
                        if (NativeLibrary.TryLoad(_extractedDllPath, out _loadedLibHandle))
                            return _loadedLibHandle;
                    }

                    // Fallback: try the exe's directory (in case user placed lsl.dll there manually)
                    var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    var sideBySidePath = Path.Combine(exeDir, "lsl.dll");
                    if (File.Exists(sideBySidePath))
                    {
                        if (NativeLibrary.TryLoad(sideBySidePath, out _loadedLibHandle))
                            return _loadedLibHandle;
                    }

                    // Last resort: let the OS search PATH
                    NativeLibrary.TryLoad("lsl", assembly, searchPath, out _loadedLibHandle);
                }
                return _loadedLibHandle;
            }

            // For all other native libraries, fall back to default resolution
            return IntPtr.Zero;
        }
    }
}
