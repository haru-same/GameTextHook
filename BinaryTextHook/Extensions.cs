using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTextHook {
    public static class Extensions {
        public static T[] SubArray<T>(this T[] data, int index, int length) {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static bool SubArrayIs(this byte[] bytes, int start, byte[] query)
        {
            if (bytes.Length < start + query.Length) return false;

            for(var i = 0; i < query.Length; i++)
            {
                if(bytes[start + i] != query[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static void Replace(this byte[] bytes, byte query, byte replacement)
        {
            for(var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == query) bytes[i] = replacement;
            }
        }

        public static string ToByteString(this byte[] bytes)
        {
            return ToByteString(bytes, 0, bytes.Length);
        }

        public static string ToByteString(this byte[] bytes, int start, int length)
        {
            var s = "";
            for (var i = start; i < bytes.Length && i < start + length; i++)
            {
                s += bytes[i].ToString("X2") + " ";
            }
            return s;
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
