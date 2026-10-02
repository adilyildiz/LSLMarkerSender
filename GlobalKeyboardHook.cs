using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LSLMarkerSender
{
    /// <summary>
    /// Low-level keyboard hook that captures key presses globally (even when the app is not focused).
    /// Uses Win32 SetWindowsHookEx with WH_KEYBOARD_LL.
    /// </summary>
    public class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;

        /// <summary>
        /// Fired when a key is pressed. The Keys argument indicates which key was pressed.
        /// </summary>
        public event Action<Keys> KeyPressed;

        /// <summary>
        /// Whether the hook is currently active and capturing key events.
        /// </summary>
        public bool IsHooked => _hookId != IntPtr.Zero;

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
        }

        /// <summary>
        /// Install the global keyboard hook.
        /// </summary>
        public void Hook()
        {
            if (_hookId != IntPtr.Zero) return;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// Remove the global keyboard hook.
        /// </summary>
        public void Unhook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;
                KeyPressed?.Invoke(key);
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Unhook();
        }

        // --- Win32 P/Invoke ---

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
