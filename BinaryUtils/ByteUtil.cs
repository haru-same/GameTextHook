using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryUtils
{
    public static class ByteUtil
    {
        public static bool SubArrayIs(this byte[] bytes, int start, byte[] query)
        {
            if (bytes.Length < start + query.Length) return false;

            for (var i = 0; i < query.Length; i++)
            {
                if (bytes[start + i] != query[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static void Replace(this byte[] bytes, byte query, byte replacement)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == query) bytes[i] = replacement;
            }
        }

        public static uint Search(byte[] bytes, byte[] query)
        {
            uint position = 0;

            while (position < bytes.Length - query.Length)
            {
                if (ByteCompare(query, bytes, position))
                {
                    return position;
                }
                position += 0x1;
            }
            return 0;
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

        public static bool ByteCompare(byte?[] query, byte[] buffer, uint bufferStart)
        {
            for (int i = 0; i < query.Length; i++)
            {
                if (query[i] != null && query[i] != buffer[bufferStart + i])
                    return false;
            }

            return true;
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static string ToByteString(this byte[] bytes, bool space = true)
        {
            return ToByteString(bytes, 0, bytes.Length, space);
        }

        public static string ToByteString(this byte[] bytes, int start, int length, bool space = true)
        {
            var s = "";
            var spaceChar = "";
            if (space) spaceChar = " ";
            for (var i = start; i < bytes.Length && i < start + length; i++)
            {
                s += bytes[i].ToString("X2") + spaceChar;
            }
            return s;
        }
    }
}
