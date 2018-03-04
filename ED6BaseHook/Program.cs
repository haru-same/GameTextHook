using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED6BaseHook
{
    class Program
    {
        static void Main(string[] args)
        {
            var processHandle = ProcessUtil.OpenProcess("ed6_win_DX9");
            ED6Util.ED6Monitor(processHandle);
        }
    }
}
