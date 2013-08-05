using Emgu.CV;
using Emgu.CV.Structure;
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

	public class Camera : IDisposable
	{
		CancellationTokenSource cancellation;
		Thread thread;
		private readonly object syncRoot = new object();
		Bitmap lastBitmap;
        public Camera()
        {
            cancellation = new CancellationTokenSource();
            thread = new Thread(() =>
            {
                var token = cancellation.Token;
                double intensity = 0.5;
                while (!token.IsCancellationRequested)
                {
                    lock (syncRoot)
                    {
                        lastBitmap = new Bitmap(500, 500);
                        Graphics.QuickDraw.Start(lastBitmap)
                            .All((x, y) =>
                            {
                                var d = (int)(255 * intensity * (Math.Abs(0.5 - x) + Math.Abs(0.5 - y)));
                                return Color.FromArgb(d, d, d);
                            })
                            .Finish();
                        intensity *= 0.95;
                        if (intensity < 0.0001)
                            intensity = 0.5;
                        Monitor.Pulse(syncRoot);
                    }
                    Thread.Sleep(60);
                }
            });
            thread.Start();
        }

        KinectSensor sensor;
        public Camera(KinectSensor sensor, Microsoft.Kinect.ColorImageFormat imageFormat)
		{
			cancellation = new CancellationTokenSource();
            this.sensor = sensor;
            thread = new Thread(() =>
            {
                var token = cancellation.Token;
                sensor.ColorStream.Enable(imageFormat);
                EventHandler<ColorImageFrameReadyEventArgs> onFrame = (o, e) =>
                    {
                        var frame = HandleFrame(e);
                        lock (syncRoot)
                        {
                            if (lastBitmap != null)
                                lastBitmap.Dispose();
                            lastBitmap = frame;
                            Monitor.Pulse(syncRoot);
                        }
                    };
                sensor.ColorFrameReady += onFrame;
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        sensor.ColorFrameReady -= onFrame;
                        sensor.Stop();
                        return;
                    }
                 }
            });

			thread.Start();
		}

        private void Awake()
        {
            lock (syncRoot)
                Monitor.PulseAll(syncRoot);
        }

        public Bitmap TakePicture(int discardCount = 0)
		{
            Bitmap pic = null;
            for (var i = 0; i <= discardCount; i++)
            {
                if (pic != null)
                    pic.Dispose();
                pic = _takePicture();

            }
            pic.RotateFlip(RotateFlipType.RotateNoneFlipX);
            return pic;
            //Bitmap previous = null;
            //while (true)
            //{
            //    var taken = _takePicture();
            //    if (previous != null)
            //    {
            //        using (var a = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(taken).SmoothBlur(45, 45))
            //        using (var b = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(previous).SmoothBlur(45, 45))
            //        {
            //            var d = a - b;
            //            double[] min, max;
            //            Point[] minp, maxp;
            //            d.MinMax(out min, out max, out minp, out maxp);
            //            d.Dispose();
            //            previous.Dispose();
            //            if (max.All(row => row <= maxIntensityDifference))
            //            {
            //                return taken;
            //            }
            //            previous = taken;
            //        }
            //    }
            //    else
            //        previous = taken;
            //}
		}

		private Bitmap _takePicture()
		{
            while (true)
            {
                lock (syncRoot)
                {
                    if (Monitor.Wait(syncRoot, 2000))
                    {
                        if (lastBitmap != null)
                        {
                            var temp = lastBitmap;
                            lastBitmap = null;
                            return temp;
                        }
                    }

                }
                this.sensor.Stop();
                while (this.sensor.Status != KinectStatus.Connected)
                    Thread.Sleep(2000);
                this.sensor.Start();
            }
		}

		public Size Size
		{
            get { return new Size(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight); }
		}

		static Bitmap ImageToBitmap(ColorImageFrame Image)
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

		static Bitmap HandleFrame(ColorImageFrameReadyEventArgs args)
		{
			using (var frame = args.OpenColorImageFrame())
			{
				if (frame == null)
					return null;
				return ImageToBitmap(frame);
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			cancellation.Cancel();
			thread.Join();
		}

		#endregion
	}

	public class Camera2
	{
		KinectSensor sensor;

		public Camera2(KinectSensor sensor, Microsoft.Kinect.ColorImageFormat imageFormat)
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
			}
			return result;
		}

		public Size Size
		{
			get { return new Size(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight); }
		}
	}
}
