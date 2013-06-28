using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{
	public struct GridSquarePoint
	{
		public Point UL;
		public Point BL;
		public Point BR;
		public Point UR;
	}

	public class GridSquare
	{
		public DenseVector UL;
		public DenseVector BL;
		public DenseVector BR;
		public DenseVector UR;
		public GridSquarePoint ID;

		public GridSquare(GridSquarePoint point, GridSmoothing data)
		{
			UL = data[point.UL];
			BL = data[point.BL];
			BR = data[point.BR];
			UR = data[point.UR];
			this.ID = point;
		}

	}

	public struct PointAndVector
	{
		public Point Point;
		public Vector<double> Vector;
	}

	public class Square
	{
		public DenseVector Center;
		public DenseVector F1;
		public DenseVector F2;
		public double Size;
		public GridSquarePoint ID;

		public IEnumerable<PointAndVector> GetCorners()
		{
			//yield return new PointAndVector() { Vector = Center + Size * F1 / 2 + Size * F2 / 2, Point = ID.UL };
			//yield return new PointAndVector() { Vector = Center - Size * F1 / 2 + Size * F2 / 2, Point = ID.UR };
			//yield return new PointAndVector() { Vector = Center - Size * F1 / 2 - Size * F2 / 2, Point = ID.BL };
			//yield return new PointAndVector() { Vector = Center + Size * F1 / 2 - Size * F2 / 2, Point = ID.BR };
			yield return new PointAndVector() { Vector = Center + Size * F1 / 2 + Size * F2 / 2, Point = ID.UR };
			yield return new PointAndVector() { Vector = Center - Size * F1 / 2 + Size * F2 / 2, Point = ID.BR };
			yield return new PointAndVector() { Vector = Center - Size * F1 / 2 - Size * F2 / 2, Point = ID.BL };
			yield return new PointAndVector() { Vector = Center + Size * F1 / 2 - Size * F2 / 2, Point = ID.UL };
		}
	}

	public class GridSmoothing
	{
		DenseVector[] data;
		Size pattern;
		private GridSmoothing(DenseVector[] data, Size pattern)
		{
			this.data = data;
			this.pattern = pattern;
		}

		public DenseVector this[int x, int y]
		{
			get
			{
				if (x < 0 || x >= pattern.Width)
					throw new IndexOutOfRangeException("X is not in range");
				if (y < 0 || y >= pattern.Height)
					throw new IndexOutOfRangeException("Y is not in range");
				return data[x + y * pattern.Width];
			}
		}

		public DenseVector this[Point p]
		{
			get
			{
				return this[p.X, p.Y];
			}
		}

		public static IEnumerable<GridSquarePoint> Generate(Size pattern)
		{
			for (int x = 0; x < pattern.Width; x++)
				for (int y = 0; y < pattern.Height; y++)
					for (int d = 1; x + d < pattern.Width && y + d < pattern.Height; d++)
						yield return new GridSquarePoint()
						{
							UL = new Point(x, y),
							BL = new Point(x, y + d),
							BR = new Point(x + d, y + d),
							UR = new Point(x + d, y)
						};
		}

		public static IEnumerable<T> Enumerate<T>(params T[] ts)
		{
			foreach (var t in ts)
				yield return t;
		}

		public static DenseVector Center(GridSquare square)
		{
			return (square.BR + square.BL + square.UR + square.UL) / 4.0;
		}

		public static void Axes(DenseVector ul, DenseVector ul2br, out DenseVector p1, out DenseVector p2)
		{
			DenseVector pv;
			pv = new DenseVector(new double[] { -ul2br[1], ul2br[0] });
			p1 = ul + 0.5 * ul2br + 0.5 * pv;
			p2 = ul + 0.5 * ul2br - 0.5 * pv;
		}

		public static void Axes(GridSquare square, out DenseVector f1, out DenseVector f2)
		{
			var ul2br = square.BR - square.UL;
			DenseVector p11, p21;
			Axes(square.UL, ul2br, out p11, out p21);
			var ur2bl = square.BL - square.UR;
			DenseVector p12, p22;
			Axes(square.UR, ur2bl, out p12, out p22);
			var ul = (square.UL + p12) / 2;
			var ur = (square.UR + p21) / 2;
			var bl = (square.BL + p11) / 2;
			var br = (square.BR + p22) / 2;
			f1 = ul - bl;
			f2 = br - bl;
		}

		public static Square MakeSquare(GridSquare gsquare)
		{
			var square = new Square();
			square.Center = Center(gsquare);
			DenseVector f1, f2;
			Axes(gsquare, out f1, out f2);
			square.Size = (f1.Norm(1) + f2.Norm(1)) / 2;
			square.F1 = (DenseVector)f1.Normalize(1);
			square.F2 = (DenseVector)f2.Normalize(1);
			square.ID = gsquare.ID;
			return square;
		}


		public static DenseVector[] Axes(GridSquare square)
		{
			DenseVector f1, f2;
			Axes(square, out f1, out f2);
			return new DenseVector[] { f1, f2 };
		}

		public static PointF V2P(DenseVector vec)
		{
			return new PointF((float)vec[0], (float)vec[1]);
		}

		public static PointF[] Points(DenseVector center, DenseVector f1, DenseVector f2)
		{
			return (new DenseVector[] 
			{
				center + f1 / 2.0 + f2 / 2.0,
				center - f1 / 2.0 + f2 / 2.0,
				center - f1 / 2.0 - f2 / 2.0,
				center + f1 / 2.0 - f2 / 2.0
			}).Select(row => V2P(row)).ToArray();
		}

		public static DenseVector[] MergeAxes(IEnumerable<DenseVector[]> Axes)
		{
			return new DenseVector[] {
				new DenseVector(new double[] {
					Axes.Select(row => row[0][0]).Average(),
					Axes.Select(row => row[0][1]).Average()
				}), new DenseVector(new double[] {
					Axes.Select(row => row[1][0]).Average(),
					Axes.Select(row => row[1][1]).Average()
				})
			};
		}

		public static PointF[] Smooth(PointF[] points, Size pattern)
		{
			var pointVectors = points.Select(row => new DenseVector(new double[] { row.X, row.Y })).ToArray();
			var data = new GridSmoothing(pointVectors, pattern);
			var grid = Generate(pattern);
			var allRects = grid.Select(point => new GridSquare(point, data));

			var squares = allRects.Select(gs => MakeSquare(gs)).ToArray();
			var f1 = squares.Select(s => s.F1).Aggregate((v1, v2) => (v1 + v2)) / squares.Count();
			var f2 = squares.Select(s => s.F2).Aggregate((v1, v2) => (v1 + v2)) / squares.Count();
			foreach (var s in squares)
			{
				s.F1 = f1;
				s.F2 = f2;
			}
			var res = squares
				.SelectMany(row => row.GetCorners())
				.GroupBy(row => new { X = row.Point.X, Y = row.Point.Y })
				.Select(group => new
				{
					Point = new Point(group.Key.X, group.Key.Y),
					Vector = group.Aggregate((Vector<double>)new DenseVector(new double[] { 0, 0 }), (v1, v2) => (v1 + v2.Vector)) / group.Count()
				});
				return res.OrderBy(item => item.Point.Y).ThenBy(item => item.Point.X)
				.Select(item => new PointF((float)item.Vector[0], (float)item.Vector[1])).ToArray();
		}
	}
}
