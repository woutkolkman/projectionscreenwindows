using System.Runtime.InteropServices;

namespace CaptureAPI
{
    //not my code, but from https://stackoverflow.com/questions/12586957/how-do-i-open-a-process-so-that-it-doesnt-have-focus/19049930#19049930
    //more info https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow?redirectedfrom=MSDN

    public class StartNoFocus
    {
        [StructLayout(LayoutKind.Sequential)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll")]
        static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        const int STARTF_USESHOWWINDOW = 1;
        const int SW_SHOWNOACTIVATE = 4; //show the window normally but without stealing focus
        const int SW_SHOWMINNOACTIVE = 7; //start the app minimised, again without stealing focus


        public static void StartProcessNoActivate(string cmdLine)
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.dwFlags = STARTF_USESHOWWINDOW;
            si.wShowWindow = SW_SHOWNOACTIVATE;

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

#pragma warning disable CS8625
            CreateProcess(null, cmdLine, IntPtr.Zero, IntPtr.Zero, true, 0, IntPtr.Zero, null, ref si, out pi);
#pragma warning restore CS8625

            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
        }
    }
}
