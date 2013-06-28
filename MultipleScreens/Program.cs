using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultipleScreens
{
	class Program
	{
		static double Intensity(double dt, int step)
		{
			return Math.Floor(dt * Math.Pow(2, step)) % 2;
		}

		static IEnumerable<double> Range(double to, double from = 0, double step = 1)
		{
			if (to < from)
				yield break;
			var i = from;
			while (i <= to)
				yield return i+= step;
		}
		static IEnumerable<int> Range(int to, int from = 0, int step = 1)
		{
			if (to < from)
				yield break;
			var i = from;
			while (i <= to)
				yield return i += step;
		}

		static void Main(string[] args)
		{
			int idx = 0;
			int width = 500;
			int pre = 352;
			int maxStep = (int)Math.Floor(Math.Log(width, 2)) + 1;
			var t = Range(width).Select(row => row / (double)width).Select(row => Intensity(row, 9)).ToArray();
			for (var i = 1; i <= maxStep - 3; i++)
			{
				var range = Range(width).Select(row => row / (double)width).Select(row => Intensity(row, i)).ToArray();
				var hit = range[pre] > 0;
				idx = idx << 1;
				idx = idx | (hit ? 1 : 0);
			}
			var t1 = idx << 3;
			var t2 = (idx / Math.Pow(2, maxStep - 3)) * width;

			Camera cam = new Camera();

			var window = new ProgramWindow(1000, 50, 500, 500);
			window.ResizeGraphics();
			BitmapProgram bp = new BitmapProgram();
			window.SetProgram(bp);
			var xs = Range(400, 100, 25).ToArray();
			var ys = Range(400, 100, 25).ToArray();
			var points = new PointF[xs.Length * ys.Length];
			Random r = new Random();
			Func<double> ra = () => r.NextDouble() * r.NextDouble() * 15;
			for (var yi = 0; yi < ys.Length; yi++)
				for (var xi = 0; xi < xs.Length; xi++)
				{
					points[xi + yi * xs.Length] = new PointF((float)(ra() + xs[xi]), (float)(ra() + ys[yi]));
				}
			var ss = GridSmoothing.Smooth(points , new Size(xs.Length, ys.Length));
			bp.Draw().Fill(Color.Black)
				.Color(Color.White)
				//.DrawPoint(points, 5)
				.Color(Color.Red)
				.DrawPoint(ss, 5)
				.Finish();

			window.RenderFrame();
			bp.Draw().Fill(Color.Black)
				.DrawPoint(ss, 5).Finish();

			window.RenderFrame();
			while (true)
			{
				window.ProcessEvents();
				window.RenderFrame();
			}
		}
	}

}
