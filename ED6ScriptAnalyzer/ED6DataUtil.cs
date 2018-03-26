using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using BinaryUtils;
using ED6BaseHook;

namespace ED6ScriptAnalyzer
{
    class ED6DataUtil
    {
        const int VoiceStringLength = 12;
        const byte MaxValidOpByte = 0x05;

        static Dictionary<string, string> substitutions = new Dictionary<string, string>()
        {
            { "◆", "" },
            { "～", "" },
            { "《", "" },
            { "》", "" },
            { "判", "分" },
            { "較", "比" },
            { "超", "越" },
            { "一", "１" },
            { "三", "３" },
            { "ス", "す" },
            { "まじ", "マジ" },
            { "例え", "たとえ" },
            { "かソールの薬って", "の薬か解凍カイロって" },
            { "いいの？もらっちゃって……。", "……いいの、もらっちゃって？" },
            { "奴等", "ヤツラ" },
            { "来られる", "来れる" },
            { "………………あ……", "………………………………" },
            { "その名も“軌跡でポン”。いわゆるクイズゲームってやつなんだけど……", "その名も“軌跡でポン”。" },

        };

        static Encoding shiftJISEncoding = Encoding.GetEncoding("SHIFT-JIS");

        public static bool IsVoiceId(string text, int index)
        {
            if (text.Length < index + VoiceStringLength) return false;

            if (text[index] != '#') return false;
            if (text[index + 11] != 'v' && text[index + 11] != 'V') return false;

            for (var i = 1; i < VoiceStringLength - 1; i++)
            {
                if (!text[index + i].IsASCIINumeral()) return false;
            }

            return true;
        }

