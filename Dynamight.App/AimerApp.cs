using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class AimerApp
    {
        public static void Run(string[] args)
        {
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var display = new BitmapWindow(main.Bounds.Left + 50, 50, 640, 480);
            display.Load();
            display.ResizeGraphics();
            var display2 = new BitmapWindow(main.Bounds.Left + 690, 50, 640, 480);
            display2.Load();
            display2.ResizeGraphics();

            var displays = new BitmapWindow[] { display, display2 };
            var kinects = KinectSensor.KinectSensors.Where(r => r.Status == KinectStatus.Connected).ToArray();
            foreach (var k in kinects)
            {
                k.Start();
                k.ElevationAngle = 19;
            }
            var cameras = kinects.Select(k => new Camera(k, ColorImageFormat.RgbResolution640x480Fps30)).ToArray();
            while (true)
            {
                cameras.Zip(displays, (c, d) =>
                {
                    d.DrawBitmap(c.TakePicture(0));
                    d.ProcessEvents();
                    return 0;
                }).ToArray();
            }

        }
    }
}
