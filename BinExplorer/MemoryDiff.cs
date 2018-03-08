using HookUtils;
using System;

namespace BinExplorer
{
    class MemoryDiff
    {
        public static void DiffProcess(int processHandle)
        {
            MemoryUtil.ReadToEnd(processHandle);
            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}
