using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoraVoiceLib
{
    public class Class1
    {
        public static void DoTest()
        {
            File.WriteAllText("test.txt", "hello world!\n");
        }
    }
}
