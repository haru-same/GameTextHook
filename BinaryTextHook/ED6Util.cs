using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BinaryTextHook
{
    class ED6Util
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

        static string GetPrefixChunk(string line)
        {
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

        static string StripPrefix(string line)
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

        public static uint SearchDialogueStart(int processPointer)
        {
            //var search = MemoryUtil.Search(processPointer, query, 0x3000000, startIndex: 0x1180000);
            var search = MemoryUtil.Search(processPointer, query, 0x4000000, startIndex: 0x2A00000);
            MemoryUtil.Fill(processPointer, shortBuffer, search + 146);
            if (!ByteUtil.ByteCompare(isSpokenDialogueQuery, shortBuffer, 0))
            {
                search = MemoryUtil.Search(processPointer, query, 0x4000000, startIndex: search + 302);
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
            var contentStrings = TextFormat.ExtractED6Text(memory, BeforeDialogueBlockSize);

            var nameBuffer = new byte[64];
            MemoryUtil.Fill(processPointer, nameBuffer, memoryPointer + 1024 - 8);
            var nameString = TextFormat.ExtractED6Text(nameBuffer, 8)[0];

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
            foreach(var c in input)
            {
                var int_val = (int)c;
                if(int_val <= 0x03)
                {
                    sb.Append('\n');
                } else if (int_val == 0x0A)
                {
                    sb.Append('\n');
                } else if (int_val > 0x1F)
                {
                    sb.Append(c);
                }
            }
            sb.Replace("\n\n", "\n");
            return sb.ToString();
        }

        static void UpdateContent(int processPointer, byte[] dialogueMemory, uint memoryPointer, ref string content)
        {
            var newLines = GetContent(processPointer, dialogueMemory, memoryPointer);
            var newContent = String.Join("\n", newLines.Item2);

            if (newContent != content)
            {
                byte[] nearby = new byte[2048];
                MemoryUtil.Fill(processPointer, nearby, memoryPointer - 512);
                File.WriteAllBytes("ed6_tmp.txt", nearby);

                //var search = MemoryUtil.Search(processPointer, query, 0x3000000, startIndex: memoryPointer);
                var search = MemoryUtil.Search(processPointer, query, 0x3000000, startIndex: memoryPointer);
                var fileIndex = 1;
                while (search != 0)
                {
                    MemoryUtil.Fill(processPointer, nearby, search - 64);
                    File.WriteAllBytes("ed6_tmp" + fileIndex + ".txt", nearby);
                    //search = MemoryUtil.Search(processPointer, query, 0x3000000, startIndex: search + 64);
                    search = MemoryUtil.Search(processPointer, query, 0x3000000, startIndex: search + 64);
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
                        Request.MakeRequest("http://localhost:1414/new-text?text=", requestText);
                    }
                }
            }
        }

        public static void ED6Monitor(int processPointer)
        {
            var ed6DialogueStart = SearchDialogueStart(processPointer);
            Console.WriteLine("NEW HEAD IS: " + ed6DialogueStart.ToString("X4"));
            var currentDialogue = GetActiveDialogue(processPointer, ed6DialogueStart);
            var buffer = new byte[currentDialogue.Length];

            var visibleBuffer = new byte[2];
            var newVisisbleBuffer = new byte[2];
            MemoryUtil.Fill(processPointer, visibleBuffer, DialogueMemoryPointerToVisibleMemoryPointer(ed6DialogueStart));
            Console.WriteLine(visibleBuffer.ToByteString());

            var content = "";
            UpdateContent(processPointer, currentDialogue, ed6DialogueStart, ref content);


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
                UpdateContent(processPointer, currentDialogue, ed6DialogueStart, ref content);

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
    }
}
