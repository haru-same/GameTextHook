using HookUtils;
using SteinsHook;
using System;
using System.Text;

namespace DanganropaHook
{
    class Program
    {
        static void Main(string[] args)
        {
            int processPtr = ProcessUtil.OpenProcess("Game");

            //var query = SteinsEncoding.Encode("ねぇねぇ");
            var query = Encoding.UTF8.GetBytes("右耳に当て");
            //var query = SteinsEncoding.Encode("を受けて");
            //var query = SteinsEncoding.Encode("ぽたりと");
            //var query = SteinsEncoding.Encode("オカリン？");
            //"　ねぇってばー");
            //var query = SteinsEncoding.Encode("誰かと電話中？");
            //var query = SteinsEncoding.Encode("うなずいてから、");
            //var query = SteinsEncoding.Encode("電話の向こうから");

            //var query = new byte[] { 0x0D, 0x00, 0x00, 0x00, 0x0e };

            Console.WriteLine("Target is: " + MemoryUtil.Search(processPtr, query));



            uint memoryPtr = 33771968; //21123520;//MemoryUtil.Search(processPtr, query);
            MemoryUtil.DumpSection("found.txt", processPtr, memoryPtr - 512, 1024);
            Console.WriteLine(memoryPtr);

            var buffer = new byte[512];
            MemoryUtil.Fill(processPtr, buffer, memoryPtr - 10);

            Console.WriteLine(SteinsText.ExtractText(buffer, 10));
        }
    }
}
