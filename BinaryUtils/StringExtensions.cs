using System;
using System.Collections.Generic;
using System.Text;

namespace BinaryUtils
{
    public static class StringExtensions
    {
        public static bool IsASCIINumeral(this char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsLowercaseASCIILetter(this char c)
        {
            return c >= 'a' && c <= 'z';
        }

        public static bool IsUppercaseASCIILetter(this char c)
        {
            return c >= 'A' && c <= 'Z';
        }

        public static bool IsASCIILetter(this char c)
        {
            return IsLowercaseASCIILetter(c) || IsUppercaseASCIILetter(c);
        }

        public static bool IsASCIINumeralOrLetter(this char c)
        {
            return IsASCIINumeral(c) || IsASCIILetter(c);
        }

        static bool IsTerminated(byte[] bytes, int index, int terminatorCount)
        {
            switch (terminatorCount)
            {
                case 1:
                    return bytes[index] == 0;
                case 2:
                    return bytes[index] == 0 && bytes[index + 1] == 0;
                default:
                    for (int i = index; i < bytes.Length && i < index + terminatorCount; i++)
                    {
                        if (bytes[i] != 0) return false;
                    }
                    return true;
            }
        }

        public static string GetString(this byte[] bytes, int start)
        {
            return GetString(bytes, start, Encoding.Unicode);
        }

        public static string GetString(this byte[] bytes, int start, Encoding encoding, int terminatorCount = 2)
        {
            int end;
            return GetString(bytes, start, encoding, terminatorCount, out end);
        }

        public static string GetString(this byte[] bytes, int start, Encoding encoding, int terminatorCount, out int end)
        {
            for (var i = start; i < bytes.Length; i += terminatorCount)
            {
                if (IsTerminated(bytes, i, terminatorCount))
                {
                    end = i;
                    return encoding.GetString(bytes.SubArray(start, i - start));
                }
            }
            end = -1;
            return "";
        }

        public static List<string> GetStrings(this byte[] bytes, int start, Encoding encoding, int terminatorCount = 2, int maxCount = 1)
        {
            var strings = new List<string>();
            int end = start;
            do
            {
                var s = GetString(bytes, end, encoding, terminatorCount, out end);
                if (end == -1) return strings;

                strings.Add(s);

                maxCount--;
                while (end < bytes.Length && bytes[end] == 0) end++;
            } while (maxCount > 0 && end != -1);
            return strings;
        }
    }
}
