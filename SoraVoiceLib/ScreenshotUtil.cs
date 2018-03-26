using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoraVoiceLib
{
    class ScreenshotUtil
    {
        const string ImagesDir = "screenshotimgs";
        const int RollbackCount = 20;

        static void RollbackImages()
        {
            if (File.Exists(Path.Combine(ImagesDir, RollbackCount + ".png")))
            {
                File.Delete(Path.Combine(ImagesDir, RollbackCount + ".png"));
            }

            for (int i = RollbackCount - 1; i >= 0; i--)
            {
                var filename = Path.Combine(ImagesDir, i + ".png");
                var newFilename = Path.Combine(ImagesDir, (i + 1) + ".png");
                if (File.Exists(filename))
                {
                    File.Move(filename, newFilename);
                }
            }
        }

        public static void TakeScreenshot()
        {
            var pName = "ed6_win3_DX9";
            Process p;
            try
            {
                p = Process.GetProcessesByName(pName)[0];
            }
            catch
            {
                return;
            }

            if (!Directory.Exists(ImagesDir))
            {
                Directory.CreateDirectory(ImagesDir);
            }

            Screenshot ps = new Screenshot();

            var img = new Bitmap(ps.CaptureWindow(p.MainWindowHandle));
            var uglyBorderSize = 3;
            var barHeight = uglyBorderSize;
            var lastColor = img.GetPixel(uglyBorderSize, uglyBorderSize);
            var color = lastColor;
            do
            {
                lastColor = color;
                barHeight++;
                color = img.GetPixel(uglyBorderSize, barHeight);
            } while (color == lastColor);
            Console.WriteLine("size: " + barHeight);

            var cropped = img.Clone(new Rectangle(uglyBorderSize, barHeight, img.Width - 2 * uglyBorderSize, img.Height - barHeight - uglyBorderSize), PixelFormat.DontCare);

            RollbackImages();
            cropped.Save(Path.Combine(ImagesDir, "0.png"), ImageFormat.Png);
        }
    }
}
