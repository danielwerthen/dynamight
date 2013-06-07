using Microsoft.Kinect;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using System.Drawing;
using Dynamight.ImageProcessing.Util;
using Emgu.CV.Structure;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Runtime.InteropServices;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            for (var y = 0; y < CameraPatternSize.Height; y++)
            {
                for (var x = 0; x < CameraPatternSize.Width; x++)
                {
                    globalPoints[x + y * CameraPatternSize.Width] = new Emgu.CV.Structure.MCvPoint3D32f((float)(x * 0.050), -(float)(y * 0.050), 0);
                }
            }
            coordinates[0] = new Emgu.CV.Structure.MCvPoint3D32f(0, 0, 0);
            coordinates[1] = new Emgu.CV.Structure.MCvPoint3D32f(0.5f, 0, 0);
            coordinates[2] = new Emgu.CV.Structure.MCvPoint3D32f(0, 0.5f, 0);
            coordinates[3] = new Emgu.CV.Structure.MCvPoint3D32f(0, 0, 0.5f);
            int intensity = 0;
            gridColors = globalPoints.Select(row => new Emgu.CV.Structure.Gray((1.0 - (double)(intensity++) / (double)globalPoints.Count() * 0.5) * 255.0)).ToArray();
            InitializeComponent();
        }

        ProjectorCalibration projector;
        Bitmap bitmap;
        readonly object bitmapLock = new object();
        Emgu.CV.Structure.Gray[] gridColors;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            projector = new ProjectorCalibration();
            projector.Show();
            var sensor = KinectSensor.KinectSensors.First();
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
            var colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
            var colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
            Display.Source = colorBitmap;
            sensor.ColorFrameReady += (o, arg) =>
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
                        lock (bitmapLock)
                        {
                            bitmap = BitmapSourceConvert.BitmapFromSource(colorBitmap);
                        }
                        if (detecting && (DateTime.Now - lastRecord).TotalSeconds > 0.5)
                        {
                            record.Add(bitmap);
                            lastRecord = DateTime.Now;
                        }
                    }
                }
            };
            sensor.Start();
            sensor.ElevationAngle = 0;
        }

        static System.Drawing.Size CameraPatternSize = new System.Drawing.Size(7, 4);
        Emgu.CV.Structure.MCvPoint3D32f[] globalPoints = new Emgu.CV.Structure.MCvPoint3D32f[CameraPatternSize.Width * CameraPatternSize.Height];
        Emgu.CV.Structure.MCvPoint3D32f[] coordinates = new Emgu.CV.Structure.MCvPoint3D32f[4];
        
        List<Bitmap> record = new List<Bitmap>();
        DateTime lastRecord = DateTime.Now;

        CalibrationData ProjectorData = new CalibrationData();
        CalibrationData CameraData = new CalibrationData();

        private void TakePic()
        {
            var img = GetBitmap();
            var corners = Detect(CameraPatternSize, img, true);
            if (corners != null)
                CameraData.Add(corners, globalPoints);
        }

        private Image<Emgu.CV.Structure.Gray, byte> GetBitmap()
        {
            Image<Emgu.CV.Structure.Gray, byte> image;
            lock (bitmapLock)
                image = new Image<Emgu.CV.Structure.Gray, byte>(bitmap);
            return image;
        }

        private System.Drawing.Size GetSize()
        {
            System.Drawing.Size size;
            lock (bitmapLock)
                size = new System.Drawing.Size(bitmap.Size.Width, bitmap.Size.Height);
            return size;
        }

        private PointF[] Detect(System.Drawing.Size pattern, Image<Emgu.CV.Structure.Gray, byte> image, bool reverse = false)
        {
            var corners = Emgu.CV.CameraCalibration.FindChessboardCorners(image, pattern, Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
            if (corners == null)
                return null;
            var cc = new PointF[][] { corners };
            image.FindCornerSubPix(cc, new System.Drawing.Size(11, 11), new System.Drawing.Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(30, 0.1));
            if (reverse)
                corners = corners.Reverse().ToArray();
            var withCorners = new Image<Emgu.CV.Structure.Gray, byte>(image.Size);
            withCorners.Draw(new CircleF(corners[0], 3), gridColors[0], 1);
            for (int i = 1; i < corners.Length; i++)
            {
                withCorners.Draw(new LineSegment2DF(corners[i - 1], corners[i]), gridColors[i], 2);
                withCorners.Draw(new CircleF(corners[i], 3), gridColors[i], 1);
            }
            CalibrationDisplay.Source = BitmapSourceConvert.ToBitmapSource(withCorners);
            return corners;

        }

        public struct Corner
        {
            public PointF[] ImageCoord { get; set; }
            public MCvPoint3D32f[] GlobalCoord { get; set; }
        }

        public class CalibrationData
        {
            private List<Corner> corners = new List<Corner>();
            public void Add(PointF[] local, MCvPoint3D32f[] global)
            {
                corners.Add(new Corner() { ImageCoord = local, GlobalCoord = global });
            }

            public void Calibrate(System.Drawing.Size imageSize, out IntrinsicCameraParameters intrinsic, out ExtrinsicCameraParameters[] extrinsic)
            {
                intrinsic = new IntrinsicCameraParameters();
                Emgu.CV.CameraCalibration.CalibrateCamera(
                    corners.Select(row => row.GlobalCoord).ToArray(),
                    corners.Select(row => row.ImageCoord).ToArray(),
                    imageSize,
                    intrinsic,
                    Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_RATIONAL_MODEL,
                    out extrinsic);
            }
        }

        public class CalibratedTransform
        {
            private IntrinsicCameraParameters intrinsic;
            private ExtrinsicCameraParameters extrinsic;

            public Func<double[], double, double[]> LocalToGlobal { get; private set; }
            public Func<double[], double[]> GlobalToLocal { get; set; }

            public CalibratedTransform(IntrinsicCameraParameters intrinsic, ExtrinsicCameraParameters extrinsic)
            {
                this.intrinsic = intrinsic;
                this.extrinsic = extrinsic;
                var ex = extrinsic.ExtrinsicMatrix.Data;
                var rt = DenseMatrix.OfRows(4, 4, new double[][] {
                    new double[] { ex[0,0], ex[0,1], ex[0,2], ex[0,3] },
                    new double[] { ex[1,0], ex[1,1], ex[1,2], ex[1,3] },
                    new double[] { ex[2,0], ex[2,1], ex[2,2], ex[2,3] },
                    new double[] { 0, 0, 0, 1 },
                });
                var ic = intrinsic.IntrinsicMatrix.Data;
                var it = DenseMatrix.OfRows(4, 4, new double[][] {
                    new double[] { ic[0,0], ic[0,1], ic[0,2], 0 },
                    new double[] { ic[1,0], ic[1,1], ic[1,2], 0 },
                    new double[] { ic[2,0], ic[2,1], ic[2,2], 0 },
                    new double[] { 0, 0, 0, 1 },
                });
                var g2l = it.Multiply(rt);
                var l2g = g2l.Inverse();


                Func<double[], double[]> h = (v) => v.Length < 4 ? v.Concat(new double[4 - v.Length]).ToArray() : v;
                //GlobalToLocal = (v) => g2l.Multiply(new DenseVector(h(v))).Take(2).ToArray();
                GlobalToLocal = (v) =>
                {
                    var t = Emgu.CV.CameraCalibration.ProjectPoints(new MCvPoint3D32f[] { new MCvPoint3D32f((float)v[0], (float)v[1], (float)v[2]) }, extrinsic, intrinsic).First();
                    return new double[] { t.X, t.Y };
                };
                LocalToGlobal = (v, z) => 
                {
                    return v;
                };

            }
        }

        public static PointF[] ProjectPoints(
          MCvPoint3D32f[] objectPoints,
          ExtrinsicCameraParameters extrin,
          IntrinsicCameraParameters intrin,
          params Matrix<float>[] mats)
        {
            PointF[] imagePoints = new PointF[objectPoints.Length];

            int matsLength = mats.Length;
            GCHandle handle1 = GCHandle.Alloc(objectPoints, GCHandleType.Pinned);
            GCHandle handle2 = GCHandle.Alloc(imagePoints, GCHandleType.Pinned);
            using (Matrix<float> pointMatrix = new Matrix<float>(objectPoints.Length, 1, 3, handle1.AddrOfPinnedObject(), 3 * sizeof(float)))
            using (Matrix<float> imagePointMatrix = new Matrix<float>(imagePoints.Length, 1, 2, handle2.AddrOfPinnedObject(), 2 * sizeof(float)))
                CvInvoke.cvProjectPoints2(
                      pointMatrix,
                      extrin.RotationVector.Ptr,
                      extrin.TranslationVector.Ptr,
                      intrin.IntrinsicMatrix.Ptr,
                      intrin.DistortionCoeffs.Ptr,
                      imagePointMatrix,
                      matsLength > 0 ? mats[0] : IntPtr.Zero,
                      matsLength > 1 ? mats[1] : IntPtr.Zero,
                      matsLength > 2 ? mats[2] : IntPtr.Zero,
                      matsLength > 3 ? mats[3] : IntPtr.Zero,
                      matsLength > 4 ? mats[4] : IntPtr.Zero,
                      0.0);
            handle1.Free();
            handle2.Free();
            return imagePoints;
        }

        private readonly object cornerLock = new object();

        /*private double Calibrate0000()
        {
            System.Drawing.Size size;
            lock (bitmapLock)
                size = new System.Drawing.Size(bitmap.Size.Width, bitmap.Size.Height);
            Emgu.CV.Structure.MCvPoint3D32f[][] globalList = cornerList.Select(row => globalPoints).ToArray();
            Emgu.CV.IntrinsicCameraParameters intrinParam = new IntrinsicCameraParameters();
            Emgu.CV.ExtrinsicCameraParameters[] extrinParams;
            return Emgu.CV.CameraCalibration.CalibrateCamera(globalList, cornerList.ToArray(), size, intrinParam, Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_RATIONAL_MODEL, out extrinParams);
        }*/

        private void CalibrateButton_Click(object sender, RoutedEventArgs e)
        {
            var size = GetSize();
            IntrinsicCameraParameters intrinsic;
            ExtrinsicCameraParameters[] extrinsic;
            CameraData.Calibrate(size, out intrinsic, out extrinsic);
            var ct = new CalibratedTransform(intrinsic, extrinsic.First());
            var t = ct.GlobalToLocal(new double[] { 1, 1, 0 });
            var t2 = ct.LocalToGlobal(t, 0);

            var projpoints = coordinates.Select(row => ct.GlobalToLocal(new double[] { row.x, row.y, row.z }));
            //var projpoints = Emgu.CV.CameraCalibration.ProjectPoints(coordinates, extrinsic.First(), intrinsic);
            var drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            DrawingDisplay.Source = imageSource;
            Func<double[], System.Windows.Point> transform = (p) =>
            {
                return new System.Windows.Point(p[0], p[1]);
            };
            var points = projpoints.Select(row => transform(row)).ToArray();
            using (var dc = drawingGroup.Open())
            {
                dc.DrawRectangle(System.Windows.Media.Brushes.Transparent, null, new Rect(0, 0, size.Width, size.Height));
                foreach (var p in points)
                {
                    dc.DrawEllipse(System.Windows.Media.Brushes.Red, null, p, 5, 5);
                }
                dc.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.White, 2), points[0], points[1]);
                dc.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.White, 2), points[0], points[2]);
                dc.DrawLine(new System.Windows.Media.Pen(System.Windows.Media.Brushes.White, 2), points[0], points[3]);
            }
        }

        private void FirstButton_Click(object sender, RoutedEventArgs e)
        {
            CameraData = new CalibrationData();
            DrawingDisplay.Source = null;
            CalibrationDisplay.Source = null;
            TakePic();
        }

        private void PixButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingDisplay.Source = null;
            CalibrationDisplay.Source = null;
            TakePic();
        }

        bool detecting = false;

        private void DetectButton_Checked(object sender, RoutedEventArgs e)
        {
            record.Clear();
            detecting = true;
            DetectButton.Content = "Detecting";
        }

        private void DetectButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Mouse.SetCursor(Cursors.Wait);
            detecting = false;
            foreach (var map in record)
            {
                bitmap = map;
                TakePic();
            }
            DetectButton.Content = "Detect";
            Mouse.SetCursor(Cursors.Arrow);
        }

        private void BlackButton_Checked(object sender, RoutedEventArgs e)
        {
            projector.Black(true);
        }

        private void BlackButton_Unchecked(object sender, RoutedEventArgs e)
        {
            projector.Black(false);
        }

        private void ResetProjButton_Click(object sender, RoutedEventArgs e)
        {
            projector.Reset();
        }

        private void StepProjButton_Click(object sender, RoutedEventArgs e)
        {
            projector.Update();
        }

        private void TakeProjPic()
        {
            var img = GetBitmap();
            var corners = Detect(projector.PatternSize, img);
        }

        private void FirstProjButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectorData = new CalibrationData();
            TakeProjPic();
        }

        private void NextProjButton_Click(object sender, RoutedEventArgs e)
        {
            TakeProjPic();
        }

        private void CalibrateProjButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DetectCornersButton_Click(object sender, RoutedEventArgs e)
        {
            var img = GetBitmap();
            for (var y = 5; y < 12; y++)
            {
                for (var x = 5; x < 12; x++)
                {
                    var cs = Detect(new System.Drawing.Size(x, y), img);
                    if (cs != null)
                        return;
                }
            }
        }
    }
}
