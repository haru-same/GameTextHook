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

        public static string GetString(this byte[] bytes, int start)
        {
            return GetString(bytes, start, Encoding.Unicode);
        }

        public static string GetString(this byte[] bytes, int start, Encoding encoding)
        {
            for(var i = start; i < bytes.Length; i += 2)
            {
                if(bytes[i] == 0 && bytes[i+1] == 0)
                {
                    return encoding.GetString(bytes.SubArray(start, i - start));
                }
            }
            return "";
        }
    }
}
