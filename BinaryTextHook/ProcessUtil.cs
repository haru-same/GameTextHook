using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace BinaryTextHook
{
    public static class ProcessUtil
    {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        // REQUIRED STRUCTS

        public struct MEMORY_BASIC_INFORMATION
        {
            public int BaseAddress;
            public int AllocationBase;
            public int AllocationProtect;
            public int RegionSize;
            public int State;
            public int Protect;
            public int lType;
        }

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

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
