using BinaryTextHook;
using ED6BaseHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED7ZeroPCHook
{
    class Program
    {
        static Encoding encoding = Encoding.GetEncoding("SHIFT-JIS");
        static HashSet<string> addedStrings = new HashSet<string>();

        static string GetED7String(byte[] buffer, int start)
        {
            while (start > 0)
            {
                if (buffer[start - 1] <= 3 && buffer[start] == '#')
                {
                    break;
                }
                start++;
            }

            var end = start + 1;
            while (end < buffer.Length)
            {
                if (buffer[end] == 0x02)
                {
                    break;
                }
                end++;
            }

            return encoding.GetString(buffer, start, end - start);
        }

        static bool IsValidVoiceString(byte[] bytes)
        {
            for (int i = 1; i < bytes.Length - 1; i++)
            {
                if (bytes[i] < '0' || bytes[i] > '9')
                {
                    return false;
                }
            }
            return true;
        }

        static void HandleNewText(string text)
        {
            var voice = ED6Util.GetVoicePrefix(text);
            text = ED6Util.StripPrefix(text).Replace('', '\n');

            Console.WriteLine("v(" + voice + "): " + text);

            var metadata = new Dictionary<string, string>() { { "game", "ed7zero" }, { "voice", voice } };

            Request.MakeRequest("http://localhost:1414/new-text?text=", text, metadata);
        }

        static void GetVoicedStrings(int processHandle)
        {
            Console.WriteLine("starting search");
            var count = 0;
            var query = new byte?[] { 0x23, null, null, null, null, null, null, null, 0x76 };
            var queryBuffer = new byte[query.Length];
            var stringBuffer = new byte[1024];

            uint start = 0;
            uint lastStart = 0;
            bool startedRead = false;
            List<string> lines = new List<string>();
            do
            {
                start = MemoryUtil.Search(processHandle, query, startIndex: start + 1);

                MemoryUtil.Fill(processHandle, queryBuffer, start);
                if (IsValidVoiceString(queryBuffer))
                {
                    if (lastStart > 0 && start - lastStart > 0x1000000)
                    {
                        Console.WriteLine("started read: " + lastStart + "; " + start);
                        startedRead = true;
                    }
                    lastStart = start;

                    if (startedRead)
                    {
                        MemoryUtil.Fill(processHandle, stringBuffer, start - 128);
                        var content = GetED7String(stringBuffer, 128);

                        if (!addedStrings.Contains(content))
                        {
                            Console.WriteLine(start.ToString("X4") + "; " + count);
                            HandleNewText(content);
                            count++;
                            addedStrings.Add(content);
                        }
                    }
                }
            } while (start != 0);

            Console.WriteLine("done, count: " + count++);
        }

        static void Main(string[] args)
        {
            var process = ProcessUtil.OpenAndGetProcess("ED_ZERO");

            while (true)
            {
                GetVoicedStrings(process.Item2);
                System.Threading.Thread.Sleep(345);
            }
        }
    }
}
