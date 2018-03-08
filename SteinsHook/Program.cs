using System;
using System.Runtime.InteropServices;
using HookUtils;

namespace SteinsHook
{
    class Program
    {
        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        //static void Main(string[] args)
        //{
        //    Console.OutputEncoding = Encoding.Unicode;
        //    Process process = Process.GetProcessesByName("Game")[0];
        //    IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
        //    int processPtr = (int)processHandle;

        //    //byte[] query = Encoding.Unicode.GetBytes("ねぇねぇ。なにブツブツ言ってるのー？");
        //    ////Encoding.GetEncoding(932).GetBytes("ねぇねぇ");
        //    //byte[] query2 = Encoding.GetEncoding(932).GetBytes("ふふん");
        //    //byte[] contentKey = new byte[query.Length];
        //    //byte[] content = new byte[256];
        //    //byte[] nearby = new byte[1024];

        //    //var memoryPtr = MemoryUtil.Search(processPtr, query);
        //    //MemoryUtil.DumpSection("found.txt", processPtr, memoryPtr - 0x1000, 0x2000);
        //    //Console.WriteLine(memoryPtr);

        //    //var memoryPtr2 = MemoryUtil.Search(processPtr, query2);
        //    //Console.WriteLine(memoryPtr2);

        //    var binDecoder = new BinaryDecoder();

        //    var testBuffer = new byte[] { 0x11, 0x12, 0x13, 0x23, 0x24, 0x12, 0x13, 0x23, 0x24, 0x11 };
        //    Console.WriteLine(testBuffer.ToByteString());
        //    Console.WriteLine(binDecoder.IsPossible("ねぇねぇ", testBuffer, 0));
        //    Console.WriteLine(binDecoder.IsPossible("ねぇねぇ", testBuffer, 1));

        //    binDecoder.CheckPossible(processPtr, "右耳に当てているケータイ電話。通話口からはなにも聞こえてこない。");
        //    //binDecoder.CheckPossible(processPtr, "ねぇねぇ。なにブツブツ言ってるのー？");

        //    Console.ReadLine();
        //    //return;
        //}
        
        static void Main(string[] args)
        {
            int processPtr = ProcessUtil.OpenProcess("Game");

            SteinsMonitor.Run(processPtr);
        }
    }
}
