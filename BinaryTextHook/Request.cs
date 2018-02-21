using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BinaryTextHook
{
    public class Request
    {
        public static void MakeRequest(string url, string text)
        {
            try
            {
                url = url + HttpUtility.UrlEncode(text);

                if (url.Contains("%00"))
                {
                    Console.WriteLine("ERROR: Illegal character");
                    File.AppendAllText("out_url.txt", "IGNORING: " + url + "\n");
                    return;
                }

                File.AppendAllText("out_url.txt", url + "\n");

                string html = string.Empty;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }

                Console.WriteLine(html);
            } catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
