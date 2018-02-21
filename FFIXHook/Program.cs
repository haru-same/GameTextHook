using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFIXHook
{
    class Program
    {

        static byte[] bytes = new byte[2000];
        static uint firstFound = 0;
        static Dictionary<string, string> substringToFullstring;

        static void FindAll(Tuple<Process, int> process, byte[] query, HashSet<string> strings)
        {
            uint startIndex = firstFound;
            uint max = 0x2F000000;
            //uint.MaxValue;
            while (true)
            {
                //max = (uint)Math.Min(max, firstFound + process.Item1.PeakWorkingSet64);

                startIndex = MemoryUtil.Search(process.Item2, query, startIndex: startIndex + 2, max: max);
                //Console.WriteLine("start: " + startIndex.ToString("X4"));

                if (firstFound == 0) firstFound = startIndex;

                if (startIndex == 0)
                {
                    break;
                }

                MemoryUtil.Fill(process.Item2, bytes, startIndex);

                var extractedString = bytes.GetString(0);
                strings.Add(ClearBrackets(extractedString));

                //System.Threading.Thread.Sleep(1);
            }
        }

        static string ClearBrackets(string input)
        {
            var sb = new StringBuilder();
            var writeOn = true;
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '[') writeOn = false;
                if (writeOn) sb.Append(input[i]);
                if (input[i] == ']') writeOn = true;
            }
            return sb.ToString();
        }

        static HashSet<string> ReduceSubstrings(IEnumerable<string> strings)
        {
            HashSet<string> outStrings = new HashSet<string>();
            var array = strings.ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                var isUnique = true;
                for (var j = 0; j < array.Length; j++)
                {
                    if (array[j].Length <= array[i].Length) continue;
                    if (array[j].Contains(array[i].Substring(0, array[i].Length - 1)))
                    {
                        isUnique = false;
                        break;
                    }
                }
                if (isUnique)
                {
                    outStrings.Add(array[i]);
                }
            }
            return outStrings;
        }

        static void GenerateSubstringToString(IEnumerable<string> strings)
        {
            HashSet<string> unique = new HashSet<string>();
            HashSet<string> notUnique = new HashSet<string>();
            var array = strings.ToArray();
            //Console.WriteLine("array");
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i].Length == 0) continue;

                //File.AppendAllText("lines.txt", "{{" + array[i] + "}}\n");
                var isUnique = true;
                for (var j = 0; j < array.Length; j++)
                {
                    if (array[j].Length <= array[i].Length) continue;
                    if (array[j].Contains(array[i].Substring(0, array[i].Length - 1)))
                    {
                        isUnique = false;
                        break;
                    }
                }

                if (isUnique)
                {
                    substringToFullstring[array[i]] = "";
                    unique.Add(array[i]);
                }
                else 
                {
                    notUnique.Add(array[i]);
                }
            }
            //Console.WriteLine("added");

            foreach (var subKey in notUnique)
            {
                foreach (var uniqueKey in unique)
                {
                    if (uniqueKey.Contains(subKey))
                    {
                        substringToFullstring[subKey] = uniqueKey;
                        break;
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            substringToFullstring = new Dictionary<string, string>();
            Console.OutputEncoding = Encoding.Unicode;

            //Console.WriteLine(ClearBrackets("「今のとこ[00]ろ異常ないでよ!"));

            var process = ProcessUtil.OpenAndGetProcess("FF9");

            //var query = Encoding.Unicode.GetBytes(@"覚悟はいいか");
            var query = Encoding.Unicode.GetBytes(@"[C8C8C8]");

            var foundSet = new HashSet<string>();
            FindAll(process, query, foundSet);

            Console.WriteLine("================================================================================");
            foreach (var key in foundSet)
            {
                Console.WriteLine(key);
                //File.AppendAllText("original.txt", "\n" + "{{" + key + "}}");
            }

            Console.WriteLine("---------------------------------------------------------------------------------");
            HashSet<string> displayed = new HashSet<string>();
            displayed.Add("");
            while (true)
            {
                var newSet = new HashSet<string>();
                FindAll(process, query, newSet);
                Console.WriteLine("================================================================================");
                GenerateSubstringToString(newSet);

                var diff = newSet.Except(foundSet);
                Console.WriteLine("before: " + diff.Count());

                diff = ReduceSubstrings(diff);
                Console.WriteLine("after: " + diff.Count());
                var finalSet = diff;

                //var maxString = "";
                //var finalSet = new HashSet<string>();
                //foreach (var newKey in diff)
                //{
                //    //File.AppendAllText("lines.txt", "\n{{" + newKey + "}}: ");

                //    //if (newKey.Length > maxString.Length) maxString = newKey;
                //    if (!substringToFullstring.ContainsKey(newKey)) continue;
                //    //File.AppendAllText("lines.txt", "\n[" + substringToFullstring[newKey] + "]");
                //    finalSet.Add(substringToFullstring[newKey]);
                //}

                foreach (var newKey in finalSet)
                {
                    if (!displayed.Contains(newKey))
                    {
                        //File.AppendAllText("lines.txt", newKey + "\n\n");
                        Request.MakeRequest("http://localhost:1414/new-text?text=", newKey);
                        displayed.Add(newKey);
                    }
                }
                foundSet = newSet;

                System.Threading.Thread.Sleep(123);
            }
        }
    }
}
