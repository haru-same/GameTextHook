using BinaryUtils;
using HookUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WOFFHook
{
    class Program
    {
        const int CheckSize = 64;
        static Encoding encoding = Encoding.UTF8;
        static HashSet<string> addedLines = new HashSet<string>();

        static bool IsNumeralOrLowerCaseASCII(byte b)
        {
            return (b >= '0' && b <= '9') || (b >= 'a' && b <= 'z') || b == '_';
        }

        static bool IsAsciiLetter(byte c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
        }

        static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
        }

        static bool IsAllAsciiLetters(string s)
        {
            foreach(var c in s)
            {
                if (!IsAsciiLetter(c)) return false;
            }
            return true;
        }

        static bool IsValidJapaneseString(string s)
        {
            // probably an integer
            if (s.Length <= 4) return false;

            foreach(var c in s)
            {
                if (c <= ' ') return false;
            }

            if (IsAllAsciiLetters(s)) return false;

            return true;
        }

        static bool IsEvBuffer(byte[] buffer, uint start)
        {
            if (buffer[start] != 'e') return false;
            if (buffer[start + 1] != 'v') return false;
            if (!IsNumeralOrLowerCaseASCII(buffer[start + 2])) return false;
            if (!IsNumeralOrLowerCaseASCII(buffer[start + 3])) return false;
            if (buffer[start + 4] != '_') return false;
            return true;
        }

        static bool IsSubBuffer(byte[] buffer, uint start)
        {
            if (buffer[start] != 's') return false;
            if (buffer[start + 1] != 'u') return false;
            if (buffer[start + 2] != 'b') return false;
            if (!IsNumeralOrLowerCaseASCII(buffer[start + 3])) return false;
            if (!IsNumeralOrLowerCaseASCII(buffer[start + 4])) return false;
            if (buffer[start + 5] != '_') return false;
            return true;
        }

        // Search for 3 'words' together. The first has the prefix ev{0-9}{0-9}_ but after that there is a quite a bit of variation
        static bool TestBuffer(byte[] buffer, uint start)
        {
            if(buffer.Length - start < 8)
            {
                return false;
            }

            if(!IsEvBuffer(buffer, start) && !IsSubBuffer(buffer, start))
            {
                return false;
            }

            var end = Math.Min(buffer.Length, start + CheckSize);
            uint i = start + 5;
            for (; i < end; i++)
            {
                if (buffer[i] == 0) break;
                if (!IsNumeralOrLowerCaseASCII(buffer[i])) return false;
            }

            while(buffer[i] == 0)
            {
                i++;
                if (i == end) return false;
            }

            for(; i < end; i++)
            {
                if (buffer[i] == 0) break;
                if (!IsAsciiLetter(buffer[i])) return false;
            }

            while (buffer[i] == 0)
            {
                i++;
                if (i == end) return false;
            }

            for (; i < end; i++)
            {
                if (buffer[i] == 0) break;
                if (!IsAsciiLetter(buffer[i])) return false;
            }

            return true;
        }

        static void HandleNewText(string text, string voice, string key = null)
        {
            if (!IsValidJapaneseString(text)) return;

            var metadata = new Dictionary<string, string>();
            metadata["game"] = "woff";
            metadata["voice"] = voice;
            Request.MakeRequest("http://localhost:1414/new-text?text=", text, metadata);

            if (key == null) key = "";
            File.AppendAllText("voice-line-pairs.txt", voice + "; " + key +  ": " + text + "\n");
        }

        static void ScanForEventStrings(int processHandle, byte[] buffer)
        {
            uint start = 0x40000000;
            uint end = 0x70000000;
            var count = 0;
            Console.WriteLine("starting search");
            do
            {
                start = MemoryUtil.Search(processHandle, CheckSize, TestBuffer, startIndex: start + 1, endIndex: end);
                MemoryUtil.Fill(processHandle, buffer, start);
                var strings = buffer.GetStrings(0, encoding, terminatorCount: 1, maxCount: 7);
                if (strings.Count >= 6)
                {
                    if (!addedLines.Contains(strings[0]))
                    {
                        //foreach(var s in strings)
                        //{
                        //    Console.WriteLine(s);
                        //}
                        Console.WriteLine("mem: " + start.ToString("X4") + "; str: " + strings[0] + "; total: " + strings.Count);
                        //MemoryUtil.DumpSection(start + ".bin", processHandle, start - 5000, 10000);
                        //Console.WriteLine(strings[5]);
                        HandleNewText(strings[5], strings[1], strings[0]);
                        if (strings.Count > 6)
                        {
                            HandleNewText(strings[6], strings[1], strings[0]);
                        }
                        addedLines.Add(strings[0]);
                    }
                }

                count++;
            } while (start != 0);
            Console.WriteLine("done; count: " + count);
        }

        static void Main(string[] args)
        {
            var processHandle = ProcessUtil.OpenProcess("WOFF");

            //var query = encoding.GetBytes("タマも敗北宣");
            //uint start = 0;
            //do
            //{
            //    start = MemoryUtil.Search(processHandle, query, startIndex: start + 1);
            //    MemoryUtil.DumpSection(start.ToString("X4") + ".bin", processHandle, start - 5000, 10000);
            //    Console.WriteLine("mem: " + start.ToString("X4"));
            //} while (start != 0);
            //Console.ReadLine();
            //return;

            var buffer = new byte[1024];

            while (true)
            {
                ScanForEventStrings(processHandle, buffer);
                Thread.Sleep(13);
            }

            Console.ReadLine();
        }
    }
}
