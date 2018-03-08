using HookUtils;
using System;

namespace SteinsHook
{
    class SteinsMonitor
    {
        static uint textLocation = 33771968;//21123520;

        static void UpdateContent(int processPointer, byte[] buffer, ref string content)
        {
            MemoryUtil.Fill(processPointer, buffer, textLocation - 10);
            var newContent = SteinsText.ExtractText(buffer, 10);
            if (newContent != content)
            {
                content = newContent;
                Console.WriteLine(content);
                Request.MakeRequest("http://localhost:80/new-text?text=", content);
            }
        }

        public static void Run(int processPointer)
        {
            var buffer = new byte[512];
            var content = "";

            while (true)
            {
                UpdateContent(processPointer, buffer, ref content);
                System.Threading.Thread.Sleep(51);
                //MemoryUtil.Fill(processPointer, newVisisbleBuffer, DialogueMemoryPointerToVisibleMemoryPointer(textStart));
                //if (visibleBuffer[0] != newVisisbleBuffer[0] || visibleBuffer[1] != newVisisbleBuffer[1])
                //{
                //    Console.WriteLine("VISIBLE: " + newVisisbleBuffer.ToByteString());
                //    visibleBuffer[0] = newVisisbleBuffer[0];
                //    visibleBuffer[1] = newVisisbleBuffer[1];

                //    if (visibleBuffer[0] == 0 || visibleBuffer[1] == 0)
                //    {
                //        Console.WriteLine("SEEKING NEW HEAD...");
                //        uint newStart = 0;
                //        while (newStart == 0)
                //        {
                //            newStart = SearchDialogueStart(processPointer);
                //            Thread.Sleep(50);
                //        }
                //        Console.WriteLine("NEW HEAD IS: " + newStart.ToString("X4"));
                //        textStart = newStart;
                //        currentDialogue = GetActiveDialogue(processPointer, textStart);
                //        UpdateContent(processPointer, currentDialogue, textStart, ref content);
                //    }
                //}

                //while (!MemoryUtil.HasMemoryChanged(processPointer, currentDialogue, buffer, textStart - BeforeDialogueBlockSize))
                //{
                //    System.Threading.Thread.Sleep(50);
                //}

                //currentDialogue = GetActiveDialogue(processPointer, textStart);
                //UpdateContent(processPointer, currentDialogue, textStart, ref content);
            }
        }
    }
}
