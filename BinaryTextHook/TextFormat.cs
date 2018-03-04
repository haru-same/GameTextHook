using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTextHook
{
    public class TextFormat
    {
        public static List<string> GetStringsWithEOT(byte[] text)
        {
            var start = 0;
            var end = 0;
            List<string> strings = new List<string>();
            for(end = 0; end < text.Length; end++)
            {
                if (text[end] == 0x03)
                {
                    start = end + 1;
                } else if(text[end] == 0x02)
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
            while(start > 2)
            {
                if (content[start - 2] == 0 && content[start - 1] == 0) break;
                start--;
            }
            var end = start;
            while(end < content.Length - 2)
            {
                if (content[end + 1] == 0 && content[end + 2] == 0) break;
                end++;
            }
            //Console.WriteLine("start: " + start + " end: " + end);

            return GetStringsWithEOT(content.SubArray(start, end - start + 1));
        }
    }
}
