using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXHook
{
    class Program
    {
        static void Main(string[] args)
        {
            int processPtr = ProcessUtil.OpenProcess("FFX");

            var bc = new BinaryDecoder();
            bc.CheckPossible(processPtr, "いろいろ考えてたのは");
                //"ぜんぶ話しておきたいんだ");

            //byte[] query = Encoding.Unicode.GetBytes("Listen to my story.");
            byte[] query = Encoding.GetEncoding(932).GetBytes("ぜんぶ話しておきたいんだ");

            var memoryPtr = MemoryUtil.Search(processPtr, query);
            MemoryUtil.DumpSection("found-ffx.txt", processPtr, memoryPtr - 0x1000, 0x2000);
            Console.WriteLine(memoryPtr);
        }
    }
}
