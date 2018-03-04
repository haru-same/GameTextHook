using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BinaryTextHook
{
    class Program
    {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
          int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern int GetProcessId(IntPtr handle);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr SetFocus(HandleRef hWnd);

        public static void Main()
        {
            Console.OutputEncoding = Encoding.Unicode;

            //foreach(var p in Process.GetProcesses())
            //{
            //    Console.WriteLine(p.ProcessName);
            //}

            //Process process = Process.GetProcessesByName("ePSXe")[0];
            //Process process = Process.GetProcessesByName("ed6_win")[0];
            //Process process = Process.GetProcessesByName("ed6_win_DX9")[0];
            Process process = Process.GetProcessesByName("ed6_win2_DX9")[0];
            IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
            int processPtr = (int)processHandle;

            //MemoryUtil.WriteMemoryStringsToFile(processPtr, "strings.txt");

            //byte[] query = { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x43 };
            //byte[] query = Encoding.GetEncoding(932).GetBytes("エステル");
            //byte[] query = Encoding.GetEncoding(932).GetBytes("シェラね");
            //byte[] query = Encoding.GetEncoding(932).GetBytes("うーん");
            //byte[] query = Encoding.GetEncoding(932).GetBytes("男性の声");
            //byte[] query = Encoding.GetEncoding(932).GetBytes("カシウス");
            //byte[] query = Encoding.GetEncoding(932).GetBytes("お、");
            //byte[] query = Encoding.GetEncoding(932).GetBytes("男の子");
            //byte[] query = Encoding.GetEncoding(932).GetBytes("おう");
            //byte[] query = Encoding.GetEncoding(932).GetBytes("おとーさん");
            byte[] query = Encoding.GetEncoding(932).GetBytes("いい子");
            byte[] query2 = Encoding.GetEncoding(932).GetBytes("ふふん");
            byte[] contentKey = new byte[query.Length];
            byte[] content = new byte[256];
            byte[] nearby = new byte[1024];

            uint memoryPtr = 100000;//MemoryUtil.Search(processPtr, query);
            MemoryUtil.DumpSection("found.txt", processPtr, memoryPtr - 0x1000, 0x2000);
            Console.WriteLine(memoryPtr);

            //var memoryPtr2 = MemoryUtil.Search(processPtr, query2);
            //Console.WriteLine(memoryPtr2);

            //Console.ReadLine();
            //return;

            //ED6Util.ED6Monitor(processPtr);

            while (true)
            {
                if (memoryPtr == 0)
                {
                    System.Threading.Thread.Sleep(51);
                    continue;
                }

                //var contentPointer = (uint)(memoryPtr + query.Length + contentOffset);
                var contentPointer = memoryPtr;
                MemoryUtil.Fill(processPtr, content, contentPointer);
                MemoryUtil.Fill(processPtr, contentKey, memoryPtr);
                MemoryUtil.Fill(processPtr, nearby, memoryPtr - 16);
                MemoryUtil.HandleContent(content);
                File.WriteAllBytes("content.txt", content);
                File.WriteAllBytes("tmp.txt", nearby);

                Console.Write("string: ");
                var lines = TextFormat.ExtractED6Text(nearby, 16);
                foreach (var line in lines)
                {
                    Console.WriteLine(line);
                    Request.MakeRequest("http://localhost:1414/new-text?text=", line);
                }

                //var processId = Process.GetProcessesByName("ed6_win")[0].Id;
                //ProcessExtension.Suspend(processId);
                //FocusThis();
                //Console.WriteLine("Press enter to continue...");
                //Console.ReadLine();
                //ProcessExtension.Resume(processId);
                //FocusProcess(processId);

                var handleIsSame = MemoryUtil.SearchAtPointer(processPtr, contentKey, memoryPtr);
                var contentIsSame = MemoryUtil.SearchAtPointer(processPtr, content, contentPointer);
                while (handleIsSame && contentIsSame)
                {
                    System.Threading.Thread.Sleep(50);
                    handleIsSame = MemoryUtil.SearchAtPointer(processPtr, contentKey, memoryPtr);
                    contentIsSame = MemoryUtil.SearchAtPointer(processPtr, content, contentPointer);
                }
                Console.WriteLine("handle same: " + handleIsSame + "; content same: " + contentIsSame);
            }

            Console.ReadLine();
        }

        static void FocusThis()
        {
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr hWnd = currentProcess.MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);
                SetFocus(new HandleRef(null, currentProcess.Handle));
                //ShowWindow(hWnd, User32.SW_MAXIMIZE);
            }
        }

        static void FocusProcess(int pid)
        {
            Process currentProcess = Process.GetProcessById(pid);
            IntPtr hWnd = currentProcess.MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);
            }
        }
    }
}
