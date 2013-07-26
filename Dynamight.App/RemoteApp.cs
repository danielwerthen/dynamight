using Dynamight.RemoteSlave;
using Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class RemoteApp
    {
        public static void Run(string[] args)
        {
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var window = new BitmapWindow(main.Bounds.Left + main.Width / 2 + 50, 50, 640, 480);
            window.Load();
            window.ResizeGraphics();
            RemoteKinect kinect = new RemoteKinect("localhost", 10500);
            Bitmap map = new Bitmap(100, 100);
            object sync = new object();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int frames = 0;
            kinect.ReceivedSkeletons += (o, e) =>
            {
                frames++;
                e.Skeletons.ToString();
            };
            kinect.ReceivedColorImage += (o, e) =>
            {
                frames++;
                //map = e.Bitmap;
                //lock (sync)
                //    Monitor.Pulse(sync);
            };
            kinect.ReceivedDepthImage += (o, e) =>
            {
                frames++;
                e.Pixels.ToString();
            };
            kinect.Start(Commands.Skeleton);
            //while (true)
            //{
            //    lock (sync)
            //        Monitor.Wait(sync);
            //    window.DrawBitmap(map);
            //}
            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine("{0} frames in {1} ms", frames, sw.ElapsedMilliseconds);
                frames = 0;
                sw.Reset();
                sw.Start();
            }
            Console.ReadLine();
        }
    }
}
