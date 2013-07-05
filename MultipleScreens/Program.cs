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

			var window = new ProgramWindow(1000, 50, 640, 480);
			var window2 = new ProgramWindow(1000, 530, 640, 480);
			window.ResizeGraphics();
			window2.ResizeGraphics();
			var cp = new CheckerboardProgram();
			var bp = new BitmapProgram();
			window.SetProgram(cp);
			window2.SetProgram(bp);
			cp.SetSize(8, 8);
			double xrot = 0;
			var corners = cp.GetCorners().ToArray();
			window.RenderFrame();
			//cp.Draw().Fill(Color.Black).DrawPoint((1.0 / (double)8.0) * 640.0, (1.0 / (double)5.0) * 480.0).Finish();
			bp.Draw().Fill(Color.Black).DrawPoint(corners, 5).Finish();
			window2.RenderFrame();
			while (true)
			{
				cp.SetTransforms(xrot, -2 * xrot, xrot, 0.7,0.0,0.0);
				window.ProcessEvents();
				window.RenderFrame();
				window2.ProcessEvents();
				window2.RenderFrame();
				corners = cp.GetCorners().ToArray();
				bp.Draw().Fill(Color.Black).DrawPoint(corners, 5).Finish();
				xrot += 0.025;
			}
		}
	}

}
