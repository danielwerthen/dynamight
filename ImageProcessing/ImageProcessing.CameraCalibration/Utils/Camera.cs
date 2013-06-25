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

	public class RobustCamera : IDisposable
	{
		CancellationTokenSource cancellation;
		Thread thread;
		private readonly object syncRoot = new object();
		Bitmap lastBitmap;
		public RobustCamera()
		{
			cancellation = new CancellationTokenSource();
			//thread = new Thread(() =>
			//{
			//	var token = cancellation.Token;
			//	sensor.ColorStream.Enable(imageFormat);
			//	if (!sensor.IsRunning)
			//		sensor.Start();
			//	EventHandler<ColorImageFrameReadyEventArgs> onFrame = (o, e) =>
			//		{
			//			lock (syncRoot)
			//			{
			//				if (lastBitmap == null)
			//					lastBitmap.Dispose();
			//				lastBitmap = HandleFrame(e);
			//				Monitor.Pulse(syncRoot);
			//			}
			//		};
			//	sensor.ColorFrameReady += onFrame;
			//	while (true)
			//	{
			//		if (token.IsCancellationRequested)
			//		{
			//			sensor.ColorFrameReady -= onFrame;
			//			sensor.Stop();
			//		}
			//	}
			//});
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
							.All((x,y) => {
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

		public Bitmap TakePicture(double differenceThreshold = 10000)
		{
			Bitmap previous = null;
			while (true)
			{
				var taken = _takePicture();
				if (previous != null)
				{
					var a = new Emgu.CV.Image<Emgu.CV.Structure.Rgb, byte>(taken);
					var b = new Emgu.CV.Image<Emgu.CV.Structure.Rgb, byte>(previous);
					var t = a.DotProduct(b);
					if (t < differenceThreshold)
						return taken;
					previous.Dispose();
					previous = taken;
				}
				else
					previous = taken;
			}
		}

		private Bitmap _takePicture()
		{
			lock (syncRoot)
			{
				Monitor.Wait(syncRoot);
				if (lastBitmap != null)
				{
					var temp = lastBitmap;
					lastBitmap = null;
					return temp;
				}
			}
			return null;
		}

		public Size Size
		{
			get { return new Size(1, 1); }
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
