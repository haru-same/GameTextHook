using BinaryTextHook;
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
        static bool IsValid(string s)
        {
            if (s.Length > 5 && s[0] == 'S' && s[5] == '_') return true;
            if (s.Substring(0, 3) == "vo_") return false;
            if (s.Length > 4 && s.Substring(0, 3) == "ev2") return false;
            if (s == "SERAFY_0") return false;
            if (s == "ACHO2") return false;

            foreach (var c in s)
            {
                if (c >= '0' && c <= '9') return true;
            }
            return false;
        }

        static void Main(string[] args)
        {
            var bytes = File.ReadAllBytes("H:/game hack/SonicAudioTools/vo.acb");
            var query = Encoding.UTF8.GetBytes("CueIndex");

            var lines = new List<string>();
            var diffLines = new List<string>();
            var start = ByteUtil.Search(bytes, query);
            var count = 0;
            Console.WriteLine("start: " + start);
            var lastLine = "";
            for (int i = (int)(start + query.Length); i < bytes.Length; i++)
            {
                if(bytes[i] == 0 && bytes[i + 1] == 0) break;
                if (bytes[i] == 0)
                {
                    var s = bytes.GetString(i + 1, Encoding.UTF8, 1);
                    if (IsValid(s))
                    {
                        lines.Add(s);

                        if (s.Length != lastLine.Length)
                        {
                            diffLines.Add(s);
                        }
                        count++;
                    }
                    lastLine = s;
                }
            }
            File.WriteAllLines("lines2.txt", lines.ToArray());
            File.WriteAllLines("lines3.txt", diffLines.ToArray());
            Console.WriteLine("count: " + count);

            Console.ReadLine();
        }
    }
}
