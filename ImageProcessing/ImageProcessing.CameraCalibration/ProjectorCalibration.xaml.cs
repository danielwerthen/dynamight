using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    /// <summary>
    /// Interaction logic for ProjectorCalibration.xaml
    /// </summary>
    public partial class ProjectorCalibration : Window
    {
        public ProjectorCalibration()
        {
            InitializeComponent();
            GetScale = () => new Size(this.scale.Width + ((Size)this.scaleRange).Width, this.scale.Height + ((Size)this.scaleRange).Height);
            GetStart = () => new Point(this.start.X + ((Point)this.startRange).X, this.start.Y + ((Point)this.startRange).Y);
            GetRotY = () => this.RotateY + this.rotYRange;
            GetRotZ = () => this.RotateZ + this.rotZRange;
        }

        private DrawingGroup drawingGroup;
        private void Display_Loaded(object sender, RoutedEventArgs e)
        {
            drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            Display.Source = imageSource;
            Redraw();
        }

        Size scale = new Size(20,20);
        Point start = new Point(0,0);
        double RotateY = 0;
        double RotateZ = 0;
        readonly Size scaleInit = new Size(20, 20);
        readonly Point startInit = new Point(0, 0);
        readonly double RotateYInit = 0;
        readonly double RotateZInit = 0;
        static readonly int steps = 25;
        Range<Size> scaleRange = new Range<Size>()
        {
            Steps = steps,
            x0 = new Size(0, 0),
            x1 = new Size(20, 20),
            Converter = (r) =>
                {
                    return new Size(r.x0.Width + (r.x1.Width - r.x0.Width) * r.Percent, r.x0.Height + (r.x1.Height - r.x0.Height) * r.Percent);
                }
        };

        Range<Point> startRange = new Range<Point>()
        {
            Steps = steps,
            x0 = new Point(0, 0),
            x1 = new Point(20, 20),
            Converter = (r) =>
            {
                return new Point(r.x0.X + (r.x1.X - r.x0.X) * r.Percent, r.x0.Y + (r.x1.Y - r.x0.Y) * r.Percent);
            }
        };

        Range<double> rotYRange = new Range<double>() 
        {
            Steps = steps,
            x0 = 0,
            x1 = 0.03,
            Converter = (r) =>
            {
                return r.x0 + (r.x1 - r.x0) * r.Percent;
            }
        };

        Range<double> rotZRange = new Range<double>()
        {
            Steps = steps,
            x0 = 0,
            x1 = 0,
            Converter = (r) =>
            {
                return r.x0 + (r.x1 - r.x0) * r.Percent;
            }
        };

        Func<Size> GetScale;
        Func<Point> GetStart;
        Func<double> GetRotY, GetRotZ;


        public class Range<T>
        {
            public T x0 { get; set; }
            public T x1 { get; set; }
            public int Steps { get; set; }

            public int Step { get; set; }

            public Func<Range<T>, T> Converter { get; set; }

            public double Percent
            {
                get { return (double)Step / (double)Steps; }
            }

            public static Range<T> operator ++(Range<T> r)
            {
                r.Step++;
                if (r.Step > r.Steps)
                    r.Step = 0;
                return r;
            }
            public static Range<T> operator --(Range<T> r)
            {
                r.Step++;
                if (r.Step < 0)
                    r.Step = r.Steps;
                return r;
            }

            public static implicit operator T(Range<T> r)
            {
                return r.Converter(r);
            }
        }

        public System.Drawing.Size PatternSize { get; set; }

        private void Redraw()
        {
            this.PatternSize = new System.Drawing.Size(6, 6);
            var corners = GetCheckerboard(PatternSize.Width + 1, PatternSize.Height + 1);
            var tc = ScreenCorners(corners);
            Draw(tc);
        }

        public void Update()
        {
            scaleRange++;
            startRange++;
            rotYRange++;
            rotZRange++;
            Redraw();
        }
        public void Reset()
        {
            start = new Point(startInit.X, startInit.Y);
            scale = new Size(scaleInit.Width, scaleInit.Height);
            RotateY = RotateYInit;
            RotateZ = RotateZInit;
            scaleRange.Step = 0;
            startRange.Step = 0;
            rotYRange.Step = 0;
            rotZRange.Step = 0;
            Redraw();
        }

        public void Black(bool on)
        {
            var br = on ? Brushes.Black : Brushes.Transparent;
            using (var dc = drawingGroup.Open())
            {
                dc.DrawRectangle(br, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            }
        }

        public struct Corner
        {
            public Point p0 { get; set; }
            public Point p1 { get; set; }
            public Point p2 { get; set; }
            public Point p3 { get; set; }
            public bool inner { get; set; }
        }

        private Corner[] GetCheckerboard(int xc, int yc)
        {
            var list = new Corner[xc * yc];
            for (var yi = 0; yi < yc; yi++)
            {
                for (var xi = 0; xi < xc; xi++)
                {
                    double x = (double)xi - (double)xc / 2.0;
                    double y = (double)yi - (double)yc / 2.0;
                    list[(xc - xi - 1) + yi * xc] = new Corner() { p0 = new Point(x,y), p1 = new Point(x + 1, y), p2 = new Point(x + 1, y + 1), p3 = new Point(x, y + 1), inner = (x == 0 || y == 0) };
                }
            }
            return list;
        }

        public System.Drawing.PointF[] CurrentCorners { get; set; }

        private Corner[] ScreenCorners(Corner[] corners)
        {
            var scale = GetScale();
            var start = GetStart();
            var RotateY = GetRotY();
            var RotateZ = GetRotZ();
            DenseMatrix tranScale = DenseMatrix.OfColumns(4, 4, new double[][] { new double[] { scale.Width, 0,0,0 },
                new double[] { 0, scale.Height, 0,0 }, new double[] { 0,0,1,0 }, new double[] { this.ActualWidth / 2 + start.X, this.ActualHeight / 2 + start.Y, 0, 1 } });

            double theta = RotateY;
            DenseMatrix rotY = DenseMatrix.OfColumns(4, 4, new double[][] { new double[] { Math.Cos(theta), 0,0, Math.Sin(theta)*(-1.0) },
                new double[] { 0, 1.0, 0,0 }, new double[] { Math.Sin(theta),0,Math.Cos(theta),0 }, new double[] { 0, 0, 0, 1 } });
            theta = RotateZ;
            DenseMatrix rotZ = DenseMatrix.OfColumns(4, 4, new double[][] { new double[] { Math.Cos(theta), Math.Sin(theta),0, 0,},
                new double[] { Math.Sin(theta)*(-1.0), Math.Cos(theta), 0,0 }, new double[] { 0,0,1,0 }, new double[] { 0, 0, 0, 1.0 } });

            var matrix = tranScale.Multiply(rotY.Multiply(rotZ));
            Func<Point, Point> tp = (p) =>
            {
                var v = matrix.Multiply(new DenseVector(new double[] { p.X, p.Y, 0, 1 }));
                return new Point(v[0], v[1]);
            };
            Func<Corner, Corner> t = (c) =>
            {
                return new Corner()
                {
                    p0 = tp(c.p0),
                    p1 = tp(c.p1),
                    p2 = tp(c.p2),
                    p3 = tp(c.p3),
                    inner = c.inner
                };
            };
            var tc = corners.Select((c) =>
                {
                    return t(c);
                });
            CurrentCorners = tc.Where(row => row.inner).Select(row => new System.Drawing.PointF((float)row.p0.X, (float)row.p0.Y)).ToArray();
            return tc.ToArray();
        }

        private void Draw(Corner[] corners)
        {
            if (Display.Source == null)
                return;
            Func<Brush> checkerBrush;
            Func<Brush> black = null;
            Func<Brush> white = null;
            black = () =>
            {
                checkerBrush = white;
                return Brushes.Black;
            };
            white = () =>
            {
                checkerBrush = black;
                return Brushes.White;
            };
            checkerBrush = black;
            using (var dc = drawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0,0, this.ActualWidth, this.ActualHeight));
                int idx = 0;
                foreach (var c in corners)
                {
                    var value = (1 - ((double)idx++ / (double)corners.Length)) * 255.0;
                    dc.DrawGeometry(checkerBrush(), null, new PathGeometry(new PathFigure[] {
                new PathFigure(c.p0, new PathSegment[] {
                    new LineSegment(c.p1, false),
                    new LineSegment(c.p2, false),
                    new LineSegment(c.p3, false),
                }, true)
            }));
                }
            }
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (this.WindowState == System.Windows.WindowState.Normal)
                {
                    this.WindowState = System.Windows.WindowState.Maximized;
                    this.WindowStyle = System.Windows.WindowStyle.None;
                    Hide();
                    Show();
                }
                else
                {
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                }
            }
            else if (e.Key == Key.OemPlus)
            {
                scale.Width *= 1.05;
                scale.Height *= 1.05;
            }
            else if (e.Key == Key.OemMinus)
            {
                scale.Width *= 0.95;
                scale.Height *= 0.95;
            }
            else if (e.Key == Key.Down)
            {
                start.Y += 1;
            }
            else if (e.Key == Key.Up)
            {
                start.Y -= 1;
            }
            else if (e.Key == Key.Right)
            {
                start.X += 1;
            }
            else if (e.Key == Key.Left)
            {
                start.X -= 1;
            }
            else if (e.Key == Key.A)
            {
                RotateZ -= 0.01;
            }
            else if (e.Key == Key.D)
            {
                RotateZ += 0.01;
            }
            else if (e.Key == Key.W)
            {
                RotateY += 0.01;
            }
            else if (e.Key == Key.S)
            {
                RotateY -= 0.01;
            }
            else if (e.Key == Key.U)
            {
                this.Update();
            }
            else if (e.Key == Key.R)
            {
                Reset();
            }
            Redraw();
        }
    }
}
