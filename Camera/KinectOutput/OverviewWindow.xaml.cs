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
        int gridCount = 4;
        int ppgridLine = 50;
        Pen linePen = new Pen(Brushes.LightGray, 1);

        public OverviewWindow()
        {
            InitializeComponent();
        }

        string sensorId1;
        string sensorId2;
        Dictionary<string, string> kinectNames;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Holder_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = sender as Grid;
            pwidth = (int)grid.Width;
            pheight = (int)grid.Height;
            ppgridLine = pwidth / gridCount;
            var connected = KinectSensor.KinectSensors.Where(row => row.Status == KinectStatus.Connected);
            DrawBackground(BackgroundImage);
            kinectNames = new Dictionary<string, string>();
            sensorId2 = connected.Select(row => row.UniqueKinectId).First();
            kinectNames[sensorId2] = "Right";
            sensorId1 = connected.Select(row => row.UniqueKinectId).Skip(1).First();
            kinectNames[sensorId1] = "Left";
            foreach (var sensor in connected)
            {
                InitSensor(sensor);
                var cbutton = new Button();
                cbutton.Content = "Kinect " + kinectNames[sensor.UniqueKinectId];
                cbutton.Click += (o, arg) =>
                {
                    KinectCalibrationWindow.Calibrate(sensor);
                };
                CalibrationButtons.Children.Add(cbutton);
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
                    //dc.DrawEllipse(Brushes.Turquoise, null, new Point(pwidth / 2, ppgridLine / 2), 8, 8);
                    DrawKinect(dc, sensor);
                    ConfidenceView.Inactivate(sensor.UniqueKinectId);
                    if (skeletons.Length > 0)
                    {
                        foreach (var skeleton in skeletons)
                        {
                            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                ConfidenceView.Measure(skeleton, sensor.UniqueKinectId);
                                //dc.DrawEllipse(Brushes.Turquoise, null, new Point(pwidth / 2 + skeleton.Position.X * ppgridLine, 50 + skeleton.Position.Z * ppgridLine), 8, 8);
                                //dc.DrawEllipse(GetTrackedBrush(sensor), null, Transform(sensor, skeleton.Position), 8, 8);
                                DrawSkeleton(dc, skeleton, sensor);
                            }
                        }
                    }
                }
            };
            Holder.Children.Add(image);
            sensor.Start();
            sensor.ElevationAngle = 0;
        }

        #region Transforms

        Dictionary<string, Point> kinectPositions = new Dictionary<string, Point>();
        private Point GetKinectCenter(KinectSensor sensor)
        {
            if (kinectPositions.ContainsKey(sensor.UniqueKinectId))
                return kinectPositions[sensor.UniqueKinectId];
            if (sensor.UniqueKinectId == sensorId1)
                return kinectPositions[sensor.UniqueKinectId] = new Point(ppgridLine, ppgridLine);
            else
                return kinectPositions[sensor.UniqueKinectId] = new Point(ppgridLine + ppgridLine*1.53, ppgridLine);

        }

        private Dictionary<string, Func<double[], double[]>> kinectTransforms = new Dictionary<string, Func<double[], double[]>>();
        private Func<double[], double[]> GetKinectTransform(KinectSensor sensor)
        {
            if (kinectTransforms.ContainsKey(sensor.UniqueKinectId))
                return kinectTransforms[sensor.UniqueKinectId];
            if (sensor.UniqueKinectId != sensorId1)
            {
                var center = GetKinectCenter(sensor);
                var f1 = new double[] { 1 / Math.Sqrt(2), 0, 1 / Math.Sqrt(2) };
                var f3 = new double[] { -1 / Math.Sqrt(2), 0, 1 / Math.Sqrt(2) };
                var f2 = new double[] { 0, 1, 0 };
                return kinectTransforms[sensor.UniqueKinectId] = (v) =>
                {
                    // 1 0 0 cx  x  = x + cx
                    // 0 0 1 cy  y  = z + cy
                    // 0 1 0 0   z  = y
                    // 0 0 0 1   1  = 1
                    return new double[] { f1[0] * v[0] + f2[0] * v[1] + f3[0] * v[2],
                        f1[1] * v[0] + f2[1] * v[1] + f3[1] * v[2],
                        f1[2] * v[0] + f2[2] * v[1] + f3[2] * v[2]
                    };
                };
            }
            else
            {
                var f1 = new double[] { 1 / Math.Sqrt(2), 0, -1 / Math.Sqrt(2) };
                var f3 = new double[] { 1 / Math.Sqrt(2), 0, 1 / Math.Sqrt(2) };
                var f2 = new double[] { 0, 1, 0 };
                return kinectTransforms[sensor.UniqueKinectId] = (v) =>
                {
                    // 1 0 0 cx  x  = x + cx
                    // 0 0 1 cy  y  = z + cy
                    // 0 1 0 0   z  = y
                    // 0 0 0 1   1  = 1
                    return new double[] { f1[0] * v[0] + f2[0] * v[1] + f3[0] * v[2],
                        f1[1] * v[0] + f2[1] * v[1] + f3[1] * v[2],
                        f1[2] * v[0] + f2[2] * v[1] + f3[2] * v[2]
                    };
                };
            }
        }

        private Point Transform(KinectSensor sensor, SkeletonPoint sp)
        {
            var center = GetKinectCenter(sensor);
            var transform = GetKinectTransform(sensor);
            var res = transform(new double[] { sp.X, sp.Y, sp.Z });
            return new Point(res[0] * ppgridLine + center.X, res[2] * ppgridLine + center.Y);
        }

        private Point Transform(KinectSensor sensor, double x, double y, double z)
        {
            var center = GetKinectCenter(sensor);
            var transform = GetKinectTransform(sensor);
            var res = transform(new double[] { x, y, z });
            return new Point(res[0] * ppgridLine + center.X, res[2] * ppgridLine + center.Y);
        }

        #endregion

        private void DrawSkeleton(DrawingContext dc, Skeleton skeleton, KinectSensor sensor)
        {
            DrawBone(sensor, skeleton, dc, JointType.HandLeft, JointType.WristLeft);
            DrawBone(sensor, skeleton, dc, JointType.WristLeft, JointType.ElbowLeft);
            DrawBone(sensor, skeleton, dc, JointType.ElbowLeft, JointType.ShoulderLeft);
            DrawBone(sensor, skeleton, dc, JointType.HandRight, JointType.WristRight);
            DrawBone(sensor, skeleton, dc, JointType.WristRight, JointType.ElbowRight);
            DrawBone(sensor, skeleton, dc, JointType.ElbowRight, JointType.ShoulderRight);

            foreach (Joint joint in skeleton.Joints.Where(row => row.JointType == JointType.HandLeft || row.JointType == JointType.HandRight || row.JointType == JointType.WristLeft || row.JointType == JointType.WristRight || row.JointType == JointType.ElbowLeft || row.JointType == JointType.ElbowRight || row.JointType == JointType.ShoulderLeft || row.JointType == JointType.ShoulderRight || row.JointType == JointType.ShoulderCenter))
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = GetTrackedBrush(sensor);
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = GetInferredBrush(sensor);
                }

                if (drawBrush != null)
                {
                    dc.DrawEllipse(drawBrush, null, Transform(sensor, joint.Position), 4, 4);
                }
            }
        }

        private void DrawBone(KinectSensor sensor, Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = GetInferredPen(sensor);
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = GetTrackedPen(sensor);
            }

            drawingContext.DrawLine(drawPen, Transform(sensor, joint0.Position), Transform(sensor, joint1.Position));
        }

        private Pen GetInferredPen(KinectSensor sensor)
        {
            return new Pen(GetInferredBrush(sensor), 1);
        }

        private Pen GetTrackedPen(KinectSensor sensor)
        {
            return new Pen(Brushes.Blue, 1);
        }

        private Brush GetInferredBrush(KinectSensor sensor)
        {
            return Brushes.Yellow;
        }

        private Brush GetTrackedBrush(KinectSensor sensor)
        {
            if (sensor.UniqueKinectId == sensorId1)
                return Brushes.Red;
            else
                return Brushes.Green;

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
            var size = 0.1;
            var p1 = Transform(sensor, 0, 0, 0);
            var p2 = Transform(sensor, Math.Sin(sensor.DepthStream.NominalHorizontalFieldOfView * 0.0175) * size, 0, 1 * size);
            var p3 = Transform(sensor, Math.Sin(-sensor.DepthStream.NominalHorizontalFieldOfView * 0.0175) * size, 0, 1 * size);
            dc.DrawLine(new Pen(GetTrackedBrush(sensor), 4), p1, p2);
            dc.DrawLine(new Pen(GetTrackedBrush(sensor), 4), p1, p3);
            dc.DrawLine(new Pen(GetTrackedBrush(sensor), 4), p3, p2);
        }
    }
}
