using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using BinaryTextHook;
using ED6BaseHook;

namespace ED6SCHook
{
    class Program
    {
        static string GetFromResources(string resourceName)
        {
            Assembly assem = Assembly.GetCallingAssembly();
            using (Stream stream = assem.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        static void HandleNewText(string text, Dictionary<string, string> metadata)
        {
            //Console.WriteLine("HANDLER: " + text);
            if (metadata == null) metadata = new Dictionary<string, string>();

            string voice = null;
            metadata.TryGetValue("voice", out voice);

            if (voice != null) Console.WriteLine("VOICE:" + voice);

            metadata["game"] = "ed6sc";

            Request.MakeRequest("http://localhost:1414/new-text?text=", text, metadata);
        }

        static void Main(string[] args)
        {
            var processHandle = ProcessUtil.OpenProcess("ed6_win2_DX9");
            ED6Util.ED6Monitor(processHandle, HandleNewText);

            Console.ReadLine();
        }

    }
};