using HookUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinExplorer
{
    class Program
    {
        static void Main(string[] args)
        {
            var process = ProcessUtil.OpenProcess("ed6_win3_DX9");

            MemoryUtil.DumpSection("2FA2F000.bin", process, 0x2FA2F000, 100 * 0x1000);

            //MemoryDiff.DiffProcess(process);
        }
    }
}
