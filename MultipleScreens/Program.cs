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
		static void Main(string[] args)
		{
			BitmapWindow b1 = new BitmapWindow(50, 50, 500, 500);
			b1.Load();
			b1.ResizeGraphics();
			using (var bitmap = new Bitmap(b1.Width, b1.Height))
			{
				b1.LoadBitmap(bitmap);
				b1.RenderFrame();
				List<double> times = new List<double>();
				for (var i = 0; i < 50; i++)
				{
					DateTime t0 = DateTime.Now;
					QuickDraw.Start(bitmap).Color(Color.White)
						.Fill(Color.Black)
						.DrawShape(new System.Windows.Media.RectangleGeometry(new System.Windows.Rect(i * 10 , 0, 10, bitmap.Height)))
						.Finish();
					times.Add((DateTime.Now - t0).TotalMilliseconds);
					b1.LoadBitmap(bitmap);
					b1.RenderFrame();
				}
				var avg = times.Average();
				Console.ReadLine();
			}
		}

		static Bitmap MakeBitm(int Width, int Height, double max = 1)
		{
			Bitmap bitmap = new Bitmap(Width, Height);
			{
				for (int y = 0; y < Height; y++)
					for (int x = 0; x < Width; x++)
					{
						double ii = (double)x / (double)Width;
						ii = ii * 255 * max;
						byte z = (byte)ii;
						bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(z, z, z));
					}
			}
			return bitmap;
		}
	}

}
