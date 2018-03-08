using BinaryUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookUtils
{
    public class BinaryDecoder
    {
        public bool IsPossible(string input, byte[] buffer, uint bufferIndex)
        {
            var characterDictionary = new Dictionary<char, ushort>();
            var seenShorts = new HashSet<ushort>();
            for (int i = 0; i < input.Length; i++)
            {
                ushort thisShortValue = buffer.GetShort((int)(bufferIndex + (i * 2)));

                var shouldBeInPreviousButIsnt = characterDictionary.ContainsKey(input[i]) && thisShortValue != characterDictionary[input[i]];
                var shouldNotBeInPreviousButIs = !characterDictionary.ContainsKey(input[i]) && seenShorts.Contains(thisShortValue);

                if (shouldBeInPreviousButIsnt || shouldNotBeInPreviousButIs)
                {
                    return false;
                }

                characterDictionary[input[i]] = thisShortValue;
                seenShorts.Add(thisShortValue);
            }
            return true;
        }

        public void CheckPossible(int processHandle, string input)
        {
            var buffer = new byte[0x10000];
            uint bufferStart = 0;
            uint memoryPointer = 0;
            var possibleCount = 0;

            File.WriteAllText(input + ".txt", "");

            while(memoryPointer < 0xFFFFFF00)
            {
                if(memoryPointer >= bufferStart + buffer.Length - (input.Length * 2) - 1)
                {
                    MemoryUtil.Fill(processHandle, buffer, memoryPointer);
                    bufferStart = memoryPointer;
                }

                if (IsPossible(input, buffer, memoryPointer - bufferStart))
                {
                    var outString = buffer.ToByteString((int)(memoryPointer - bufferStart), input.Length * 2);
                    Console.WriteLine(outString);
                    using(var writer = File.AppendText(input + ".txt"))
                    {
                        writer.WriteLine(outString);
                    }
                    possibleCount++;
                }

                memoryPointer++;
            }
            Console.WriteLine("READ TO: " + memoryPointer);
            Console.WriteLine("POSSIBLE: " + possibleCount);
        }
    }
}
