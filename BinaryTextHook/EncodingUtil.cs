using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinaryTextHook
{
    public static class EncodingUtil
    {
        const ushort MinShiftJIS = 0x813f;
        const ushort MaxShiftJIS = 0xEEF0;

        static byte[] shortBuffer = new byte[] { 0, 0 };

        public static ushort GetShort(this byte[] bytes, int start)
        {
            shortBuffer[1] = bytes[start];
            shortBuffer[0] = bytes[start + 1];
            return BitConverter.ToUInt16(shortBuffer, 0);
        }

        public static bool IsShiftJISChar(byte[] bytes, int index)
        {
            var shortVal = GetShort(bytes, index);
            return shortVal >= MinShiftJIS && shortVal <= MaxShiftJIS;
        }
    }
}
