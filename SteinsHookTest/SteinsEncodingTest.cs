using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteinsHook;
using BinaryTextHook;

namespace SteinsHookTest
{
    [TestClass]
    public class SteinsEncodingTest
    {
        [TestMethod]
        public void TestEncode()
        {
            var input = "右耳に当て";
            var actualOutput = SteinsEncoding.Encode(input);
            var expectedOutput = new byte[] { 0xFB, 0x02, 0x02, 0x07, 0x01, 0x02, 0xAC, 0x0A, 0xFC, 0x01 };

            Console.WriteLine(expectedOutput.ToByteString());
            Console.WriteLine(actualOutput.ToByteString());
            Assert.IsTrue(MemoryUtil.ByteCompare(actualOutput, expectedOutput, 0));
        }

        [TestMethod]
        public void TestDecode()
        {
            var input = new byte[] { 0xFB, 0x02, 0x02, 0x07, 0x01, 0x02, 0xAC, 0x0A, 0xFC, 0x01 };
            var actualOutput = SteinsEncoding.Decode(input);
            var expectedOutput = "右耳に当て";

            Assert.AreEqual(actualOutput, expectedOutput);
        }
    }
}
