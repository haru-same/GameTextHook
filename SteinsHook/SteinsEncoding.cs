using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HookUtils;

namespace SteinsHook
{
    public class SteinsEncoding
    {
        const char AnchorCharValue = '０';
        const short AnchorCharIndex = 128;
        static short anchorStart = 0;
        static string characters;
        static Dictionary<char, short> characterToCode;

        static SteinsEncoding()
        {
            if(!File.Exists("encoding_dict.txt") && File.Exists("Game.exe"))
            {
                DumpDictionary();
            }

            characters = File.ReadAllText("encoding_dict.txt", Encoding.Unicode);
            characterToCode = new Dictionary<char, short>();
            short i = 0;
            while(characters[i] != AnchorCharValue)
            {
                i++;
            }
            anchorStart = i;
            while(i < characters.Length)
            {
                characterToCode[characters[i]] = (short)(AnchorCharIndex + i - anchorStart);
                i++;
            }
        }

        public static void DumpDictionary()
        {
            var fileBytes = File.ReadAllBytes("Game.exe");
            var query = new byte[] { 0xC0, 0x00, 0xC1, 0x00, 0xC2, 0x00, 0xC3, 0x00, 0xC4, 0x00, 0xC5, 0x00, 0xC6, 0x00, 0xC7, 0x00, };

            for (var i = 0; i < fileBytes.Length - query.Length; i++)
            {
                if (MemoryUtil.ByteCompare(query, fileBytes, (uint)i))
                {
                    var outBuffer = new List<byte>();
                    var j = i;
                    while (j - i < 14000)
                    {
                        outBuffer.Add(fileBytes[j]);
                        j++;
                    }
                    while (!(fileBytes[j] == 0 && fileBytes[j + 1] == 0))
                    {
                        outBuffer.Add(fileBytes[j]);
                        j++;
                    }
                    File.WriteAllBytes("encoding_dict.txt", outBuffer.ToArray());
                    return;
                }
            }
        }

        static string ShortToChar(ushort input)
        {
            var index = input + anchorStart - AnchorCharIndex;
            if (index >= 0 && index <= characters.Length)
                return characters[index].ToString();
            //else if (input == 3)
            //    return "\n";
            else
                return "";
        }

        public static byte[] Encode(string input)
        {
            byte[] outBytes = new byte[input.Length * 2];
            for(var i = 0; i < input.Length; i++)
            {
                var val = characterToCode[input[i]];
                outBytes[i * 2] = (byte)val;
                outBytes[i * 2 + 1] = (byte)(val >> 8); 
            }
            return outBytes;
        }

        public static string Decode(byte[] input)
        {
            StringBuilder sb = new StringBuilder();
            for(var i = 0; i < input.Length - 1; i += 2)
            {
                var val = BitConverter.ToUInt16(input, i);
                sb.Append(ShortToChar(val));
            }
            return sb.ToString();
        }
    }
}
