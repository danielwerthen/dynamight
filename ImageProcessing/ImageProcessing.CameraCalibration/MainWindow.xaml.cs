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

namespace Dynamight.ImageProcessing.CameraCalibration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            for (var y = 0; y < patternSize.Height; y++)
            {
                for (var x = 0; x < patternSize.Width; x++)
                {
                    globalPoints[x + y * patternSize.Width] = new Emgu.CV.Structure.MCvPoint3D32f((float)(x * 0.050), -(float)(y * 0.050), 0);
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

        static System.Drawing.Size patternSize = new System.Drawing.Size(6,6);
        Emgu.CV.Structure.MCvPoint3D32f[] globalPoints = new Emgu.CV.Structure.MCvPoint3D32f[patternSize.Width * patternSize.Height];
        Emgu.CV.Structure.MCvPoint3D32f[] coordinates = new Emgu.CV.Structure.MCvPoint3D32f[4];
        List<PointF[]> cornerList = new List<PointF[]>();
        List<Bitmap> record = new List<Bitmap>();
        DateTime lastRecord = DateTime.Now;

        private void TakePic()
        {
            Image<Emgu.CV.Structure.Gray, byte> image;
            lock (bitmapLock)
                image = new Image<Emgu.CV.Structure.Gray, byte>(bitmap);
            var corners = Emgu.CV.CameraCalibration.FindChessboardCorners(image, patternSize, Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
            if (corners == null)
            {
                CalibrationDisplay.Source = null;
                return;
            }
            var cc = new PointF[][] { corners };
            image.FindCornerSubPix(cc, new System.Drawing.Size(11, 11), new System.Drawing.Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(30, 0.1));
            var withCorners = new Image<Emgu.CV.Structure.Gray, byte>(image.Size);
            //Emgu.CV.CameraCalibration.DrawChessboardCorners(withCorners, patternSize, corners);
            withCorners.Draw(new CircleF(corners[0], 3), gridColors[0], 1);
            for (int i = 1; i < corners.Length; i++)
            {
                withCorners.Draw(new LineSegment2DF(corners[i - 1], corners[i]), gridColors[i], 2);
                withCorners.Draw(new CircleF(corners[i], 3), gridColors[i], 1);
            }
            cornerList.Add(corners);
            CalibrationDisplay.Source = BitmapSourceConvert.ToBitmapSource(withCorners);
        }

        private readonly object cornerLock = new object();

        private double Calibrate()
        {
            System.Drawing.Size size;
            lock (bitmapLock)
                size = new System.Drawing.Size(bitmap.Size.Width, bitmap.Size.Height);
            Emgu.CV.Structure.MCvPoint3D32f[][] globalList = cornerList.Select(row => globalPoints).ToArray();
            Emgu.CV.IntrinsicCameraParameters intrinParam = new IntrinsicCameraParameters();
            Emgu.CV.ExtrinsicCameraParameters[] extrinParams;
            return Emgu.CV.CameraCalibration.CalibrateCamera(globalList, cornerList.ToArray(), size, intrinParam, Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_RATIONAL_MODEL, out extrinParams);
        }


        private void CalibrateButton_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Size size;
            lock (bitmapLock)
                size = new System.Drawing.Size(bitmap.Size.Width, bitmap.Size.Height);
            Emgu.CV.Structure.MCvPoint3D32f[][] globalList = cornerList.Select(row => globalPoints).ToArray();
            Emgu.CV.IntrinsicCameraParameters intrinParam = new IntrinsicCameraParameters();
            Emgu.CV.ExtrinsicCameraParameters[] extrinParams;
            var err = Emgu.CV.CameraCalibration.CalibrateCamera(globalList, cornerList.ToArray(), size, intrinParam, Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_RATIONAL_MODEL, out extrinParams);
            var projpoints = Emgu.CV.CameraCalibration.ProjectPoints(coordinates, extrinParams.First(), intrinParam);

            var drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            DrawingDisplay.Source = imageSource;
            Func<PointF, System.Windows.Point> transform = (p) =>
            {
                //return new System.Windows.Point((p.X / size.Width) * DrawingDisplay.ActualWidth, (p.X / size.Height) * DrawingDisplay.ActualHeight);
                return new System.Windows.Point(p.X, p.Y);
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
            cornerList.Clear();
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
    }
}
