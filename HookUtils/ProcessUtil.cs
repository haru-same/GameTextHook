using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace HookUtils
{
    public static class ProcessUtil
    {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        public static int OpenProcess(string name)
        {
            //foreach (var p in Process.GetProcesses())
            //{
            //    Console.WriteLine(p.ProcessName);
            //}

            Process process = Process.GetProcessesByName(name)[0];
            IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
            return (int)processHandle;
        }

        public static Tuple<Process, int> OpenAndGetProcess(string name)
        {
            Process process = Process.GetProcessesByName(name)[0];
            IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
            return new Tuple<Process, int>(process, (int)processHandle);
        }
    }
}
