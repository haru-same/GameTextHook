using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinaryUtils;
using Newtonsoft.Json;

namespace ED8ScriptAnalyzer
{
    public class VoiceLine
    {
        public string voice { get; set; }
        public string text { get; set; }
    }

    class Program
    {
        static HashSet<char> SingleCharTerminationChars = new HashSet<char>("KF");
        static HashSet<char> TerminationChars = new HashSet<char>("ACEFJKPST01234689");
        static HashSet<char> BracketTerminationChars = new HashSet<char>("]");

        static HashSet<int> GetVoiceIds(string directory)
        {
            var ids = new HashSet<int>();
            foreach (var file in Directory.GetFiles(directory))
            {
                var filename = Path.GetFileName(file);
                int id;
                if (!int.TryParse(filename.Substring(3, 5), out id))
                {
                    continue;
                }

                ids.Add(id);
            }
            return ids;
        }

        static int TryGetTagEnd(string text, int start)
        {
            if (SingleCharTerminationChars.Contains(text[start + 1]))
            {
                return start + 1;
            }

            var stack = 0;
            var end = start + 2;
            while (end < text.Length)
            {
                if (text[end] == '[')
                {
                    stack++;
                }

                if (stack == 0 && TerminationChars.Contains(text[end]))
                {
                    return end;
                } else if (stack > 0 && text[end] == ']')
                {
                    stack--;
                    if (stack == 0)
                    {
                        return end;
                    }
                }

                end++;
            }
            return -1;
        }

        static string CleanText(string text)
        {
            if (text.Length == 0)
            {
                return "";
            }

            while (text[0] == '#')
            {
                var end = TryGetTagEnd(text, 0);
                if (end < 0)
                {
                    break;
                }
                text = text.Substring(end + 1);
            }

            for (int i = 0; i < text.Length - 4; i++)
            {
                if (text[i] != '#')
                {
                    continue;
                }

                if (text[i + 2] == 'R' || text[i + 3] == 'R')
                {
                    var end = text.IndexOf('#', i + 1);
                    if (end > 0)
                    {
                        text = text.Remove(i, end - i + 1);
                        i--;
                    }
                }
                else if (SingleCharTerminationChars.Contains(text[i + 1]))
                {
                    text = text.Remove(i, 2);
                }
                else if (TerminationChars.Contains(text[i + 2]))
                {
                    text = text.Remove(i, 3);
                }
            }

            return text;
        }

        static string GetRawText(byte[] bytes, int start)
        {
            int end = start;
            while (end < bytes.Length && bytes[end] != 0x02)
            {
                if (bytes[end] == 0x01)
                {
                    bytes[end] = 0x0A;
                }
                end++;
            }

            return Encoding.GetEncoding("SHIFT-JIS").GetString(bytes, start, end - start);
        }

        static List<Tuple<int, int, string, string>> GetVoiceMarkerPositions(byte[] bytes)
        {
            byte?[] seekString1 = { 0, 0x11, null, null, 0, 0 };
            byte?[] seekString2 = { 0x03, 0x11, null, null, 0, 0 };
            var positions = new List<Tuple<int, int, string, string>>();

            for (int i = 0; i < bytes.Length - seekString1.Length; i++)
            {
                if (!ByteUtil.ByteCompare(seekString1, bytes, (uint)i) &&
                    !ByteUtil.ByteCompare(seekString2, bytes, (uint)i))
                {
                    continue;
                }
                var rawText = GetRawText(bytes, i + 6);
                var text = CleanText(rawText);
                if (text == "")
                {
                    continue;
                }
                positions.Add(new Tuple<int, int, string, string>(BitConverter.ToInt32(bytes, i + 2), i, rawText, text));
            }

            return positions;
        }

        static List<Tuple<byte[], byte[]>> GetLines(string filename)
        {
            var fileBytes = File.ReadAllBytes(filename);

            throw new NotImplementedException();
        }

        static void Main(string[] args)
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var voicesDirectory = Path.Combine(programFilesPath, @"Steam/SteamApps/common/Trails of Cold Steel/data/voice/wav_jp");
            var wavIds = GetVoiceIds(voicesDirectory);
            var sceneIds = new HashSet<int>();

            var jsonData = new List<VoiceLine>();

            int foundCount = 0;
            int problematicTags = 0;
            int totalPositions = 0;

            //Console.WriteLine(CleanText("#1P#E[D]#M0いや、その。別に大層な話じゃないんだ。"));
            //Console.ReadLine();
            //return;

            foreach (var filename in Directory.GetFiles(Path.Combine(programFilesPath, @"Steam\SteamApps\common\Trails of Cold Steel\data\scripts\scena\dat\")))
            {
                //if (!filename.Contains("t1510")) continue;
                //var filename = Path.Combine(programFilesPath, @"Steam\SteamApps\common\Trails of Cold Steel\data\scripts\scena\dat\m1260.dat");

                var fileBytes = File.ReadAllBytes(filename);

                var positions = GetVoiceMarkerPositions(fileBytes);
                foreach (var position in positions)
                {
                    string text = position.Item4;
                    if (text.Contains('#') || text.Contains('A'))
                    {
                        Console.WriteLine(filename);
                        Console.WriteLine(position.Item2 + ": " + position.Item1 + "; " + (wavIds.Contains(position.Item1)));
                        Console.WriteLine(text);
                        Console.WriteLine(position.Item3);
                        problematicTags++;
                    }
                    if (wavIds.Contains(position.Item1))
                    {
                        jsonData.Add(new VoiceLine() { text = text, voice = position.Item1.ToString("D5") });
                        foundCount++;
                    } 
                    sceneIds.Add(position.Item1);
                }

                totalPositions += positions.Count;
            }

            var foundInScenes = 0;
            var loggedCount = 0;
            foreach(var id in wavIds)
            {
                if (sceneIds.Contains(id))
                {
                    foundInScenes++;
                }
                else if (loggedCount < 100)
                {
                    Console.WriteLine(id);
                    loggedCount++;
                }
            }

            Console.WriteLine(foundCount + "/" + totalPositions);

            Console.WriteLine("Problematic tags: " + problematicTags);

            Console.WriteLine("wavs in scenes: " + foundInScenes + "/" + wavIds.Count);

            string json = JsonConvert.SerializeObject(jsonData.ToArray());
            File.WriteAllText(@"C:\Users\gabeculbertson\Documents\GitHub\kotoba-najimi\ocr_data\ed8i_lines.json", json);

            Console.WriteLine(voicesDirectory);

            Console.ReadLine();
        }
    }
}
