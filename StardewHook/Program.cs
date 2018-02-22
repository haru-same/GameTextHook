using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewHook
{
    class Program
    {
        const int PrefixToText = 10;

        static void Main(string[] args)
        {
            var textPrefix = ByteUtil.HexStringToByteArray("0040F0FB");
            var processId = ProcessUtil.OpenProcess("Stardew Valley");
            var buffer = new byte[1024];

            uint memoryLocation = 2;
            while (memoryLocation > 0) {
                memoryLocation += 2;
                memoryLocation = MemoryUtil.Search(processId, textPrefix, startIndex: memoryLocation, max: int.MaxValue);
                MemoryUtil.Fill(processId, buffer, memoryLocation + PrefixToText);
                Console.WriteLine(buffer.GetString(0));
            }

            Console.WriteLine("\nScan complete. Press any key to close.");
            Console.ReadLine();
        }
    }
}
