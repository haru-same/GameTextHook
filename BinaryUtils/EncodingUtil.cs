using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryUtils
{
    public static class EncodingUtil
    {
        const ushort MinShiftJIS = 0x813f;
        const ushort MaxShiftJIS = 0xEEF0;//0xFC90;

        static byte[] shortBuffer = new byte[] { 0, 0 };

        public static ushort GetShort(this byte[] bytes, int start)
        {
            shortBuffer[1] = bytes[start];
            shortBuffer[0] = bytes[start + 1];
            return BitConverter.ToUInt16(shortBuffer, 0);
        }

        public static bool IsShiftJISChar(byte[] bytes, int index)
        {
            if (index >= bytes.Length - 1) return false;
            if (bytes[index] == 0xEF) return false;
            var shortVal = GetShort(bytes, index);
            return shortVal >= MinShiftJIS && shortVal < MaxShiftJIS;
        }
    }
}
