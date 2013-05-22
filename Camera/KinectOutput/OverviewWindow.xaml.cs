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
using System.Windows.Shapes;

namespace KinectOutput
{
    /// <summary>
    /// Interaction logic for OverviewWindow.xaml
    /// </summary>
    public partial class OverviewWindow : Window
    {
        int pwidth;
        int pheight;
        Brush BackgroundBrush = Brushes.DarkGray;
        int gridCount = 5;
        int ppgridLine = 50;
        Pen linePen = new Pen(Brushes.LightGray, 1);

        public OverviewWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = sender as Window;
            pwidth = (int)window.Width;
            pheight = (int)window.Height;
            ppgridLine = pwidth / gridCount;
            var connected = KinectSensor.KinectSensors.Where(row => row.Status == KinectStatus.Connected);
            DrawBackground(Background);
            foreach (var sensor in connected)
            {
                InitSensor(sensor);
            }
        }

        private void InitSensor(KinectSensor sensor)
        {
            var image = new Image();
            var drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            image.Source = imageSource;
            sensor.SkeletonStream.Enable();
            sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            sensor.SkeletonFrameReady += (o, arg) =>
            {
                var skeletons = new Skeleton[0];
                using (var frame = arg.OpenSkeletonFrame())
                {
                    if (frame != null)
                    {
                        skeletons = new Skeleton[frame.SkeletonArrayLength];
                        frame.CopySkeletonDataTo(skeletons);
                    }

                }
                using (DrawingContext dc = drawingGroup.Open())
                {
                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, pwidth, pheight));
                    dc.DrawEllipse(Brushes.Turquoise, null, new Point(pwidth / 2, 50), 8, 8);
                    if (skeletons.Length > 0)
                    {
                        foreach (var skeleton in skeletons)
                        {
                            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                dc.DrawEllipse(Brushes.Turquoise, null, new Point(pwidth / 2 + skeleton.Position.X * ppgridLine, 50 + skeleton.Position.Z * ppgridLine), 8, 8);
                            }
                        }
                    }
                }
            };
            Holder.Children.Add(image);
            sensor.Start();
        }

        private void DrawBackground(Image image)
        {
            var drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            image.Source = imageSource;
            using (DrawingContext dc = drawingGroup.Open())
            {
                dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, pwidth, pheight));
                for (int x = 0; x <= pwidth; x += ppgridLine)
                {
                    dc.DrawLine(linePen, new Point(x, 0), new Point(x, pheight));
                }
                for (int y = 0; y <= pheight; y += ppgridLine)
                {
                    dc.DrawLine(linePen, new Point(0, y), new Point(pwidth, y));
                }
            }
        }

        private void DrawKinect(DrawingContext dc, KinectSensor sensor)
        {

        }
    }
}
