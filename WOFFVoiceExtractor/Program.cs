using BinaryTextHook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOFFVoiceExtractor
{
    class Program
    {
        const string ArchivePath = "H:/game hack/SonicAudioTools/vo.acb";

        static List<string> GetVoiceFilenames()
        {
            var bytes = File.ReadAllBytes(ArchivePath);
            var query = Encoding.UTF8.GetBytes("CueIndex");
            var lines = new List<string>();
            var start = ByteUtil.Search(bytes, query);
            for (int i = (int)(start + query.Length); i < bytes.Length; i++)
            {
                if (bytes[i] == 0 && bytes[i + 1] == 0) break;
                if (bytes[i] == 0)
                {
                    var s = bytes.GetString(i + 1, Encoding.UTF8, 1);
                    lines.Add(s);
                }
            }
            return lines;
        }

        static void Main(string[] args)
        {
            var filenames = GetVoiceFilenames();

            Console.WriteLine("before: " + filenames.Count);
            var start = filenames.FindIndex(s => s == "S0001_010001se0");
            filenames.RemoveRange(start - 85, 84);

            start = filenames.FindIndex(s => s == "fd0001ta0");
            filenames.RemoveRange(start - 122 + 84, 121 - 84);
            Console.WriteLine("after: " + filenames.Count);

            Console.WriteLine(filenames[4540] + " expected: S0001_040012le0");
            Console.WriteLine(filenames[10196] + " expected: fd0903ra0");

            var basePath = Path.GetDirectoryName(ArchivePath);
            var inPath = Path.Combine(basePath, "vo");
            var outPath = Path.Combine(basePath, "vo_out");
            Console.WriteLine(basePath);
            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }

            DirectoryInfo outPathInfo = new DirectoryInfo(outPath);
            foreach (var file in outPathInfo.GetFiles())
            {
                file.Delete();
            }

            var inFiles = Directory.GetFiles(inPath);
            var indices = new HashSet<int>();
            foreach (var f in inFiles)
            {
                var baseName = Path.GetFileNameWithoutExtension(f);
                var index = int.Parse(baseName.Split('_')[0]);
                if (indices.Contains(index))
                {
                    Console.WriteLine("WARNING: duplicate index " + index);
                }

                var outFilePath = Path.Combine(outPath, filenames[index] + ".hca");
                if (!File.Exists(outFilePath))
                {
                    File.Copy(f, outFilePath, true);
                }
            }

            Console.WriteLine("Done. Press enter to close.");
            Console.ReadLine();
        }
    }
}
