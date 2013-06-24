using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Graphics
{
	public class QuickDraw
	{
		FastBitmap fast;
		int width;
		int height;
		Color color;
		private QuickDraw(Bitmap bitmap)
		{
			fast = new FastBitmap(bitmap);
			width = bitmap.Width;
			height = bitmap.Height;
			fill = new byte[width * height * 4];
		}

		public QuickDraw Color(Color color)
		{
			this.color = color;
			return this;
		}

		byte[] fill;
		public QuickDraw Fill(Color color)
		{
			var temp = this.color;
			this.color = color;
			//for (int x = 0; x < width; x++)
			//	for (int y = 0; y < height; y++)
			//		DrawPoint(x, y);
			fast.Fill(ref fill);
			this.color = temp;
			return this;
		}

		public QuickDraw DrawPoint(PointF p)
		{
			fast[Convert.ToInt32(p.X), Convert.ToInt32(p.Y)] = this.color;
			return this;
		}

		public QuickDraw DrawPoint(Point p)
		{
			fast[p.X, p.Y] = this.color;
			return this;
		}

		public QuickDraw DrawPoint(double x, double y)
		{
			fast[Convert.ToInt32(x), Convert.ToInt32(y)] = this.color;
			return this;
		}

		public QuickDraw DrawPoint(double x, double y, double size)
		{
			foreach (var p in Grid.FromRadius(x, y, size))
				this.DrawPoint(p);
			return this;
		}

		public QuickDraw DrawShape(System.Windows.Media.Geometry shape)
		{
			foreach (var p in GetHits(shape))
				DrawPoint(p);
			return this;
		}

		private IEnumerable<Point> GetHits(System.Windows.Media.Geometry shape)
		{
			for (var x = shape.Bounds.Left; x < shape.Bounds.Right; x++)
				for (var y = shape.Bounds.Top; y < shape.Bounds.Bottom; y++)
				{
					var p = new System.Windows.Point(x,y);
					if (shape.FillContains(p))
						yield return new Point(Convert.ToInt32(x), Convert.ToInt32(y));
				}
		}

		public void Finish()
		{
			fast.Dispose();
		}

		public static QuickDraw Start(Bitmap bitmap)
		{
			return new QuickDraw(bitmap);
		}
	}

	public class Grid
	{
		public static IEnumerable<Point> FromRadius(double x, double y, double size)
		{
			for (int dy = Convert.ToInt32(y - size); dy < Convert.ToInt32(y + size); dy++)
				for (int dx = Convert.ToInt32(x - size); dx < Convert.ToInt32(x + size); dx++)
					if (Math.Sqrt(((x - dx) * (x - dx)) + ((y - dy) * (y - dy))) < size)
						yield return new Point(dx, dy);
			yield break;
		}
	}
}
