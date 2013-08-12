using Graphics;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{
	public class Projector
	{
		public ProgramWindow window;
		public Bitmap bitmap;
		public StructuredLightProgram slp;
		public BitmapProgram bp;
        public CheckerboardProgram cp;
		public Projector()
		{
            window = ProgramWindow.OpenOnSecondary();

			window.SetProgram(slp = new StructuredLightProgram());
			bp = new BitmapProgram(bitmap = new Bitmap(window.Width, window.Height));
            cp = new CheckerboardProgram();
		}

        public void Close()
        {
            window.Close();
            window.Dispose();
        }

		public void SetBounds(RectangleF bounds)
		{
			//window.SetBounds(bounds);
		}

		public void DrawBackground()
		{
			DrawBackground(Color.Black);
		}

		public void DrawBackground(Color color)
		{
			window.SetProgram(slp);
			slp.SetIdentity(color);
			window.RenderFrame();
		}

		public void DrawBitmap(Bitmap bitmap)
		{
			window.SetProgram(bp);
			bp.LoadBitmap(bitmap);
			window.RenderFrame();
		}

		public void DrawBinary(int step, bool vertical, Color color)
		{
			window.SetProgram(slp);
			slp.SetBinary(step, vertical, color);
			window.RenderFrame();
		}

		public void DrawGrey(int step, bool vertical, int offset, Color color)
		{
			window.SetProgram(slp);
			slp.SetGrey(step, vertical, offset, color);
			window.RenderFrame();
		}

        public PointF[] DrawCheckerboard(System.Drawing.Size pattern, double rotx, double roty, double rotz, double scale = 1, double offsetx = 0, double offsety = 0)
        {
            return DrawCheckerboard(pattern, rotx, roty, rotz, scale, scale, offsetx, offsety);
        }
        public PointF[] DrawCheckerboard(System.Drawing.Size pattern, double rotx, double roty, double rotz, double scalex, double scaley, double offsetx = 0, double offsety = 0)
        {
            window.SetProgram(cp);
            cp.SetSize(pattern.Width, pattern.Height);
            cp.SetTransforms(rotx, roty, rotz, scalex, scaley, offsetx, offsety);
            window.RenderFrame();
            return cp.GetCorners().ToArray();
        }


		public void DrawScanLine(int Step, int Length, bool Rows)
		{
			//QuickDraw.Start(bitmap)
			//    .Fill(Color.Black)
			//    .DrawShape(new System.Windows.Media.RectangleGeometry(new Rect(!Rows ? Step * Length : 0, Rows ? Step * Length : 0, !Rows ? Length : bitmap.Width, Rows ? Length : bitmap.Height)))
			//    .Finish();
			//window.LoadBitmap(bitmap);
			//window.RenderFrame();
		}

		public void DrawPoints(float[][] points, float size)
		{
			window.SetProgram(bp);
			var draw = bp.Draw().Fill(Color.Black);
			foreach (var p in points)
				draw.DrawPoint(p[0], p[1], size);
			draw.Finish();
			window.RenderFrame();
		}

        public void DrawPoints(System.Drawing.PointF[] points, float size)
        {
            DrawPoints(points, size, Color.White);
        }

        public void DrawPoints(System.Drawing.PointF[] points, float size, Color foreground)
        {
            window.SetProgram(bp);
            var draw = bp.Draw().Fill(Color.Black).Color(foreground);
            foreach (var p in points.Where(p => p.X > 0 && p.X < window.Width
                && p.Y > 0 && p.Y < window.Height))
                draw.DrawPoint(p.X, p.Y, size);
            draw.Finish();
            window.RenderFrame();
        }

		public System.Drawing.Size Size
		{
			get { return window.Size; }
		}

		public void Draw(Action<Bitmap> foo)
		{
			foo(bitmap);
			DrawBitmap(bitmap);
		}

		public void DrawPhaseMod(int step, float phase, bool vertical, Color color)
		{
			window.SetProgram(slp);
			slp.SetPhaseMod(step, phase, vertical, color);
			window.RenderFrame();
		}
	}

}
