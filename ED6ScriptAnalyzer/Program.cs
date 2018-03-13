using BinaryUtils;
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

        static Encoding shiftJisEncoding = Encoding.GetEncoding("SHIFT-JIS");

        static List<string> GetLinesFromFile(string file)
        {
            var ext = Path.GetExtension(file);
            Console.WriteLine(ext);
            switch (ext.ToLower())
            {
                case "._sn":
                    return ED6DataUtil.GetLinesFromSceneFile(File.ReadAllBytes(Path.Combine(PCVersionScriptsPath, file)));
                case ".txt":
                    return ED6DataUtil.GetLinesFromSoraVoiceTextScript(File.ReadAllText(Path.Combine(VitaVersionScriptsPath, file), shiftJisEncoding));
            }
            throw new Exception("Invalid extension: " + file);
        }

        static void Compare(List<string> lines1, List<string> lines2)
        {
            List<string> canonicalizedLines1 = lines1.Select(s => ED6DataUtil.Canonicalize(s)).ToList();
            List<string> canonicalizedLines2 = lines2.Select(s => ED6DataUtil.Canonicalize(s)).ToList();
            File.WriteAllLines("out-compare1.txt", lines1);
            File.WriteAllLines("out-compare2.txt", lines2);
            if (canonicalizedLines2.Count > canonicalizedLines1.Count)
            {
                var tmp = canonicalizedLines1;
                canonicalizedLines1 = canonicalizedLines2;
                canonicalizedLines2 = tmp;

                tmp = lines1;
                lines1 = lines2;
                lines2 = tmp;
            }

            Console.WriteLine("len1: " + canonicalizedLines1.Count + " len2: " + canonicalizedLines2.Count);

            var unmatchedQueue = new List<Tuple<string, string>>();

            var compareFileStrings = new List<string>();
            var matchedFileStrings = new List<string>();
            var totalGap = Math.Abs(canonicalizedLines1.Count - canonicalizedLines2.Count);
            var outLines = new List<string>();
            var l1Index = 0;
            var l2Index = 0;
            for (; l2Index < canonicalizedLines2.Count; l2Index++)
            {
                var v1 = canonicalizedLines1.GetSafely(l1Index, "");
                var v2 = canonicalizedLines2.GetSafely(l2Index, "");

                var gap = totalGap - (l1Index - l2Index);
                var findResult = canonicalizedLines1.FindInRange(l1Index, gap + 10, s => ED6DataUtil.GetSimilarity(s, v2) > 0.5);
                //Console.WriteLine(canonicalizedLines1.Count + ": " + l1Index + "; " + canonicalizedLines2.Count + ": " + l2Index + "; " + totalGap);
                //Console.WriteLine(gap + "; " + findResult + ": " + l1Index + "; " + l2Index);
                if (findResult >= 0)
                {
                    while (l1Index < findResult)
                    {
                        unmatchedQueue.Add(new Tuple<string, string>(canonicalizedLines1.GetSafely(l1Index, ""), lines1.GetSafely(l1Index, "")));
                        compareFileStrings.Add(canonicalizedLines1.GetSafely(l1Index, "") + "\t\t<>");
                        l1Index++;
                    }
                    l1Index = findResult;
                    matchedFileStrings.Add(canonicalizedLines1.GetSafely(l1Index, ""));
                    matchedFileStrings.Add(v2);
                    matchedFileStrings.Add("");
                    l1Index++;
                }
                else
                {
                    var dequeued = ED6DataUtil.TryDequeueUnmatched(v2, unmatchedQueue);
                    if (dequeued != null)
                    {
                        matchedFileStrings.Add(dequeued.Item1);// canonicalizedLines1.GetSafely(l1Index, ""));
                        matchedFileStrings.Add(v2);
                        matchedFileStrings.Add("");
                    }
                    else
                    {
                        if (ED6DataUtil.ContainsVoiceString(lines2.GetSafely(l2Index)))
                        {
                            compareFileStrings.Add("VOICE: " + v2 + "\t\t<>");
                        }
                        else
                        {
                            compareFileStrings.Add("<>\t\t" + v2);
                        }
                    }
                }
                //compareFileStrings.Add(canonicalizedLines1.GetSafely(l1Index, "") + "\t\t" + v2);
            }

            for (; l1Index < canonicalizedLines1.Count; l1Index++)
            {
                if (ED6DataUtil.ContainsVoiceString(lines1.GetSafely(l1Index)))
                {
                    compareFileStrings.Add("VOICE: " + canonicalizedLines1.GetSafely(l1Index, "") + "\t\t<>");
                }
                else
                {
                    compareFileStrings.Add(canonicalizedLines1.GetSafely(l1Index, "") + "\t\t<>");
                }
            }

            File.WriteAllLines("out-compare.txt", compareFileStrings);
            File.WriteAllLines("out-matched.txt", matchedFileStrings);
        }

        static void Compare(string file1, string file2)
        {
            Compare(GetLinesFromFile(file1), GetLinesFromFile(file2));
        }

        static void Main(string[] args)
        {
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

            //Console.WriteLine(ED6Util.StripPrefixAndSuffix("#1717F#3S#15Aポーリィ～！#2S"));

            var bytes = ByteUtil.HexStringToByteArray("070582A282E2814182BD82BE82CC817389F18EFB95A8817482BE814202");
            Console.WriteLine(ED6DataUtil.IsValidED6String(bytes, 0));
            Console.WriteLine(ED6DataUtil.IsValidED6String(bytes, 1));
            Console.WriteLine(ED6DataUtil.IsValidED6String(bytes, 2));
            var toStr = ED6DataUtil.GetED6String(bytes, 2);
            Console.WriteLine(toStr);
            Console.WriteLine(ED6DataUtil.Canonicalize(toStr));

            Console.WriteLine(ED6Util.StripFurigana(ED6Util.StripPrefixAndSuffix("#154060J#0030590623V#2B#23Z#49B#106Z#1642Fまったく、そんな技#2Rワ#術#2Rザ#どこで覚えたのよ……#123e")));
            Console.WriteLine(ED6Util.StripSizeChanges("abc#40Wdef#20W"));

            Console.WriteLine(ED6DataUtil.PrepareSoraVoiceTextScriptLine("[x07][x05]#130805J#0800500265V#1B#13Z#30B#68Zいや、ただの《回収物》だ。[x02][x03]"));

            Compare("U7000_1._SN", "U7000_1.txt");

            Console.ReadLine();
            //return;

            //ED6DataUtil.MatchStrings(a0020v, a0020p);
            //ED6DataUtil.MatchStrings(c0301v, c0301p);

            var vitaCount = 0;
            foreach (var file in Directory.GetFiles(VitaVersionScriptsPath))
            {
                var key = Path.GetFileNameWithoutExtension(file).ToLower();
                var fileText = File.ReadAllText(file, shiftJisEncoding);
                if (ED6DataUtil.ContainsVoiceString(fileText))
                {
                    scenes[key] = new List<List<string>>();
                    var lines = ED6DataUtil.GetLinesFromSoraVoiceTextScript(File.ReadAllText(file, shiftJisEncoding));
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
            File.WriteAllText("out-counts.txt", "");
            File.WriteAllText("output-best-match.txt", "");
            File.WriteAllText("scenes.txt", "");

            foreach (var file in Directory.GetFiles(PCVersionScriptsPath))
            {
                var key = Path.GetFileNameWithoutExtension(file).ToLower();
                if (scenes.ContainsKey(key))
                {
                    File.AppendAllText("out-mismatch.txt", file + "\n");
                    var lines = ED6DataUtil.GetLinesFromSceneFile(File.ReadAllBytes(file));
                    scenes[key].Add(lines);

                    //var match = ED6DataUtil.MatchStrings(scenes[key][0], scenes[key][1]);
                    //matchedCount += match.Item1;
                    //unmatchedCount += match.Item2;

                    File.AppendAllText("out-nonunique.txt", file + "\n");
                    //ED6DataUtil.BuildCppVoiceDictionaryScene("voice_map.cpp", file, scenes[key][0], scenes[key][1]);
                    var buildResult = ED6DataUtil.BuildSceneResourceFile_Cmp(file, scenes[key][0], scenes[key][1]);
                    matchedCount += buildResult.Item1;
                    unmatchedCount += buildResult.Item2;
                    //perSceneNonunique += ED6DataUtil.CountNonUniqueVoicedLines(scenes[key][0]);

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

            Console.WriteLine("matched: " + matchedCount + "; unmatched: " + unmatchedCount);

            Console.WriteLine("Unique/total lines: " + uniqueLines + "/" + totalLines);
            Console.WriteLine("Per scene non-unique lines: " + perSceneNonunique);

            Console.WriteLine("PC count: " + pcCount + "; Vita count with voice: " + vitaCount);
            Console.ReadLine();
        }
    }
}
