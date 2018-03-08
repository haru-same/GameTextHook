using ED6BaseHook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED6ScriptAnalyzer
{
    class Program
    {
        const string PCVersionScriptsPath = "H:/FALCOM/ed6_3rd_testing/ED6_DT21";
        const string VitaVersionScriptsPath = "C:/Users/gabeculbertson/Documents/GitHub/SoraVoice2/SoraVoiceScripts/en.3rd/out.msg";

        static void Main(string[] args)
        {
            var encoding = Encoding.GetEncoding("SHIFT-JIS");
            var scenes = new Dictionary<string, List<List<string>>>();

            Console.WriteLine("is v string? " + ED6DataUtil.IsVoiceId("#0010610649V", 0));

            //var a0020v = ED6DataUtil.GetLinesFromSoraVoiceTextScript(File.ReadAllText(Path.Combine(VitaVersionScriptsPath, "a0020.txt"), encoding));
            //var a0020p = ED6DataUtil.GetLinesFromSceneFile(File.ReadAllBytes(Path.Combine(PCVersionScriptsPath, "A0020._SN")));
            //var c0301v = ED6DataUtil.GetLinesFromSoraVoiceTextScript(File.ReadAllText(Path.Combine(VitaVersionScriptsPath, "c0301.txt"), encoding));
            //var c0301p = ED6DataUtil.GetLinesFromSceneFile(File.ReadAllBytes(Path.Combine(PCVersionScriptsPath, "C0301._SN")));
            //File.WriteAllLines("out-c0301.txt", c0301v);
            //File.WriteAllLines("out-a0020.txt", a0020v);
            //File.WriteAllLines("out-c0301sn.txt", c0301p);
            //File.WriteAllLines("out-a0020sn.txt", a0020p);

            //Console.WriteLine(ED6Util.StripPrefixAndSuffix("#1679F#1S#6P…………もう十分、驚いてるよ。#2S"));
            //Console.ReadLine();
            //return;

            var testFile = ED6DataUtil.GetLinesFromSceneFile(File.ReadAllBytes(Path.Combine(PCVersionScriptsPath, "C1500._SN")));
            var stripped = new List<string>();
            foreach (var line in testFile) stripped.Add(ED6Util.StripPrefixAndSuffix(line));
            File.WriteAllLines("out-test.txt", stripped);

            //ED6DataUtil.MatchStrings(a0020v, a0020p);
            //ED6DataUtil.MatchStrings(c0301v, c0301p);

            var vitaCount = 0;
            foreach (var file in Directory.GetFiles(VitaVersionScriptsPath))
            {
                var key = Path.GetFileNameWithoutExtension(file).ToLower();
                var fileText = File.ReadAllText(file, encoding);
                if (ED6DataUtil.ContainsVoiceString(fileText))
                {
                    scenes[key] = new List<List<string>>();
                    var lines = ED6DataUtil.GetLinesFromSoraVoiceTextScript(File.ReadAllText(file, encoding));
                    scenes[key].Add(lines);
                    vitaCount++;
                }
            }

            var lineSet = new HashSet<string>();
            var totalLines = 0;
            var uniqueLines = 0;

            var pcCount = 0;
            var matchedCount = 0;
            var unmatchedCount = 0;
            var perSceneNonunique = 0;
            File.WriteAllText("out-mismatch.txt", "");

            File.WriteAllText("voice_map.cpp", @"#include <map>
#include <string>

struct A{
static std::map<std::string, std::map<std::string, std::string>> create_map()
    {
        std::map<std::string, std::map<std::string, std::string>> m;");

            foreach (var file in Directory.GetFiles(PCVersionScriptsPath))
            {
                var key = Path.GetFileNameWithoutExtension(file).ToLower();
                if (scenes.ContainsKey(key))
                {
                    File.AppendAllText("out-mismatch.txt", file + "\n");
                    var lines = ED6DataUtil.GetLinesFromSceneFile(File.ReadAllBytes(file));
                    scenes[key].Add(lines);

                    var match = ED6DataUtil.MatchStrings(scenes[key][0], scenes[key][1]);
                    matchedCount += match.Item1;
                    unmatchedCount += match.Item2;

                    File.AppendAllText("out-nonunique.txt", file + "\n");
                    //ED6DataUtil.BuildCppVoiceDictionaryScene("voice_map.cpp", file, scenes[key][0], scenes[key][1]);
                    ED6DataUtil.BuildSceneResourceFile(file, scenes[key][0], scenes[key][1]);
                    perSceneNonunique += ED6DataUtil.CountNonUniqueVoicedLines(scenes[key][0]);

                    foreach (var line in scenes[key][0])
                    {
                        if (ED6DataUtil.ContainsVoiceString(line))
                        {
                            var strippedline = ED6Util.StripPrefixAndSuffix(line).Replace(" ", "").Replace("　", "");
                            if (!lineSet.Contains(strippedline))
                            {
                                uniqueLines++;
                                lineSet.Add(strippedline);
                            }
                            totalLines++;
                        }
                    }
                    //Console.WriteLine("WARNING: key not found " + key);
                }
                pcCount++;
            }

            File.AppendAllText("voice_map.cpp", @"        return m;
    }
    static const std::map<std::string, std::map<std::string, std::string>> lineToVoice;
};

const std::map<std::string, std::map<std::string, std::string>> A:: lineToVoice =  A::create_map();");

            Console.WriteLine("matched: " + matchedCount + "; unmatched: " + unmatchedCount);

            Console.WriteLine("Unique/total lines: " + uniqueLines + "/" + totalLines);
            Console.WriteLine("Per scene non-unique lines: " + perSceneNonunique);

            Console.WriteLine("PC count: " + pcCount + "; Vita count with voice: " + vitaCount);
            Console.ReadLine();
        }
    }
}
