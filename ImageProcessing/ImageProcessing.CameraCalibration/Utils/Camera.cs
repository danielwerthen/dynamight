using Microsoft.Kinect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{

    public class RobustCamera
    {
        readonly object synclock = new object();
        Bitmap currentPic;
        Camera camera;
        public RobustCamera(KinectSensor sensor, Microsoft.Kinect.ColorImageFormat imageFormat)
        {
            var thread = new Thread(() =>
            {
                camera = new Camera(sensor, imageFormat);
                while (true)
                {
                    lock (synclock)
                    {
                        if (currentPic != null)
                            currentPic.Dispose();
                        currentPic = camera.TakePicture();
                    }
                }
            });
            thread.Start();
        }

        public Bitmap TakePicture()
        {
            lock (synclock)
                return (Bitmap)currentPic.Clone();
        }

        public Size Size
        {
            get { return camera.Size; }
        }
    }

    public class Camera
    {
        KinectSensor sensor;

        public Camera(KinectSensor sensor, Microsoft.Kinect.ColorImageFormat imageFormat)
        {
            this.sensor = sensor;
            sensor.ColorStream.Enable(imageFormat);
            if (!sensor.IsRunning)
                sensor.Start();
        }

        public Bitmap TakePicture()
        {
            Thread.Sleep(700);
            Bitmap last = null;
            while (true)
            {
                Bitmap now = _TakePicture(1);
                if (now == null && last != null)
                    return last;
                else
                {
                    if (last != null)
                        last.Dispose();
                    last = now;
                }
            }
        }

        Bitmap ImageToBitmap(ColorImageFrame Image)
        {
            byte[] pixeldata = new byte[Image.PixelDataLength];
            Image.CopyPixelDataTo(pixeldata);
            Bitmap bmap = new Bitmap(Image.Width, Image.Height, PixelFormat.Format32bppRgb);
            BitmapData bmapdata = bmap.LockBits(
                new Rectangle(0, 0, Image.Width, Image.Height),
                ImageLockMode.WriteOnly,
                bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            Marshal.Copy(pixeldata, 0, ptr, Image.PixelDataLength);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }
        private Bitmap _TakePicture(int wait = 1000)
        {
            Bitmap result;
            using (var frame = sensor.ColorStream.OpenNextFrame(wait))
            {
                if (frame == null)
                    return null;
                return ImageToBitmap(frame);
                //detect if the format has changed to resize buffer
                var imageBuffer = new byte[frame.PixelDataLength];
                // We must obtain a pointer to the first scanline of the top-down data.
                // This happens to be the start of the buffer.
                unsafe
                {
                    fixed (void* p = imageBuffer)
                    {
                        IntPtr ptr = new IntPtr(p);
                        PixelFormat format = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
                        result = new Bitmap(frame.Width, frame.Height, (4 * frame.Width), format, ptr);
                    }
                }
                frame.CopyPixelDataTo(imageBuffer);
            }
            return result;
        }

        public Size Size
        {
            get { return new Size(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight); }
        }
    }
}
