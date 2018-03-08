using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using BinaryUtils;

namespace HookUtils
{
    public class MemoryUtil
    {
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
          int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

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

        public static List<byte> ReadToEnd(int processHandle)
        {
            uint lastMemoryPointer = 0;
            uint memoryPointer = 0;
            int bytesRead = -1;
            //byte[] buffer = new byte[0xFF00];
            byte[] buffer = new byte[4096];

            var zeroes = 0;
            var nonZeroes = 0;
            do
            {
                lastMemoryPointer = memoryPointer;
                ReadProcessMemory(processHandle, (int)memoryPointer, buffer, buffer.Length, ref bytesRead);
                memoryPointer += (uint)buffer.Length;

                //if (bytesRead == 0)
                //{
                //    Console.WriteLine("error: " + GetLastError());
                //}

                if (bytesRead == 0) zeroes++;
                else nonZeroes++;
            } while (memoryPointer > lastMemoryPointer);
            Console.WriteLine("non-zero-to-zero-blocks: " + nonZeroes + "/" + zeroes);

            lastMemoryPointer = 0;
            memoryPointer = 0;
            var bytes = new byte[nonZeroes * buffer.Length];
            var byteIndex = 0;
            do
            {
                lastMemoryPointer = memoryPointer;
                ReadProcessMemory(processHandle, (int)memoryPointer, buffer, buffer.Length, ref bytesRead);
                memoryPointer += (uint)buffer.Length;

                if (bytesRead != 0)
                {
                    Array.Copy(buffer, 0, bytes, byteIndex, buffer.Length);
                    byteIndex += buffer.Length;
                    if (byteIndex >= bytes.Length) break;
                }
            } while (memoryPointer > lastMemoryPointer);

            Console.WriteLine("Memory copied, allow game to play and hit enter.");
            Console.ReadLine();
            Console.WriteLine("comparing...");

            lastMemoryPointer = 0;
            memoryPointer = 0;
            byteIndex = 0;
            int state = 0;
            uint currentStart = 0;
            var changedRanges = 0;
            var changedPages = new HashSet<int>();
            do
            {
                lastMemoryPointer = memoryPointer;
                ReadProcessMemory(processHandle, (int)memoryPointer, buffer, buffer.Length, ref bytesRead);

                if (bytesRead != 0)
                {
                    var areBytesSame = ByteUtil.ByteCompare(buffer, bytes, (uint)byteIndex);

                    Array.Copy(buffer, 0, bytes, byteIndex, buffer.Length);

                    byteIndex += buffer.Length;

                    if (!areBytesSame) changedPages.Add((int)memoryPointer);

                    switch (state)
                    {
                        case 0:
                            if (!areBytesSame)
                            {
                                currentStart = memoryPointer;
                                state = 1;
                            }
                            break;
                        case 1:
                            if (areBytesSame)
                            {
                                File.AppendAllText("changed-memory-1.txt", currentStart.ToString("X4") + " - " + memoryPointer.ToString("X4") + "\n");
                                changedRanges++;
                                state = 0;
                            }
                            break;
                    }
                    if (byteIndex >= bytes.Length) break;
                }

                memoryPointer += (uint)buffer.Length;
            } while (memoryPointer > lastMemoryPointer);

            Console.WriteLine("changed ranges: " + changedRanges + "(pages: " + changedPages.Count + ")");

            Console.WriteLine("Press enter again after advancing");
            Console.ReadLine();
            Console.WriteLine("comparing...");

            lastMemoryPointer = 0;
            memoryPointer = 0;
            byteIndex = 0;
            state = 0;
            currentStart = 0;
            changedRanges = 0;
            do
            {
                lastMemoryPointer = memoryPointer;
                ReadProcessMemory(processHandle, (int)memoryPointer, buffer, buffer.Length, ref bytesRead);

                if (bytesRead != 0)
                {
                    var areBytesSame = ByteUtil.ByteCompare(buffer, bytes, (uint)byteIndex);
                    byteIndex += buffer.Length;

                    if (!areBytesSame && !changedPages.Contains((int)memoryPointer))
                    {
                        var end = (int)memoryPointer;
                        do
                        {
                            end += buffer.Length;
                            byteIndex += buffer.Length;
                        } while (changedPages.Contains(end));

                        File.AppendAllText("changed-memory-2.txt", memoryPointer.ToString("X4") +  "\n");
                        //DumpSection(memoryPointer.ToString("X4") + ".bin", processHandle, memoryPointer, (int)(end - memoryPointer));
                        changedRanges++;

                        memoryPointer = (uint)(end - buffer.Length);
                        byteIndex -= buffer.Length;
                    }

                    if (byteIndex >= bytes.Length) break;
                }

                memoryPointer += (uint)buffer.Length;
            } while (memoryPointer > lastMemoryPointer);
            Console.WriteLine("changed ranges: " + changedRanges);

            return new List<byte>();
        }

