using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Graphics.Projection;
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
			var window = new ProgramWindow(1000, 50, 640, 480);
			window.ResizeGraphics();
			window.Load();

			var program = new PointCloudProgram(15f);
			window.SetProgram(program);
			program.Draw().All((xp, yp) =>
			{
				var x = 0.5 - xp;
				var y = 0.5 - yp;
				var i = Math.Sqrt(x * x + y * y) * 2.5;
				if (i > 1)
					i = 1;
				i = Math.Pow(1 - i, 3);
				byte ii = (byte)(i * 255);
				return Color.FromArgb(ii, 255, 255, 255);
			}).Finish();
			program.SetProjection(pc);
		}
	}

}
