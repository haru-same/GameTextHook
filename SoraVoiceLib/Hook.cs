﻿using ED6BaseHook;
using HookUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SoraVoiceLib
{
    public class Hook
    {
        static string lastGame = "";
        static string lastText = "";
        static string lastVoice = "";

        public static void DelayAction(int millisecond, Action action)
        {
            var timer = new DispatcherTimer();
            timer.Tick += delegate

            {
                action.Invoke();
                timer.Stop();
            };

            timer.Interval = TimeSpan.FromMilliseconds(millisecond);
            timer.Start();
        }

        public static void SendToInterface(string game, string text, string voice)
        {
            text = ED6Util.RemoveCommandCharacters(text);
            text = ED6Util.StripTags(text);

            if (lastText == text || lastText.Contains(text))
            {
                return;
            }
            lastGame = game;
            lastVoice = voice;
            lastText = text;

            var metadata = new Dictionary<string, string>();
            metadata["game"] = game;
            if (voice != null && voice != "") metadata["voice"] = voice;
            Request.MakeRequest("http://localhost:1414/new-text?text=", text, metadata, doLogging: false);
        }
    }
}
