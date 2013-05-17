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

namespace CameraDrawing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var connected = KinectSensor.KinectSensors.Where(row => row.Status == KinectStatus.Connected);
            var sensor1 = connected.FirstOrDefault();
            var sensor2 = connected.Skip(1).FirstOrDefault();
            int angle = 15;
            if (sensor1 != null)
            {
                depth(sensor1, ImageLeft);
                sensor1.Start();
                sensor1.ElevationAngle = angle;
            }

            if (sensor2 != null)
            {
                depth(sensor2, ImageRight);
                sensor2.Start();
                sensor2.ElevationAngle = angle;
            }


            // Create the drawing group we'll use for drawing
            var drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            var imageSource = new DrawingImage(drawingGroup);

            // Display the drawing using our image control
            DrawLeft.Source = imageSource;

            using (DrawingContext dc = drawingGroup.Open())
            {

                dc.DrawRectangle(Brushes.White, null, new Rect(0.0, 0.0, 400, 400));
            }
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

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
        }
    }
}
