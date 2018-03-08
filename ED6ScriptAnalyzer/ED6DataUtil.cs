using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BinaryUtils;
using ED6BaseHook;

namespace ED6ScriptAnalyzer
{
    class ED6DataUtil
    {
        const int VoiceStringLength = 12;

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

        public static string RemoveBoundryBytesFromText(string text)
        {
            var sb = new StringBuilder();
            var inByte = false;
            for (var i = 0; i < text.Length; i++)
            {
                if (inByte) continue;
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
                sb.Append(text[i]);
            }
            return sb.ToString();
        }

        public static int IsValidED6StringChar(byte[] bytes, int index)
        {
            if (bytes[index] == 0) return 0;

            if (index < bytes.Length - 1 && EncodingUtil.IsShiftJISChar(bytes, index))
            {
                return 2;
            }
            if (bytes[index] <= 0x03 || bytes[index] == '#' || bytes[index] == ' ' || ((char)bytes[index]).IsASCIINumeralOrLetter())
            {
                return 1;
            }
            return 0;
        }

        public static int IsValidED6String(byte[] bytes, int index)
        {
            var start = index;
            var hasJaChar = false;
            while (index < bytes.Length)
            {
                if (bytes[index] == 0x02)
                {
                    if (hasJaChar) return index - start;
                    else return 0;
                }

                var nextCharLength = IsValidED6StringChar(bytes, index);
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
                if (bytes[index] == 0x02)
                {
                    break;
                }
                if (bytes[index] == 0)
                {
                    Console.WriteLine("HIT unexpected 0");
                    break;
                }
                else if (bytes[index] <= 0x03)
                {
                    index++;
                }
                else if (bytes[index] == '#' || bytes[index] == ' ' || ((char)bytes[index]).IsASCIINumeralOrLetter())
                {
                    outBytes.Add(bytes[index]);
                    index += 1;
                }
                else if (EncodingUtil.IsShiftJISChar(bytes, index))
                {
                    outBytes.Add(bytes[index]);
                    outBytes.Add(bytes[index + 1]);
                    index += 2;
                }
            }
            return shiftJISEncoding.GetString(outBytes.ToArray()).Replace(" ", "").Replace("　", "");
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
                        outLines[outLines.Count - 1] += RemoveBoundryBytesFromText(line);
                    }
                    else
                    {
                        outLines.Add(RemoveBoundryBytesFromText(line));
                    }
                    lineContinued = line.Contains("[x01]");
                }
            }
            return outLines;
        }

        public static List<string> GetLinesFromSceneFile(byte[] fileBytes)
        {
            var outLines = new List<string>();
            var index = 0;
            while (index < fileBytes.Length)
            {
                var nextStringLength = IsValidED6String(fileBytes, index);
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
            return ED6Util.StripPrefixAndSuffix(line).Replace(" ", "").Replace("　", "");
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

        public static void BuildSceneResourceFile(string sceneFile, List<string> voicedStrings, List<string> sceneFileStrings)
        {
            if (!Directory.Exists("scenes"))
            {
                Directory.CreateDirectory("scenes");
            }

            var fileKey = Path.GetFileNameWithoutExtension(sceneFile).PadRight(8) + "._SN";
            var fileLines = new List<string>();

            var addedKeys = new HashSet<string>();

            for (var i = 0; i < voicedStrings.Count; i++)
            {
                var voiceKey = ED6Util.GetVoicePrefix(voicedStrings[i]);
                if (voiceKey != null)
                {
                    var lineKey = shiftJISEncoding.GetBytes(LineToKey(voicedStrings[i])).ToByteString(false);
                    if (!addedKeys.Contains(lineKey))
                    {
                        var line = string.Format("{0}\t{1}", lineKey, voiceKey);
                        fileLines.Add(line);
                        addedKeys.Add(lineKey);
                    }
                }
            }

            File.WriteAllLines("scenes/" + fileKey, fileLines);
        }
    }
}
