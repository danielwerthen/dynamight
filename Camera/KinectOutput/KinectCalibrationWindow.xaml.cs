using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KinectOutput
{
    /// <summary>
    /// Interaction logic for KinectCalibrationWindow.xaml
    /// </summary>
    public partial class KinectCalibrationWindow : Window
    {
        public KinectCalibrationWindow()
        {
            InitializeComponent();
            Display.MouseDown += Display_MouseDown;
        }

        public static CalibrationResult Calibrate(KinectSensor sensor)
        {
            if (!sensor.IsRunning)
                sensor.Start();
            var window = new KinectCalibrationWindow();
            window.color(sensor, window.Display);
            var res = window.ShowDialog();
            window.Cleanup(sensor);
            return new CalibrationResult() { P0 = window.p0, F1 = window.f1, F2 = window.f2, F3 = window.f3 };
        }

        private void Cleanup(KinectSensor sensor)
        {
            sensor.ColorFrameReady -= ColorReady;
            sensor.DepthFrameReady -= DepthReady;
        }

        System.EventHandler<ColorImageFrameReadyEventArgs> ColorReady;
        System.EventHandler<DepthImageFrameReadyEventArgs> DepthReady;
        private void color(KinectSensor sensor, Image image)
        {
            {
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                var colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                var colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                image.Source = colorBitmap;
                ColorReady = new System.EventHandler<ColorImageFrameReadyEventArgs>((o, arg) =>
                {
                    using (ColorImageFrame colorFrame = arg.OpenColorImageFrame())
                    {
                        if (colorFrame != null)
                        {
                            // Copy the pixel data from the image to a temporary array
                            colorFrame.CopyPixelDataTo(colorPixels);
                            // Write the pixel data into our bitmap
                            colorBitmap.WritePixels(
                                new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                                colorPixels,
                                colorBitmap.PixelWidth * colorFrame.BytesPerPixel,
                                0);
                        }
                    }
                });
                sensor.ColorFrameReady += ColorReady;
            }

            {
                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Allocate space to put the depth pixels we'll receive
                var depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
                var colorPixels = new byte[sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                var colorPoints = new ColorImagePoint[640 * 480];
                var colorBitmap = new WriteableBitmap(sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);


                var drawingGroup = new DrawingGroup();
                var imageSource = new DrawingImage(drawingGroup);
                DisplayAxes.Source = imageSource;

                DisplayHelp.Source = colorBitmap;
                DepthReady = (o, arg) =>
                {
                    using (DepthImageFrame frame = arg.OpenDepthImageFrame())
                    {
                        if (frame != null)
                        {
                            frame.CopyDepthImagePixelDataTo(depthPixels);
                            sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depthPixels, ColorImageFormat.RgbResolution640x480Fps30, colorPoints);
                            Array.Clear(colorPixels, 0, colorPixels.Length);
                            for (var i = 0; i < depthPixels.Length; i++)
                            {
                                    var cp = colorPoints[i];
                                    var idx = (cp.X + cp.Y * 640) * 4;

                                    if (idx + 3 < colorPixels.Length)
                                    {
                                        if (depthPixels[i].IsKnownDepth)
                                        {
                                            colorPixels[idx] = 0;
                                            colorPixels[idx + 1] = 0;
                                            colorPixels[idx + 2] = 0;
                                        }
                                        else
                                        {
                                            colorPixels[idx] = 0;
                                            colorPixels[idx + 1] = 0;
                                            colorPixels[idx + 2] = 150;
                                        }
                                    }

                            }
                            // Write the pixel data into our bitmap
                            colorBitmap.WritePixels(
                                new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                                colorPixels,
                                colorBitmap.PixelWidth * sizeof(int),
                                0);

                            if (pRed != null)
                            {
                                var idx = FindDepthIndex(colorPoints, pRed);
                                dRed = FindDepth(sensor, depthPixels, pRed, idx);
                            }
                            if (pBlue != null)
                            {
                                var idx = FindDepthIndex(colorPoints, pBlue);
                                dBlue = FindDepth(sensor, depthPixels, pBlue, idx);
                            }
                            if (pYellow != null)
                            {
                                var idx = FindDepthIndex(colorPoints, pYellow);
                                dYellow = FindDepth(sensor, depthPixels, pYellow, idx);
                            }
                            ColorToggle(Pred, pRed, dRed);
                            ColorToggle(Pblue, pBlue, dBlue);
                            ColorToggle(Pyellow, pYellow, dYellow);
                            IsValid = false;
                            if (dBlue.HasValue && dRed.HasValue && dYellow.HasValue)
                                computeTransform();
                            using (var dc = drawingGroup.Open())
                            {
                                drawAxes(sensor, dc);
                            }
                        }
                    }
                };
                sensor.DepthFrameReady += DepthReady;
            }
        }

        public Vector<double> f1, f2, f3, p0;
        public bool IsValid;
        private void computeTransform()
        {
            Func<SkeletonPoint, Vector<double>> conv = (sp) => new DenseVector(new double[] { sp.X, sp.Y, sp.Z });
            p0 = conv(dBlue.Value);
            var p1 = conv(dRed.Value);
            var p2 = conv(dYellow.Value);


            f2 = (p1 - p0).Normalize(1);
            f1 = (p2 - p0).Normalize(1);
            f1 = (f1 - (f1.DotProduct(f2) * f2)).Normalize(1);
            f3 = new DenseVector(new double[] { f1[1] * f2[2] - f1[2] * f2[1], f1[2] * f2[0] - f1[0] * f2[2], f1[0] * f2[1] - f1[1] * f2[0] });
            f3 = f3.Normalize(1);
            IsValid = true;
        }

        private void drawAxes(KinectSensor sensor, DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 640, 480));
            
            if (dRed.HasValue)
            {
                dc.DrawEllipse(Brushes.Red, null, SkeletonPointToScreen(sensor, dRed.Value), 8, 8);
            }
            if (dBlue.HasValue)
            {
                dc.DrawEllipse(Brushes.Blue, null, SkeletonPointToScreen(sensor, dBlue.Value), 8, 8);
            }
            if (dYellow.HasValue)
            {
                dc.DrawEllipse(Brushes.Yellow, null, SkeletonPointToScreen(sensor, dYellow.Value), 8, 8);
            }
            if (dRed.HasValue && dBlue.HasValue && dYellow.HasValue)
            {
                dc.DrawLine(new Pen(Brushes.Red, 5.5), VectorToScreen(sensor, p0), VectorToScreen(sensor, p0 + f1 / 10));
                dc.DrawLine(new Pen(Brushes.Green, 5.5), VectorToScreen(sensor, p0), VectorToScreen(sensor, p0 + f2 / 10));
                dc.DrawLine(new Pen(Brushes.Blue, 5.5), VectorToScreen(sensor, p0), VectorToScreen(sensor, p0 + f3 / 10));
                drawCheckboard(sensor, dc);
            }
        }

        private void drawCheckboard(KinectSensor sensor, DrawingContext dc)
        {
            bool sw = false;
            Func<Brush> br = () => (sw = !sw) ? Brushes.White : Brushes.Black;
            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    double scale = 10;
                    var po0 = p0 + x * (f1 / scale) + y * (f2 / scale);
                    var po1 = p0 + (x + 1) * (f1 / scale) + y * (f2 / scale);
                    var po2 = p0 + (x + 1) * (f1 / scale) + (y + 1) * (f2 / scale);
                    var po3 = p0 + x * (f1 / scale) + (y + 1) * (f2 / scale);
                    dc.DrawGeometry(br(), null, new PathGeometry(new PathFigure[] { 
                        new PathFigure(VectorToScreen(sensor, po0), new PathSegment[] { new PolyLineSegment(new Point[] {
                            VectorToScreen(sensor, po1),
                            VectorToScreen(sensor, po2),
                            VectorToScreen(sensor, po3),
                        }, false)}, true) 
                    }));
                }
            }
        }

        private static Point VectorToScreen(KinectSensor sensor, Vector<double> point)
        {
            DepthImagePoint depth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(new SkeletonPoint() { X = (float)point[0], Y = (float)point[1], Z = (float)point[2] }, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depth.X, depth.Y);
        }

        private static Point SkeletonPointToScreen(KinectSensor sensor, SkeletonPoint point)
        {
            DepthImagePoint depth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(point, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depth.X, depth.Y);
        }

        private static void ColorToggle(ToggleButton button, Point p, SkeletonPoint? d)
        {
            if (p != null && p != default(Point) && d.HasValue)
                button.Background = Brushes.Green;
            else if (p != null && p != default(Point))
                button.Background = Brushes.Red;
            else
                button.Background = ToggleButton.BackgroundProperty.DefaultMetadata.DefaultValue as Brush;
        }

        private static int FindDepthIndex(ColorImagePoint[] colorPoints, Point point)
        {
            for (var i = 0; i < colorPoints.Length; i++)
            {
                var cpoint = colorPoints[i];
                if (cpoint.X == (int)Math.Round(point.X) && cpoint.Y == (int)Math.Round(point.Y))
                    return i;
            }
            return -1;
        }

        private static SkeletonPoint? FindDepth(KinectSensor sensor, DepthImagePixel[] depthPixels, Point point, int idx)
        {
            if (idx >= 0)
            {
                if (!depthPixels[idx].IsKnownDepth)
                    return null;
                var dp = new DepthImagePoint() { X = (int)point.X, Y = (int)point.Y, Depth = depthPixels[idx].Depth };
                return sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(DepthImageFormat.Resolution640x480Fps30, dp);
            }
            return null;
        }

        Action<Point> PointSetter = null;
        Point pRed, pBlue, pYellow;
        SkeletonPoint? dRed, dBlue, dYellow;
        int setPoint = 0;

        void Display_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(Display);
            if (PointSetter == null)
            {
                if (setPoint == 0)
                    pRed = p;
                if (setPoint == 1)
                    pBlue = p;
                if (setPoint == 2)
                    pYellow = p;
                setPoint++;
                if (setPoint > 2)
                    setPoint = 0;
            }
            else
            {
                PointSetter(p);
            }
            
        }

        private void Pred_Checked(object sender, RoutedEventArgs e)
        {
            var toogle = sender as ToggleButton;
            if (toogle.IsChecked != true)
            {
                PointSetter = null;
                return;
            }
            PointSetter = (p) =>
                {
                    pRed = p;
                    Pred.IsChecked = false;
                    PointSetter = null;
                };
        }

        private void Pblue_Checked(object sender, RoutedEventArgs e)
        {
            var toogle = sender as ToggleButton;
            if (toogle.IsChecked != true)
            {
                PointSetter = null;
                return;
            }
            PointSetter = (p) =>
            {
                pBlue = p;
                Pblue.IsChecked = false;
                PointSetter = null;
            };
        }

        private void Pyellow_Checked(object sender, RoutedEventArgs e)
        {
            var toogle = sender as ToggleButton;
            if (toogle.IsChecked != true)
            {
                PointSetter = null;
                return;
            }
            PointSetter = (p) =>
            {
                pYellow = p;
                Pyellow.IsChecked = false;
                PointSetter = null;
            };
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
