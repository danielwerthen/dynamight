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
    /// Interaction logic for SkeletonWindow.xaml
    /// </summary>
    public partial class SkeletonWindow : Window
    {

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        public SkeletonWindow()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            var connected = KinectSensor.KinectSensors.Where(row => row.Status == KinectStatus.Connected);
            var sensor1 = connected.FirstOrDefault();

            int angle = 0;
            if (sensor1 != null)
            {
                depth(sensor1, Image);
                skeleton(sensor1, DrawImage);
                sensor1.Start();
                sensor1.ElevationAngle = angle;
            }

        }

        private void skeleton(KinectSensor sensor, Image Image)
        {
            var drawingGroup = new DrawingGroup();
            var imageSource = new DrawingImage(drawingGroup);
            Image.Source = imageSource;

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
                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 640, 480));
                    if (skeletons.Length > 0)
                    {
                        foreach (var skeleton in skeletons)
                        {
                            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                DrawBonesAndJoints(sensor, skeleton, dc);
                                dc.DrawEllipse(Brushes.Turquoise, null, SkeletonPointToScreen(sensor, skeleton.Position), BodyCenterThickness, BodyCenterThickness);
                            }
                            else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                            {
                                dc.DrawEllipse(Brushes.Turquoise, null, SkeletonPointToScreen(sensor, skeleton.Position), BodyCenterThickness, BodyCenterThickness);
                            }
                        }
                    }
                }
            };
        }

        private Point SkeletonPointToScreen(KinectSensor sensor, SkeletonPoint point)
        {
            DepthImagePoint depth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(point, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depth.X, depth.Y);
        }

        private void DrawBonesAndJoints(KinectSensor sensor, Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(sensor, skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(sensor, skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(sensor, skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(sensor, skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(sensor, skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(sensor, skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(sensor, joint.Position), JointThickness, JointThickness);
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
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(sensor, joint0.Position), this.SkeletonPointToScreen(sensor, joint1.Position));
        }

        private void depth(KinectSensor sensor, Image Image)
        {
            DepthImagePixel[] depthPixels;
            byte[] colorPixels;
            WriteableBitmap colorBitmap;
            // Turn on the depth stream to receive depth frames
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

            // Allocate space to put the depth pixels we'll receive
            depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];

            // Allocate space to put the color pixels we'll create
            colorPixels = new byte[sensor.DepthStream.FramePixelDataLength * sizeof(int)];

            // This is the bitmap we'll display on-screen
            colorBitmap = new WriteableBitmap(sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            // Set the image we display to point to the bitmap where we'll put the image data
            Image.Source = colorBitmap;

            sensor.DepthFrameReady += (o, arg) =>
            {
                using (DepthImageFrame depthFrame = arg.OpenDepthImageFrame())
                {
                    if (depthFrame == null)
                        return;
                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(depthPixels);

                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB
                    int colorPixelIndex = 0;
                    for (int i = 0; i < depthPixels.Length; ++i)
                    {
                        // Get the depth for this pixel
                        short depth = depthPixels[i].Depth;

                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                        // Write out blue byte
                        colorPixels[colorPixelIndex++] = intensity;

                        // Write out green byte
                        colorPixels[colorPixelIndex++] = intensity;

                        // Write out red byte                        
                        colorPixels[colorPixelIndex++] = intensity;

                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }
                    // Write the pixel data into our bitmap
                    colorBitmap.WritePixels(
                        new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                        colorPixels,
                        colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            };
        }
    }
}