        public static string PrepareSoraVoiceTextScriptLine(string text)
        {
            var sb = new StringBuilder();
            var inByte = false;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '[')
                {
                    inByte = true;
                    continue;
                }
                if (text[i] == ']')
                {
                    inByte = false;
                    continue;
                }
                if (inByte) continue;
                sb.Append(text[i]);
            }
            return sb.ToString();
        }

        public static int IsValidED6StringChar(byte[] bytes, int index, bool en = false)
        {
            if (bytes[index] == 0) return 0;

            if (index < bytes.Length - 1 && EncodingUtil.IsShiftJISChar(bytes, index))
            {
                return 2;
            }
            else if (index < bytes.Length - 1 && bytes[index] == 0x07 && bytes[index + 1] == 0x00)
            {
                return 2;
            }
            if (bytes[index] <= MaxValidOpByte || bytes[index] == '#' || bytes[index] == ':' || bytes[index] == '.' || bytes[index] == ' ' || ((char)bytes[index]).IsASCIINumeralOrLetter())
            {
                return 1;
            }

            if (en && bytes[index] >= 0x20 && bytes[index] <= 0x7E) return 1;

            return 0;
        }

        static bool CommandByteInRange(byte[] bytes, int index, int count)
        {
            for (var i = index; i < index + count; i++)
            {
                if (bytes[i] <= MaxValidOpByte) return true;
            }
            return false;
        }

        public static int IsValidED6String(byte[] bytes, int index, bool en = false)
        {
            if (bytes[index] != '#')
            {
                if (!en && !EncodingUtil.IsShiftJISChar(bytes, index)) return 0;
                if (en && (bytes[index] < 0x20 || bytes[index] > 0x7E)) return 0;
            }
            if (CommandByteInRange(bytes, index, 4)) return 0;

            var start = index;
            var hasJaChar = false;
            while (index < bytes.Length)
            {
                if (bytes[index] == 0x02)
                {
                    if (!en && !hasJaChar) return 0;
                    if (index - start > 2) return index - start;
                    else return 0;
                }

                var nextCharLength = IsValidED6StringChar(bytes, index, en);
                if (nextCharLength == 0) return 0;

                hasJaChar = hasJaChar || EncodingUtil.IsShiftJISChar(bytes, index);

                index += nextCharLength;
            }
            return 0;
        }

        public static string GetED6String(byte[] bytes, int index)
        {
            var outBytes = new List<byte>();
            while (index < bytes.Length)
            {
                if (bytes[index] == 0x02) break;
                else if (bytes[index] >= 0x20) outBytes.Add(bytes[index]);
                index++;
            }
            var s = shiftJISEncoding.GetString(outBytes.ToArray());
            return s;
        }

        public static bool ContainsVoiceString(string text)
        {
            for (var i = 0; i < text.Length - VoiceStringLength; i++)
            {
                if (IsVoiceId(text, i)) return true;
            }
            return false;
        }

        public static List<string> GetLinesFromSoraVoiceTextScript(string text)
        {
            var split = text.Split('\n');
            var outLines = new List<string>();
            var lineContinued = false;
            for (var i = 0; i < split.Length; i++)
            {
                var line = split[i];

                if (line == ";----------------------------------------------------------------------------------")
                {
                    i += 5;
                    continue;
                }

                if (line != "")
                {
                    if (lineContinued)
                    {
                        outLines[outLines.Count - 1] += PrepareSoraVoiceTextScriptLine(line);
                    }
                    else
                    {
                        outLines.Add(PrepareSoraVoiceTextScriptLine(line));
                    }
                    lineContinued = line.Replace("[x02][x01]", "").Replace("[x01][x02]", "").Contains("[x01]");
                }
            }
            return outLines;
        }

        public static List<string> GetLinesFromSceneFile(byte[] fileBytes, bool en = false)
        {
            var outLines = new List<string>();
            var index = 0;
            while (index < fileBytes.Length)
            {
                var nextStringLength = IsValidED6String(fileBytes, index, en);
                if (nextStringLength != 0)
                {
                    //Console.WriteLine("word: " + nextStringLength);
                    outLines.Add(GetED6String(fileBytes, index));
                    index += nextStringLength + 1;
                }
                index++;
            }
            return outLines;
        }

        public static string LineToKey(string line)
        {
            return shiftJISEncoding.GetBytes(line).ToByteString(false);
        }

        public static string Canonicalize(string line)
        {
            var s = line.Replace(" ", "").Replace("　", "");
            s = ED6Util.StripPrefixAndSuffix(s);
            s = ED6Util.StripFurigana(s);
            s = ED6Util.StripSizeChanges(s);
            foreach (var sub in substitutions)
            {
                s = s.Replace(sub.Key, sub.Value);
            }
            return s;
        }

        public static Tuple<int, int> MatchStrings(List<string> voicedStrings, List<string> otherStrings)
        {
            var strippedOtherStrings = new HashSet<string>();
            foreach (var s in otherStrings)
            {
                strippedOtherStrings.Add(ED6Util.StripPrefixAndSuffix(s));
            }

            //var currentOtherIndex = 0;
            var matchedLines = 0;
            var unmatched = 0;
            for (var i = 0; i < voicedStrings.Count; i++)
            {
                if (ED6Util.GetVoicePrefix(voicedStrings[i]) != null)
                {
                    var key = ED6Util.StripPrefixAndSuffix(voicedStrings[i]).Replace(" ", "").Replace("　", "");
                    if (strippedOtherStrings.Contains(key))
                    {
                        matchedLines++;
                    }
                    else
                    {
                        unmatched++;
                        File.AppendAllText("out-mismatch.txt", voicedStrings[i] + "; " + key + "\n");
                    }
                }
            }
            Console.WriteLine("matched: " + matchedLines + " (unmatched: " + unmatched + ")");
            return new Tuple<int, int>(matchedLines, unmatched);
        }

        public static int CountNonUniqueVoicedLines(List<string> voicedStrings)
        {
            var nonUniqueLines = 0;
            var lineSet = new Dictionary<string, string>();

            foreach (var line in voicedStrings)
            {
                if (ContainsVoiceString(line))
                {
                    var strippedline = ED6Util.StripPrefixAndSuffix(line).Replace(" ", "").Replace("　", "");
                    var voiceKey = ED6Util.GetVoicePrefix(line);
                    if (lineSet.ContainsKey(strippedline) && lineSet[strippedline] != voiceKey)
                    {
                        nonUniqueLines++;
                        File.AppendAllText("out-nonunique.txt", line + "\n");
                    }
                    else
                    {
                        lineSet[strippedline] = voiceKey;
                    }
                }
            }

            return nonUniqueLines;
        }

        public static void BuildCppVoiceDictionaryScene(string codeFile, string sceneFile, List<string> voicedStrings, List<string> sceneFileStrings)
        {
            var fileKey = Path.GetFileNameWithoutExtension(sceneFile).PadRight(8) + "._SN";
            var fileLines = new List<string>();
            fileLines.Add("//" + fileKey);

            var addedKeys = new HashSet<string>();

            for (var i = 0; i < voicedStrings.Count; i++)
            {
                var voiceKey = ED6Util.GetVoicePrefix(voicedStrings[i]);
                if (voiceKey != null)
                {
                    var lineKey = shiftJISEncoding.GetBytes(LineToKey(voicedStrings[i])).ToByteString(false);
                    if (!addedKeys.Contains(lineKey))
                    {
                        var line = string.Format("m[\"{0}\"][\"{1}\"] = \"{2}\";", fileKey, lineKey, voiceKey);
                        fileLines.Add(line);
                        addedKeys.Add(lineKey);
                    }
                }
            }

            File.AppendAllLines(codeFile, fileLines);
        }

        public static double GetSimilarity(string s1, string s2)
        {
            var h1 = new HashSet<char>(s1);
            var h2 = new HashSet<char>(s2);
            var total = Math.Max(h1.Count, h2.Count);
            h1.IntersectWith(h2);
            return (double)h1.Count / total;
        }

        static void OutputBestMatches(string sceneFile, string key, string line, Dictionary<string, string> dict)
        {
            var lines = new List<string>();

            lines.Add("// " + sceneFile + " match for:");
            lines.Add(line);
            lines.Add(key);
            lines.Add("...");
            var bestMatches = dict.Keys.OrderByDescending((s) => GetSimilarity(s, key)).Take(5);
            foreach (var item in bestMatches)
            {
                lines.Add(item + ": " + GetSimilarity(item, key));
            }
            lines.Add("");
            lines.Add("");
            File.AppendAllLines("output-best-match.txt", lines);
        }

        static string GetBestMatch(string key, Dictionary<string, string> dict)
        {
            var bestMatch = dict.Keys.OrderByDescending((s) => GetSimilarity(s, key)).First();
            if (GetSimilarity(key, bestMatch) >= 0.5)
            {
                return bestMatch;
            }
            return null;
        }

        public static int BuildSceneResourceFile(string sceneFile, List<string> voicedStrings, List<string> sceneFileStrings)
        {
            if (!Directory.Exists("scenes"))
            {
                Directory.CreateDirectory("scenes");
            }

            var sceneFileKeys = new Dictionary<string, string>();
            foreach (var s in sceneFileStrings)
            {
                var lineKey = Canonicalize(s); //shiftJISEncoding.GetBytes(LineToKey(s)).ToByteString(false);
                sceneFileKeys[lineKey] = s;
            }

            var fileKey = Path.GetFileNameWithoutExtension(sceneFile).PadRight(8) + "._SN";
            var fileLines = new List<string>();

            var addedKeys = new HashSet<string>();
            var unmatchedStringsCount = 0;

            for (var i = 0; i < voicedStrings.Count; i++)
            {
                var voiceKey = ED6Util.GetVoicePrefix(voicedStrings[i]);
                if (voiceKey != null)
                {
                    var canonicalLine = Canonicalize(voicedStrings[i]);
                    if (!addedKeys.Contains(canonicalLine))
                    {
                        addedKeys.Add(canonicalLine);

                        if (sceneFileKeys.ContainsKey(canonicalLine))
                        {
                            var key = LineToKey(sceneFileKeys[canonicalLine]);
                            fileLines.Add(string.Format("{0}\t{1}", key, voiceKey));
                        }
                        else
                        {
                            var bestMatch = GetBestMatch(canonicalLine, sceneFileKeys);
                            if (bestMatch != null)
                            {
                                var key = LineToKey(sceneFileKeys[bestMatch]);
                                fileLines.Add(string.Format("{0}\t{1}", key, voiceKey));
                            }
                            else
                            {
                                OutputBestMatches(fileKey, canonicalLine, voicedStrings[i], sceneFileKeys);
                                unmatchedStringsCount++;
                            }
                        }
                    }
                }
            }

            File.AppendAllText("out-counts.txt", fileKey + ": " + (sceneFileStrings.Count - voicedStrings.Count) + " diff; " + voicedStrings.Count + "v/" + sceneFileStrings.Count + "t\n");

            File.WriteAllLines("scenes/" + fileKey, fileLines);
            return unmatchedStringsCount;
        }




        static string GetKeyFileLine(string line1, string line2, HashSet<string> addedVoiceIds)
        {
            string voiceKey, lineKey;
            if (ContainsVoiceString(line1))
            {
                voiceKey = ED6Util.GetVoicePrefix(line1);
                lineKey = LineToKey(line2);
            }
            else if (ContainsVoiceString(line2))
            {
                voiceKey = ED6Util.GetVoicePrefix(line2);
                lineKey = LineToKey(line1);
            }
            else
            {
                return null;
            }
            addedVoiceIds.Add(voiceKey);
            return string.Format("{0}\t{1}", lineKey, voiceKey);
        }




        public static Tuple<string, string> TryDequeueUnmatched(string key, List<Tuple<string,string>> unmatched)
        {
            foreach(var item in unmatched)
            {
                if(GetSimilarity(key, item.Item1) > 0.5)
                {
                    unmatched.Remove(item);
                    return item;
                }
            }
            return null;
        }

        public static Tuple<int, int> BuildSceneResourceFile_Cmp(string sceneFile, List<string> voicedStrings, List<string> sceneFileStrings)
        {
            if (!Directory.Exists("scenes")) Directory.CreateDirectory("scenes");
            var fileKey = Path.GetFileNameWithoutExtension(sceneFile).PadRight(8) + "._SN";
            var fileLines = new List<string>();

            var lines1 = voicedStrings;
            var lines2 = sceneFileStrings;
            var canonicalizedLines1 = lines1.Select(s => Canonicalize(s)).ToList();
            var canonicalizedLines2 = lines2.Select(s => Canonicalize(s)).ToList();
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

            var addedVoiceIds = new HashSet<string>();
            var unmatchedQueue = new List<Tuple<string, string>>();

            var unmatchedLines = new List<string>();
            var totalGap = Math.Abs(canonicalizedLines1.Count - canonicalizedLines2.Count);
            var outLines = new List<string>();
            var l1Index = 0;
            var l2Index = 0;
            var matchedStringCount = 0;
            var unmatchedStringsCount = 0;
            for (; l2Index < canonicalizedLines2.Count; l2Index++)
            {
                var v1 = canonicalizedLines1.GetSafely(l1Index, "");
                var v2 = canonicalizedLines2.GetSafely(l2Index, "");
                var gap = totalGap - (l1Index - l2Index);
                var findResult = canonicalizedLines1.FindInRange(l1Index, gap + 10, s => GetSimilarity(s, v2) > 0.5);

                if (findResult >= 0)
                {
                    while (l1Index < findResult)
                    {
                        unmatchedQueue.Add(new Tuple<string, string>(canonicalizedLines1.GetSafely(l1Index, ""), lines1.GetSafely(l1Index, "")));
                        l1Index++;
                    }

                    l1Index = findResult;
                    var resLine = GetKeyFileLine(lines1[l1Index], lines2[l2Index], addedVoiceIds);
                    if (resLine != null)
                    {
                        matchedStringCount++;
                        fileLines.Add(resLine);
                    }
                    l1Index++;
                }
                else if (ContainsVoiceString(lines2.GetSafely(l2Index)))
                {
                    var dequeued = TryDequeueUnmatched(v2, unmatchedQueue);
                    if (dequeued != null)
                    {
                        var resLine = GetKeyFileLine(dequeued.Item2, lines2[l2Index], addedVoiceIds);
                        if (resLine != null)
                        {
                            matchedStringCount++;
                            fileLines.Add(resLine);
                        }
                    }
                    else
                    {
                        var vid = ED6Util.GetVoicePrefix(lines2.GetSafely(l2Index));
                        if (!addedVoiceIds.Contains(vid))
                        {
                            unmatchedStringsCount++;

                            unmatchedLines.Add("// " + sceneFile + " match for:");
                            unmatchedLines.Add(lines2.GetSafely(l2Index));
                            unmatchedLines.Add(canonicalizedLines2.GetSafely(l2Index));
                            addedVoiceIds.Add(vid);
                        }
                    }
                }
            }

            for (; l1Index < canonicalizedLines1.Count; l1Index++)
            {
                if (ContainsVoiceString(lines1.GetSafely(l1Index)))
                {
                    var vid = ED6Util.GetVoicePrefix(lines1.GetSafely(l1Index));
                    if (!addedVoiceIds.Contains(vid))
                    {
                        unmatchedStringsCount++;

                        unmatchedLines.Add("// " + sceneFile + " match for:");
                        unmatchedLines.Add(lines1.GetSafely(l1Index));
                        unmatchedLines.Add(canonicalizedLines1.GetSafely(l1Index));
                        addedVoiceIds.Add(vid);
                    }
                }
            }

            File.AppendAllLines("output-best-match.txt", unmatchedLines);
            File.WriteAllLines("scenes/" + fileKey, fileLines);

            File.AppendAllLines("scenes.txt", fileLines);
            return new Tuple<int, int>(matchedStringCount, unmatchedStringsCount);
        }
    }
}
