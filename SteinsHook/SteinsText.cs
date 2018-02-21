using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteinsHook
{
    public class SteinsText
    {
        static bool HasBackZeroes(byte[] content, int start, int count)
        {
            //Console.WriteLine("SE: " + content[start - 2].ToString("X2") + content[start - 1].ToString("X2"));
            if (content[start - 2] == 0x02 && content[start - 1] == 0x80)
                return true;

            for (var i = 0; i < count; i++)
            {
                if (content[start - i - 1] != 0) return false;
            }

            return true;
        }

        static bool HasForwardZeroes(byte[] content, int start, int count)
        {
            if (content[start] == 0x03 && content[start + 1] == 0x80)
                return true;

            for (var i = 0; i < count; i++)
            {
                if (content[start + i] != 0) return false;
            }

            return true;
        }

        public static string ExtractText(byte[] content, int start)
        {
            while (start > 2)
            {
                if (HasBackZeroes(content, start, 4)) break;
                start--;
            }
            var end = start;
            while (end < content.Length - 2)
            {
                if (HasForwardZeroes(content, end, 4)) break;
                end++;
            }
            //Console.WriteLine("start: " + start + " end: " + end);

            return SteinsEncoding.Decode(content.SubArray(start, end - start));
        }
    }
}