        public static uint Search(int processHandle, byte[] query, uint startIndex = 0, uint endIndex = 0xFFFFFFFF)
        {
            var start = DateTime.Now;

            uint memoryPointer = startIndex;
            int bytesRead = -1;
            byte[] buffer = new byte[0xFF00];

            while (memoryPointer < endIndex)
            {
                if (SearchChunk(processHandle, query, buffer, ref memoryPointer, out bytesRead))
                {
                    if (memoryPointer <= startIndex)
                    {
                        Console.WriteLine("wrapped to beginning");
                        return 0;
                    }
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


        public static uint Search(int processHandle, byte?[] query, uint max = 0xFFFFFFFF, uint startIndex = 0)
        {
            var start = DateTime.Now;

            uint memoryPointer = startIndex;
            int bytesRead = -1;
            byte[] buffer = new byte[0xFF00];

            while (memoryPointer < max)
            {
                if (SearchChunk(processHandle, query, buffer, ref memoryPointer, out bytesRead))
                {
                    if(memoryPointer <= startIndex)
                    {
                        Console.WriteLine("wrapped to beginning");
                        return 0;
                    }
                    return memoryPointer;
                }
            }
            Console.WriteLine("did not find");
            return 0;
        }

        static bool SearchChunk(int processHandle, byte?[] query, byte[] buffer, ref uint memoryPointer, out int bytesRead)
        {
            bytesRead = 0;
            ReadProcessMemory(processHandle, (int)memoryPointer, buffer, buffer.Length, ref bytesRead);
            var memoryStart = memoryPointer;
            var memoryEnd = memoryPointer + buffer.Length - query.Length + 1;
            if (bytesRead == 0)
            {
                memoryPointer = (uint)memoryEnd;
                return false;
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

        public static bool ByteCompare(byte?[] query, byte[] buffer, uint bufferStart)
        {
            for (int i = 0; i < query.Length; i++)
            {
                if (query[i].HasValue && query[i].Value != buffer[bufferStart + i])
                    return false;
            }

            return true;
        }

        public static uint Search(int processHandle, int size, Func<byte[], uint, bool> test, uint startIndex = 0, uint endIndex = 0xFFFFFFFF)
        {
            uint memoryPointer = startIndex;
            int bytesRead = -1;
            byte[] buffer = new byte[0xFF00];

            while (memoryPointer < endIndex)
            {
                if (SearchChunk(processHandle, size, test, buffer, ref memoryPointer, out bytesRead))
                {
                    if (memoryPointer <= startIndex)
                    {
                        Console.WriteLine("wrapped to beginning");
                        return 0;
                    }
                    return memoryPointer;
                }
            }
            Console.WriteLine("did not find");
            return 0;
        }

        static bool SearchChunk(int processHandle, int size, Func<byte[], uint, bool> test, byte[] buffer, ref uint memoryPointer, out int bytesRead)
        {
            bytesRead = 0;
            ReadProcessMemory(processHandle, (int)memoryPointer, buffer, buffer.Length, ref bytesRead);
            var memoryStart = memoryPointer;
            var memoryEnd = memoryPointer + buffer.Length - size + 1;
            if (bytesRead == 0)
            {
                memoryPointer = (uint)memoryEnd;
                return false;
            }

            while (memoryPointer < memoryEnd)
            {
                if (test(buffer, memoryPointer - memoryStart))
                {
                    return true;
                }
                memoryPointer += 0x1;
            }
            return false;
        }
    }
}
