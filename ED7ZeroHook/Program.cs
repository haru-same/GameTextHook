using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED7ZeroHook
{
    class Program
    {
        static byte[] buffer;
        const int StringSize = 64;
        const int StringCount = 64;
        const int KeyToStringsOffset = 584;
        static Encoding encoding;
        static HashSet<string> displayed;
        static string[] previousContent = new string[StringCount];

        static List<string> ExtractStrings(int processHandle, uint memoryStart)
        {
            var strings = new List<string>();
            MemoryUtil.Fill(processHandle, buffer, memoryStart + KeyToStringsOffset);

            var newContent = new List<string>();
            
            for (int i = 0; i < StringCount; i++)
            {
                var length = StringSize;
                for (var j = 0; j < StringSize - 1; j++)
                {
                    if(buffer[i*StringSize + j] == 0x00 && buffer[i*StringSize + j+1] == 0x00)
                    {
                        length = j;
                        break;
                    }
                }

                var s = encoding.GetString(buffer, i * StringSize, length);
                if (s[0] == '０')
                {
                    break;
                }

                if(s != previousContent[i])
                {
                    previousContent[i] = s;
                    newContent.Add(s);
                }
                //if (strings.Count > 0 && s.Contains(strings.Last()))
                //{
                //    strings[strings.Count - 1] = s;
                //}
                //else
                //{
                //    strings.Add(s);
                //}
            }

            var uniqueNewContent = new List<string>();
            for(var i = 0; i < newContent.Count; i++)
            {
                var next = newContent[(i + 1) % newContent.Count];
                if (next.Length > newContent[i].Length && next.Contains(newContent[i]))
                {
                    continue;
                }
                uniqueNewContent.Add(newContent[i]);
            }

            return uniqueNewContent;
        }

        static void DisplayStrings(List<string> strings)
        {
            foreach(var s in strings)
            {
                if (!displayed.Contains(s))
                {
                    File.AppendAllText("out.txt", s + "\n");
                }
            }
            displayed = new HashSet<string>(strings);
        }

        static void Main(string[] args)
        {
            encoding = Encoding.GetEncoding("SHIFT-JIS");
            displayed = new HashSet<string>();
            buffer = new byte[StringSize * StringCount];
            
            Console.OutputEncoding = Encoding.Unicode;

            foreach (var p in Process.GetProcesses())
            {
                if (p.ProcessName.ToLower().Contains("ppsspp"))
                {
                    Console.WriteLine(p.ProcessName);
                }
            }
            var process = ProcessUtil.OpenAndGetProcess("PPSSPPWindows");

            var query = ByteUtil.HexStringToByteArray("C180000004C1000058C5FF");
            Console.WriteLine(query.ToByteString());

            //var query = Encoding.Unicode.GetBytes(@"どうだ");　//……ティオ

            uint baseStart = 0x10000000;
            uint max = uint.MaxValue;
            while (true)
            {
                var startIndex = MemoryUtil.Search(process.Item2, query, startIndex: baseStart, max: max);

                MemoryUtil.DumpSection(startIndex.ToString("X4") + "_5.bytes", process.Item2, startIndex + KeyToStringsOffset, 4096);
                Console.WriteLine("start: " + startIndex.ToString("X4"));

                var strings = ExtractStrings(process.Item2, startIndex);
                DisplayStrings(strings);

                System.Threading.Thread.Sleep(123);
            }

            Console.WriteLine("loop done");

            Console.ReadLine();
        }
    }
}
