using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GinharuHook
{
    class Program
    {
        const int ContentOffset = 92;

        static byte[] buffer = new byte[1000];
        static byte[] StringTerminator = new byte[] { 0x00, 0x00 };

        static string GetContent(int processPointer, uint start)
        {
            MemoryUtil.DumpSection(start.ToString("X4") + ".bytes", processPointer, start + ContentOffset, 1000);
            MemoryUtil.Fill(processPointer, buffer, start + ContentOffset);
            var stringLength = ByteUtil.Search(buffer, StringTerminator);
            var content = Encoding.GetEncoding("UTF-16").GetString(buffer.SubArray(0, (int)stringLength));
            return content;
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;

            int processPtr = ProcessUtil.OpenProcess("SiglusEngine");

            Console.WriteLine("高い夜空から");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("高い夜空から");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("こんな天気");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("それでも今年");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("スコップです");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("白い吐息");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("春休みが昨日");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("そして何より");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("かいてる");
            //var query = Encoding.GetEncoding("UTF-16").GetBytes("object[84].child[26].frame_action");
            //var query = new byte[] { 0xF1, 0x03, 0x1F, 0xA6, 0x00, 0x13, 0x00, 0x88 };
            //var query = new byte[] { 0x00, 0x00, 0x8D, 0x1B, 0xFA, 0xA6, 0x00, 0x06, 0x00, 0x8A };

            var query = Encoding.GetEncoding("UTF-16").GetBytes(@"いまは名字が");
            //var query = new byte[] { 0x01, 0x88 };
            //var startIndex = MemoryUtil.Search(processPtr, query, startIndex: 0x100000);
            //MemoryUtil.Fill(processPtr, buffer, startIndex + 0);
            //var stringLength = ByteUtil.Search(buffer, StringTerminator);
            //var content = Encoding.GetEncoding("UTF-16").GetString(buffer.SubArray(0, (int)stringLength));

            //Request.MakeRequest("http://localhost:1414/new-text?text=", content);

            uint startIndex = 10;// 0x5000000;
            uint max = uint.MaxValue; // 0x25000000;
            var lastContent = "";
            while (true)
            {
                startIndex = MemoryUtil.Search(processPtr, query, startIndex: startIndex + 2, max: max);

                MemoryUtil.DumpSection(startIndex.ToString("X4") + "_5.bytes", processPtr, startIndex - 500, 1000);

                Console.WriteLine("start: " + startIndex.ToString("X4"));

                //if(startIndex == 0)
                //{
                //    System.Threading.Thread.Sleep(567);
                //    continue;
                //}

                //var newContent = GetContent(processPtr, startIndex);
                //if(lastContent != newContent && newContent != "")
                //{
                //    lastContent = newContent;
                //    Console.WriteLine("content: " + newContent);
                //    Request.MakeRequest("http://localhost:1414/new-text?text=", newContent);
                //}

                System.Threading.Thread.Sleep(567);
            }

            Console.WriteLine("loop done");

            Console.ReadLine();
        }
    }
}
