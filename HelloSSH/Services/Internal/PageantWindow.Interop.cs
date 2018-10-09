using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HelloSSH.Services.Internal
{
    internal partial class PeageantWindow
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WndClass
        {
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CopyData
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern UInt16 RegisterClassW(
            [In] ref WndClass lpWndClass
        );[DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowExW(
            UInt32 dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            UInt32 dwStyle,
            Int32 x,
            Int32 y,
            Int32 nWidth,
            Int32 nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public const int AGENT_COPYDATA_ID = unchecked((int)0x804e50ba);
        public const int WM_COPYDATA = 0x004A;
        public const int ERROR_CLASS_ALREADY_EXISTS = 1410;
        public const string CLASS_NAME_PAGEANT = "Pageant";
    }
}
