using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED6BaseHook
{
    public class Voice
    {
        static string[] voiceIds = null;

        public static string GetOggVoiceId(int index)
        {
            if(voiceIds == null)
            {
                voiceIds = Properties.Resources.VoiceIDs.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            }
            if(index >= 0 && index < voiceIds.Length)
            {
                return voiceIds[index];
            }
            return null;
        }
    }
}
