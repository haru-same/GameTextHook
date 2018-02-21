using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTextHook
{
    public class ByteUtil
    {
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
    }
}
