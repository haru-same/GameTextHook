using BinaryUtils;
using HookUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace ED6BaseHook
{
    public class ED6Util
    {
        const int DialogueBlockLength = 512;
        const int BeforeDialogueBlockSize = 64;

        static byte[] query = { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x43, 0x00, 0x00, 0x00, 0x40 };

        static byte[] isSpokenDialogueQuery = { 0xFF, 0xFF, 0xFF, 0xFF };
        static byte[] shortBuffer = new byte[8];

        static uint DialogueMemoryPointerToVisibleMemoryPointer(uint dialogueMemoryPointer)
        {
            return dialogueMemoryPointer - 294;
        }

        public static string GetVoicePrefix(string line)
        {
            var inVoiceId = false;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (inVoiceId)
                {
                    if (line[i] == 'V' || line[i] == 'v')
                    {
                        return line.Substring(start + 1, i - start - 1);
                    }
                    else if (!line[i].IsASCIINumeral())
                    {
                        inVoiceId = false;
                    }
                }

                if (line[i] == '#')
                {
                    inVoiceId = true;
                    start = i;
                }
            }
            return null;
        }

        public static string GetPrefixChunk(string line)
        {
            if (line.Length == 0) return "";

            if (line[0] == '#')
            {
                for (var i = 1; i < line.Length; i++)
                {
                    if (!Char.IsDigit(line[i]))
                        return line.Substring(0, i + 1);
                }
                return "";
            }
            return "";
        }

        public static string StripFurigana(string line)
        {
            var sb = new StringBuilder();
            var inFurigana = false;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '#')
                {
                    if (inFurigana)
                    {
                        inFurigana = false;
                        continue;
                    }
                    else if (i < line.Length - 2 && line[i + 2] == 'R')
                    {
                        inFurigana = true;
                    }
                    else if (i < line.Length - 3 && line[i + 3] == 'R')
                    {
                        inFurigana = true;
                    }
                }

                if (!inFurigana) sb.Append(line[i]);
            }
            return sb.ToString();
        }

        public static string StripSizeChanges(string line)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < line.Length; i++)
            {
                if (i < line.Length - 3 && line[i] == '#' && line[i + 3] == 'W')
                {
                    i += 4;
                    if (i >= line.Length) break;
                }
                if (i < line.Length - 4 && line[i] == '#' && line[i + 4] == 'W')
                {
                    i += 5;
                    if (i >= line.Length) break;
                }
                sb.Append(line[i]);
            }
            return sb.ToString();
        }

        public static string StripPrefix(string line)
        {
            var prefix = "";
            var prefixChunk = GetPrefixChunk(line);
            while (prefixChunk != "")
            {
                prefixChunk = GetPrefixChunk(line.Substring(prefix.Length));
                prefix += prefixChunk;
            }
            return line.Substring(prefix.Length);
        }

        public static string StripPrefixAndSuffix(string line)
        {
            line = StripPrefix(line);

            for(var i = line.Length - 1; i > 0; i--)
            {
                if (line[i] == '#')
                {
                    line = line.Substring(0, i);
                    continue;
                }
                if (!line[i].IsASCIINumeralOrLetter()) break;
            }
            return line;
        }

        public static string StripTags(string line)
        {
            line = StripPrefixAndSuffix(line);
            line = StripFurigana(line);
            line = StripSizeChanges(line);
            return line;
        }

        public static string RemoveCommandCharacters(string line)
        {
            var encoding = Encoding.GetEncoding("SHIFT-JIS");
            var bytes = encoding.GetBytes(line);
            var outBytes = bytes.Where(b => b > 0x20).ToArray();
            return encoding.GetString(outBytes);
        }

        public static uint SearchDialogueStart(int processPointer)
        {
            //var search = MemoryUtil.Search(processPointer, query, 0x3000000, startIndex: 0x1180000);
            var search = MemoryUtil.Search(processPointer, query, endIndex: 0x4000000, startIndex: 0x1000000);
            MemoryUtil.Fill(processPointer, shortBuffer, search + 146);
            if (!ByteUtil.ByteCompare(isSpokenDialogueQuery, shortBuffer, 0))
            {
                search = MemoryUtil.Search(processPointer, query, endIndex: 0x4000000, startIndex: search + 302);
                MemoryUtil.Fill(processPointer, shortBuffer, search + 146);
            }
            Console.WriteLine("code: " + shortBuffer.ToByteString());
            if (search != 0)
            {
                return search + 302;
            }
            else
            {
                return 0;
            }
        }

        public static byte[] GetActiveDialogue(int processPointer, uint memoryPointer)
        {
            //MemoryUtil.DumpSection("ed6d.txt", processPointer, memoryPointer - 64, 1024);

            var contentBuffer = new byte[DialogueBlockLength];
            MemoryUtil.Fill(processPointer, contentBuffer, memoryPointer - BeforeDialogueBlockSize);

            return contentBuffer;
        }

        static Tuple<string, List<string>> GetContent(int processPointer, byte[] memory, uint memoryPointer)
        {
            var contentStrings = ExtractED6Text(memory, BeforeDialogueBlockSize);

            var nameBuffer = new byte[64];
            MemoryUtil.Fill(processPointer, nameBuffer, memoryPointer + 1024 - 8);
            var nameString = ExtractED6Text(nameBuffer, 8)[0];

            var strings = new List<string>();
            foreach (var s in contentStrings)
            {
                strings.Add(s);
            }
            return new Tuple<string, List<string>>(nameString, strings);
        }

        static string CleanForRequest(string input)
        {
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                var int_val = (int)c;
                if (int_val <= 0x03)
                {
                    sb.Append('\n');
                }
                else if (int_val == 0x0A)
                {
                    sb.Append('\n');
                }
                else if (int_val > 0x1F)
                {
                    sb.Append(c);
                }
            }
            sb.Replace("\n\n", "\n");
            return sb.ToString();
        }

        static void UpdateContent(int processPointer, byte[] dialogueMemory, uint memoryPointer, ref string content, Action<string, Dictionary<string, string>> handleNewText)
        {
            var newLines = GetContent(processPointer, dialogueMemory, memoryPointer);
            var newContent = String.Join("\n", newLines.Item2);

            if (newContent != content)
            {
                byte[] nearby = new byte[2048];
                MemoryUtil.Fill(processPointer, nearby, memoryPointer - 512);
                File.WriteAllBytes("ed6_tmp.txt", nearby);

                //var search = MemoryUtil.Search(processPointer, query, 0x3000000, startIndex: memoryPointer);
                var search = MemoryUtil.Search(processPointer, query, endIndex: 0x3000000, startIndex: memoryPointer);
                var fileIndex = 1;
                while (search != 0)
                {
                    MemoryUtil.Fill(processPointer, nearby, search - 64);
                    File.WriteAllBytes("ed6_tmp" + fileIndex + ".txt", nearby);
                    //search = MemoryUtil.Search(processPointer, query, 0x3000000, startIndex: search + 64);
                    search = MemoryUtil.Search(processPointer, query, endIndex: 0x3000000, startIndex: search + 64);
                    fileIndex++;
                }

                content = newContent;
                var outputContent = content;
                var stringBytes = Encoding.UTF8.GetBytes(outputContent);
                if (stringBytes.Length >= 2 && stringBytes[0] == 7)
                {
                    //Console.WriteLine(Encoding.UTF8.GetBytes(content).ToByteString());
                    //outputContent = outputContent.Substring(2, outputContent.Length - 2);
                    outputContent = Encoding.UTF8.GetString(stringBytes.SubArray(2, stringBytes.Length - 2)); //  outputContent.Substring(3);
                }
                //Console.WriteLine(outputContent);
                //Console.WriteLine(Encoding.UTF8.GetBytes(content).ToByteString());
                //Console.WriteLine(Encoding.UTF8.GetBytes(outputContent).ToByteString());
                //Request.MakeRequest("http://localhost:1414/new-text?text=", "〜" + newLines.Item1 + "〜");
                foreach (var line in newLines.Item2)
                {
                    var metadata = new Dictionary<string, string>();
                    var voice = GetVoicePrefix(line);
                    //Console.WriteLine(line);
                    if (voice != null)
                    {
                        metadata["voice"] = Voice.GetOggVoiceId(int.Parse(voice));
                    }
                    var outline = StripPrefix(line);
                    var lineBytes = Encoding.UTF8.GetBytes(outline);
                    if (lineBytes.Length >= 2 && stringBytes[0] == 7)
                    {
                        outline = Encoding.UTF8.GetString(stringBytes.SubArray(2, stringBytes.Length - 2));
                    }
                    //Console.WriteLine("l:" + outline);
                    Console.WriteLine(lineBytes.ToByteString());
                    File.WriteAllText("out_text.txt", outline);

                    var requestText = CleanForRequest(outline);
                    if (requestText != "\n" && requestText != "")
                    {
                        handleNewText(requestText, metadata);
                    }
                }
            }
        }

        public static void ED6Monitor(int processPointer, Action<string, Dictionary<string, string>> handleNewText = null)
        {
            Console.WriteLine("test");
            if (handleNewText == null)
            {
                Console.WriteLine("Using default handler");
                handleNewText = (text, metadata) =>
                {
                    Request.MakeRequest("http://localhost:1414/new-text?text=", text, new Dictionary<string, string>() { { "game", "ed6sc" } });
                };
            }

            var ed6DialogueStart = SearchDialogueStart(processPointer);
            Console.WriteLine("NEW HEAD IS: " + ed6DialogueStart.ToString("X4"));
            var currentDialogue = GetActiveDialogue(processPointer, ed6DialogueStart);
            var buffer = new byte[currentDialogue.Length];

            var visibleBuffer = new byte[2];
            var newVisisbleBuffer = new byte[2];
            MemoryUtil.Fill(processPointer, visibleBuffer, DialogueMemoryPointerToVisibleMemoryPointer(ed6DialogueStart));
            Console.WriteLine(visibleBuffer.ToByteString());

            var content = "";
            UpdateContent(processPointer, currentDialogue, ed6DialogueStart, ref content, handleNewText);


            //Thread visibilityListener = new Thread(() =>
            //{
            //    while (true)
            //    {
            //        MemoryUtil.Fill(processPointer, newVisisbleBuffer, DialogueMemoryPointerToVisibleMemoryPointer(ed6DialogueStart));
            //        if (visibleBuffer[0] != newVisisbleBuffer[0] || visibleBuffer[1] != newVisisbleBuffer[1])
            //        {
            //            Console.WriteLine("V>>>>>>>>>>>> " + newVisisbleBuffer.ToByteString());
            //            visibleBuffer[0] = newVisisbleBuffer[0];
            //            visibleBuffer[1] = newVisisbleBuffer[1];
            //            Thread.Sleep(7);
            //        }
            //    }
            //});
            //visibilityListener.Start();

            while (true)
            {
                System.Threading.Thread.Sleep(978);

                ed6DialogueStart = SearchDialogueStart(processPointer);
                currentDialogue = GetActiveDialogue(processPointer, ed6DialogueStart);
                UpdateContent(processPointer, currentDialogue, ed6DialogueStart, ref content, handleNewText);

                //MemoryUtil.Fill(processPointer, newVisisbleBuffer, DialogueMemoryPointerToVisibleMemoryPointer(ed6DialogueStart));
                //if (visibleBuffer[0] != newVisisbleBuffer[0] || visibleBuffer[1] != newVisisbleBuffer[1])
                //{
                //    Console.WriteLine("VISIBLE: " + newVisisbleBuffer.ToByteString());
                //    visibleBuffer[0] = newVisisbleBuffer[0];
                //    visibleBuffer[1] = newVisisbleBuffer[1];

                //    if (visibleBuffer[0] == 0 || visibleBuffer[1] == 0)
                //    {
                //        Console.WriteLine("SEEKING NEW HEAD...");
                //        uint newStart = 0;
                //        while (newStart == 0)
                //        {
                //            newStart = SearchDialogueStart(processPointer);
                //            Thread.Sleep(50);
                //        }
                //        Console.WriteLine("NEW HEAD IS: " + newStart.ToString("X4"));
                //        ed6DialogueStart = newStart;
                //        currentDialogue = GetActiveDialogue(processPointer, ed6DialogueStart);
                //        UpdateContent(processPointer, currentDialogue, ed6DialogueStart, ref content);
                //    }
                //}

                //while (!MemoryUtil.HasMemoryChanged(processPointer, currentDialogue, buffer, ed6DialogueStart - BeforeDialogueBlockSize))
                //{
                //    System.Threading.Thread.Sleep(50);
                //}

                //currentDialogue = GetActiveDialogue(processPointer, ed6DialogueStart);
                //UpdateContent(processPointer, currentDialogue, ed6DialogueStart, ref content);
            }
        }

        public static List<string> GetStringsWithEOT(byte[] text)
        {
            var start = 0;
            var end = 0;
            List<string> strings = new List<string>();
            for (end = 0; end < text.Length; end++)
            {
                if (text[end] == 0x03)
                {
                    start = end + 1;
                }
                else if (text[end] == 0x02)
                {
                    if (end > start)
                    {
                        var subString = text.SubArray(start, end - start);
                        //subString.Replace(0x01, (byte)'\n');
                        var utf8Bytes = Encoding.Convert(Encoding.GetEncoding(932), Encoding.Unicode, subString);
                        strings.Add(Encoding.Unicode.GetString(utf8Bytes));
                    }
                    start = end + 1;
                }
            }

            if (end > start)
            {
                var subString = text.SubArray(start, end - start);
                subString.Replace(0x01, (byte)'\n');
                var utf8Bytes = Encoding.Convert(Encoding.GetEncoding(932), Encoding.Unicode, subString);
                strings.Add(Encoding.Unicode.GetString(utf8Bytes));
            }

            return strings;
        }

        public static List<string> ExtractED6Text(byte[] content, int start)
        {
            while (start > 2)
            {
                if (content[start - 2] == 0 && content[start - 1] == 0) break;
                start--;
            }
            var end = start;
            while (end < content.Length - 2)
            {
                if (content[end + 1] == 0 && content[end + 2] == 0) break;
                end++;
            }
            //Console.WriteLine("start: " + start + " end: " + end);

            return GetStringsWithEOT(content.SubArray(start, end - start + 1));
        }
    }
}
