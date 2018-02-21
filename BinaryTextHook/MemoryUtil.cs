using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace BinaryTextHook
{
    public class MemoryUtil
    {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
          int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        static int bytesRead = 0;
        static byte[] charBytes = new byte[2];

        static void ReadChunk(int processPtr, byte[] buffer, int memoryIndex)
        {
            ReadProcessMemory(processPtr, memoryIndex, buffer, buffer.Length, ref bytesRead);
            memoryIndex += buffer.Length;
        }

        static byte[] GetChar(int processPtr, int memoryIndex, byte[] buffer, ref int arrayStartMemoryIndex)
        {
            if (memoryIndex >= arrayStartMemoryIndex + buffer.Length - 1)
            {
                arrayStartMemoryIndex = arrayStartMemoryIndex + buffer.Length - 1;
                ReadChunk(processPtr, buffer, arrayStartMemoryIndex);
            }

            if (bytesRead == 0)
            {
                charBytes[0] = 0;
                charBytes[1] = 0;
            }
            else
            {
                charBytes[0] = buffer[memoryIndex - arrayStartMemoryIndex];
                charBytes[1] = buffer[memoryIndex - arrayStartMemoryIndex + 1];
            }
            return charBytes;
        }

        public static void WriteMemoryStringsToFile(int processPointer, string filename)
        {
            using (var writer = File.CreateText(filename))
            {
                var shortBuffer = new byte[0x100];
                int memoryIndex = 0;
                int arrayStartMemoryIndex = 0;

                var maxMemoryIndex = 0x10000000;

                while (memoryIndex < maxMemoryIndex)
                {
                    var charBuffer = GetChar(processPointer, memoryIndex, shortBuffer, ref arrayStartMemoryIndex);
                    if (EncodingUtil.IsShiftJISChar(charBuffer, 0))
                    {
                        var stringBytes = new List<byte>();
                        while (EncodingUtil.IsShiftJISChar(GetChar(processPointer, memoryIndex, shortBuffer, ref arrayStartMemoryIndex), 0)
                            && memoryIndex < maxMemoryIndex)
                        {
                            stringBytes.Add(charBuffer[0]);
                            stringBytes.Add(charBuffer[1]);
                            memoryIndex += 2;
                        }
                        var utf8Bytes = Encoding.Convert(Encoding.GetEncoding(932), Encoding.Unicode, stringBytes.ToArray());
                        writer.WriteLine(Encoding.Unicode.GetString(utf8Bytes));
                        writer.WriteLine();
                        memoryIndex--;
                    }
                    memoryIndex++;
                }
            }
        }

        public static uint Search(int processHandle, byte[] query, uint max = 0xFFFFFFFF, uint startIndex = 0)
        {
            var start = DateTime.Now;

            uint memoryPointer = startIndex;
            int bytesRead = -1;
            byte[] buffer = new byte[0xFF00];

            while (memoryPointer < max)
            {
                if (SearchChunk(processHandle, query, buffer, ref memoryPointer, out bytesRead))
                {
                    //Console.WriteLine("Found: " + memoryPointer.ToString("X4") + " (" + (DateTime.Now - start).TotalMilliseconds + ")");
                    return memoryPointer;
                }
            }
            Console.WriteLine("did not find");
            return 0;
        }

        public static bool SearchAtPointer(int processHandle, byte[] query, uint memoryPointer)
        {
            int bytesRead = -1;
            byte[] buffer = new byte[query.Length];
            return SearchChunk(processHandle, query, buffer, ref memoryPointer, out bytesRead);
        }

        static bool __printed = false;
        static bool SearchChunk(int processHandle, byte[] query, byte[] buffer, ref uint memoryPointer, out int bytesRead)
        {
            bytesRead = 0;
            ReadProcessMemory(processHandle, (int)memoryPointer, buffer, buffer.Length, ref bytesRead);
            var memoryStart = memoryPointer;
            var memoryEnd = memoryPointer + buffer.Length - query.Length + 1;
            if (bytesRead == 0)
            {
                //Console.WriteLine("can't read; " + memoryPointer);
                //return false;
                //throw new Exception("End of memory");
                memoryPointer = (uint)memoryEnd;
                return false;
            }

            if (!__printed)
            {
                __printed = true;
                Console.WriteLine("MEM START: " + memoryPointer);
            }

            while (memoryPointer < memoryEnd)
            {
                if (ByteCompare(query, buffer, memoryPointer - memoryStart))
                {
                    return true;
                }
                memoryPointer += 0x1;
            }
            return false;
        }

        public static byte[] DumpSection(string name, int processPointer, uint memoryStart, int length)
        {
            var buffer = new byte[length];
            Fill(processPointer, buffer, memoryStart);
            File.WriteAllBytes(name, buffer);
            return buffer;
        }

        public static void HandleContent(byte[] content)
        {
            var start = 0;
            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] == 0x00)
                {
                    break;
                }

                if (content[i] == 0x02)
                {
                    var tmpArray = content.SubArray(start, i - start);
                    for (var j = 0; j < tmpArray.Length; j++)
                    {
                        if (tmpArray[j] == 0x01)
                            tmpArray[j] = 0x0A;
                    }

                    var utf8Bytes = Encoding.Convert(Encoding.GetEncoding(932), Encoding.Unicode, tmpArray);
                    Console.WriteLine("『" + Encoding.Unicode.GetString(utf8Bytes) + "』");
                }

                if (content[i] == 0x03)
                {
                    start = i + 1;
                    continue;
                }
            }
        }

        public static void Fill(int processHandle, byte[] buffer, uint memoryPointer)
        {
            var bytesRead = 0;
            ReadProcessMemory(processHandle, (int)memoryPointer, buffer, buffer.Length, ref bytesRead);
        }

        public static bool HasMemoryChanged(int processHandle, byte[] originalBuffer, byte[] newBuffer, uint memoryPointer)
        {
            Fill(processHandle, newBuffer, memoryPointer);
            for (var i = 0; i < originalBuffer.Length; i++)
            {
                if (originalBuffer[i] != newBuffer[i]) return true;
            }
            return false;
        }

        public static bool ByteCompare(byte[] query, byte[] buffer, uint bufferStart)
        {
            for (int i = 0; i < query.Length; i++)
            {
                if (query[i] != buffer[bufferStart + i])
                    return false;
            }

            return true;
        }
    }
}
