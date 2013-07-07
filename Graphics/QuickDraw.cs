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
		Color color = System.Drawing.Color.White;
		Action onFinish;
        int Bpp;
        System.Drawing.Imaging.PixelFormat format;
		private QuickDraw(Bitmap bitmap)
		{
			fast = new FastBitmap(bitmap);
			width = bitmap.Width;
			height = bitmap.Height;
            format = bitmap.PixelFormat;
            Bpp = (Image.GetPixelFormatSize(bitmap.PixelFormat) / 8); ;
		}

		public QuickDraw Color(Color color)
		{
			this.color = color;
			return this;
		}

        public QuickDraw All(Func<double, double, Color> foo, bool normalize = true)
        {
            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    if (normalize)
                        fast[x, y] = foo((double)x / (double)width, (double)y / (double)height);
                    else
                        fast[x, y] = foo((double)x, (double)y);
            return this;
        }

        public QuickDraw Fill(Color color)
		{
			var temp = this.color;
			this.color = color;
            byte[] filler = new byte[width * height * Bpp];
            for (var x = 0; x < width * height; x++)
            {
                if (format == System.Drawing.Imaging.PixelFormat.Format32bppRgb)
                {
                    filler[x * Bpp + 0] = color.B;
                    filler[x * Bpp + 1] = color.G;
                    filler[x * Bpp + 2] = color.R;
                    filler[x * Bpp + 3] = color.A;
								}
								else if (format == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
								{
									filler[x * Bpp + 0] = color.B;
									filler[x * Bpp + 1] = color.G;
									filler[x * Bpp + 2] = color.R;
									filler[x * Bpp + 3] = color.A;
								}
                else
                    throw new Exception("Format " + format.ToString() + " is not supported");
            }
			//for (int x = 0; x < width; x++)
			//	for (int y = 0; y < height; y++)
			//		DrawPoint(x, y);
			fast.Fill(ref filler);
			this.color = temp;
			return this;
		}

		public QuickDraw DrawPoint(PointF p)
		{
			fast[Convert.ToInt32(p.X), Convert.ToInt32(p.Y)] = this.color;
			return this;
		}

        public QuickDraw DrawPoint(IEnumerable<PointF> points, double size = 1)
        {
            if (points == null)
                return null;
            foreach (var p in points)
                this.DrawPoint(p.X, p.Y, size);
            return this;
        }

        public QuickDraw DrawPoints(IEnumerable<float[]> points, double size = 1)
        {
            if (points == null)
                return null;
            foreach (var p in points)
                this.DrawPoint(p[0], p[1], size);
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
			if (onFinish != null)
				onFinish();
		}

		public static QuickDraw Start(Bitmap bitmap, Action onFinish = null)
		{
			return new QuickDraw(bitmap) { onFinish = onFinish };
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
