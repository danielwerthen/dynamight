using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class PicTakeHelperApp
    {
        public static void Run(string[] args)
        {
            int[] ra = Range.OfInts(52).ToArray();
            string test = "decode.exe options.ini " + string.Join(" ", ra.Select(i => string.Format("picture-{0:000}.bmp", i)).ToArray());
            test.ToArray();
            string dir = @"C:\Users\ASUS\git\procamtools-v1\Debug";

            Thread.Sleep(100);
            ExCamera xcam = new ExCamera();
            Projector proj = new Projector();
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var display = new BitmapWindow(main.Bounds.Left + 50, 50, 1280, 960);
            display.Load();
            display.ResizeGraphics();
            proj.DrawBackground(Color.White);
            int c = 0;
            
            foreach (var file in Directory.GetFiles(dir, "pattern-*.bmp"))
            {
                var map = new Bitmap(Path.Combine(dir, file));
                proj.DrawBitmap(map);
                var pic = xcam.TakePicture();
                display.DrawBitmap(pic);
                var bits = (Bitmap)pic.Clone(new Rectangle(0, 0, pic.Width, pic.Height), PixelFormat.Format32bppRgb);
                bits.Save(Path.Combine(dir, string.Format("picture-{0:000}.bmp", c++)), ImageFormat.Bmp);
            }
            display.Close();
            proj.Close();
            xcam.Dispose();
        }
    }
}
