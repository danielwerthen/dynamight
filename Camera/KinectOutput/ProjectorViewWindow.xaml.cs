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
    /// Interaction logic for ProjectorViewWindow.xaml
    /// </summary>
    public partial class ProjectorViewWindow : Window
    {
        private DrawingGroup drawingGroup;
        public ProjectorViewWindow()
        {
            InitializeComponent();
            drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            Display.Source = imageSource;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (this.WindowState == System.Windows.WindowState.Normal || this.WindowState == System.Windows.WindowState.Minimized)
                {
                    this.WindowState = System.Windows.WindowState.Maximized;
                    
                }
                else
                    this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        Action<Skeleton[]> render;
        Skeleton[] state;
        public Action<Skeleton[]> GetRenderer()
        {
            return render = (sl) =>
            {
                state = sl;
                using (DrawingContext dc = drawingGroup.Open())
                {
                    double width = this.ActualWidth;
                    double height = this.ActualHeight;
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, width, height));
                    var t = Transform(new double[] { 0, 0, 0 });
                    dc.DrawEllipse(Brushes.White, null, Transform(new double[] {0,0,0}), 10, 10);
                    if (sl == null)
                        return;
                    foreach (var skeleton in sl)
                    {
                        DrawSkeleton(dc, skeleton);
                    }
                }
            };
        }

        private Point Transform(SkeletonPoint p)
        {
            return Transform(new double[] { p.X, p.Y, p.Z });
        }

        private Point Transform(double[] v)
        {
            var tv = Coordinator.GetInverse("projector")(v);
            double width = this.ActualWidth;
            double height = this.ActualHeight;
            double ppm = width / 2.2;
            return new Point(-tv[0] * ppm + width / 2, -tv[1] * ppm + height / 2);
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (render != null)
                render(state);
        }


        private void DrawSkeleton(DrawingContext dc, Skeleton skeleton)
        {
            DrawBone(skeleton, dc, JointType.HandLeft, JointType.WristLeft);
            DrawBone(skeleton, dc, JointType.WristLeft, JointType.ElbowLeft);
            DrawBone(skeleton, dc, JointType.ElbowLeft, JointType.ShoulderLeft);
            DrawBone(skeleton, dc, JointType.HandRight, JointType.WristRight);
            DrawBone(skeleton, dc, JointType.WristRight, JointType.ElbowRight);
            DrawBone(skeleton, dc, JointType.ElbowRight, JointType.ShoulderRight);

            foreach (Joint joint in skeleton.Joints.Where(row => row.JointType == JointType.HandLeft || row.JointType == JointType.HandRight || row.JointType == JointType.WristLeft || row.JointType == JointType.WristRight || row.JointType == JointType.ElbowLeft || row.JointType == JointType.ElbowRight || row.JointType == JointType.ShoulderLeft || row.JointType == JointType.ShoulderRight || row.JointType == JointType.ShoulderCenter))
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = Brushes.White;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = Brushes.Yellow;
                }

                if (drawBrush != null)
                {
                    dc.DrawEllipse(drawBrush, null, Transform(joint.Position), 4, 4);
                }
            }
        }

        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
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
            Pen drawPen = new Pen(Brushes.Yellow, 2);
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = new Pen(Brushes.White, 2);
            }

            drawingContext.DrawLine(drawPen, Transform(joint0.Position), Transform(joint1.Position));
        }
    }
}
