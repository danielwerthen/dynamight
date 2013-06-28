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

			var window = new ProgramWindow(500, 500);
			window.ResizeGraphics();
			StructuredLightProgram bp;
			window.SetProgram(bp = new StructuredLightProgram());
			bp.SetPhaseMod(1, 0, true, Color.FromArgb(0,255,0));
			window.RenderFrame();
			while (true)
			{
				window.ProcessEvents();
				window.RenderFrame();
			}
		}
	}

}
