using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryUtils
{
    public static class EncodingUtil
    {
        const ushort MinShiftJIS1 = 0x813f;
        const ushort MaxShiftJIS1 = 0x9fee + 16;
        const ushort MinShiftJIS2 = 0xe03f;//0xFC90;
        const ushort MaxShiftJIS2 = 0xEEF0;//0xFC90;

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
            if (bytes[index + 1] < 0x3f) return false;
            var shortVal = GetShort(bytes, index);
            return (shortVal >= MinShiftJIS1 && shortVal < MaxShiftJIS1) 
                || (shortVal >= MinShiftJIS2 && shortVal <= MaxShiftJIS2);
        }
    }
}
