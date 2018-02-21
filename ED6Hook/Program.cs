using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED6Hook
{
    class Program
    {
        static string[] ConsecutiveStrings;

        static readonly string[] AlternatingStrings = {
            "53757270726973652061747461636B210000000094778CE382F082C682E782EA82BD814900000000507265656D70746976652061747461636B21000090E690A78D558C82814900"
        };

        static List<string> GetStrings(byte[] bytes)
        {
            var strings = new List<string>();
            int start = 0;
            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    var s = Encoding.GetEncoding(932).GetString(bytes, start, i - start);
                    if (i - start > 1)
                        strings.Add(s);
                    start = i + 1;
                }
            }
            if (bytes[bytes.Length - 1] != 0)
            {
                var s = Encoding.GetEncoding(932).GetString(bytes, start, bytes.Length - start);
                if (s != "")
                    strings.Add(s);
            }
            return strings;
        }

        static byte[] SwapConsecutiveStrings(byte[] bytes)
        {
            var outBytes = new byte[bytes.Length];
            Array.Copy(bytes, outBytes, bytes.Length);
            var strings = GetStrings(bytes);

            var writePosition = 0;
            var stringIndex = strings.Count / 2;

            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    if (i - writePosition > 1)
                    {
                        var stringBytes = Encoding.GetEncoding(932).GetBytes(strings[stringIndex]);
                        if(stringBytes.Length > i - writePosition + 1)
                        {
                            Console.WriteLine("WARNING: " + strings[stringIndex] + " is too long for " + strings[stringIndex - (strings.Count / 2)]);
                        }
                        stringIndex++;
                        Array.Copy(stringBytes, 0, outBytes, writePosition, Math.Min(stringBytes.Length, i - writePosition));
                        outBytes[writePosition + stringBytes.Length] = 0;

                        if (stringIndex >= strings.Count) break;
                    }
                    writePosition = i + 1;
                }
            }
            return outBytes;
        }

        static void Main(string[] args)
        {
            ConsecutiveStrings = File.ReadAllLines("consecutive_strings.txt");
            var bytes = File.ReadAllBytes("base_ed6_win_DX9.exe");

            foreach (var s in ConsecutiveStrings)
            {
                var original = ByteUtil.HexStringToByteArray(s);
                var swapped = SwapConsecutiveStrings(original);
                var position = ByteUtil.Search(bytes, original);
                Array.Copy(swapped, 0, bytes, position, swapped.Length);
            }

            File.WriteAllBytes("ed6_win_DX9.exe", bytes);

            Console.ReadLine();
        }
    }
}
