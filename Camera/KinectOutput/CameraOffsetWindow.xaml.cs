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
    /// Interaction logic for CameraOffsetWindow.xaml
    /// </summary>
    public partial class CameraOffsetWindow : Window
    {
        public CameraOffsetWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var sensors = KinectSensor.KinectSensors.Where(row => row.Status == KinectStatus.Connected);
            var sensor = sensors.FirstOrDefault();
                DepthImagePixel[] depthPixels;
            if (sensor == null)
                return;
            {
                byte[] colorPixels;
                WriteableBitmap colorBitmap;
                // Turn on the depth stream to receive depth frames
                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                // Allocate space to put the depth pixels we'll receive
                depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];

                // Allocate space to put the color pixels we'll create
                colorPixels = new byte[sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                var colorPixelsRight = new byte[sensor.DepthStream.FramePixelDataLength * sizeof(int)];

                // This is the bitmap we'll display on-screen
                colorBitmap = new WriteableBitmap(sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                var colorBitmapRight = new WriteableBitmap(sensor.DepthStream.FrameWidth, sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                BackImage.Source = colorBitmap;
                BackImageRight.Source = colorBitmapRight;
                var colorPoints = new ColorImagePoint[640 * 480];
                var depthPoints = new DepthImagePoint[640 * 480];

                var ready = new System.EventHandler<DepthImageFrameReadyEventArgs>((o, arg) =>
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

                        sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depthPixels, ColorImageFormat.RgbResolution640x480Fps30, colorPoints);
                        
                        // Convert the depth to RGB
                        int colorPixelIndex = 0;
                        for (int i = 0; i < depthPixels.Length; ++i)
                        {
                            // Get the depth for this pixel
                            short depth = depthPixels[i].Depth;
                            var cp = colorPoints[i];
                            byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                            var idx = (cp.X + cp.Y * 640) * 4;
                            
                            if (idx + 3 < colorPixels.Length)
                            {
                                if (!depthPixels[i].IsKnownDepth)
                                {
                                    colorPixelsRight[idx] = 0;
                                    colorPixelsRight[idx + 1] = 0;
                                    colorPixelsRight[idx + 2] = 150;
                                }
                                else
                                {
                                    colorPixelsRight[idx] = intensity;
                                    colorPixelsRight[idx + 1] = intensity;
                                    colorPixelsRight[idx + 2] = intensity;
                                }
                            }
                            if (!depthPixels[i].IsKnownDepth)
                            {
                                colorPixels[colorPixelIndex++] = 0;
                                colorPixels[colorPixelIndex++] = 0;
                                colorPixels[colorPixelIndex++] = 150;
                            }
                            else
                            {
                                colorPixels[colorPixelIndex++] = intensity;
                                colorPixels[colorPixelIndex++] = intensity;
                                colorPixels[colorPixelIndex++] = intensity;
                            }

                            ++colorPixelIndex;
                        }

                        // Write the pixel data into our bitmap
                        colorBitmap.WritePixels(
                            new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                            colorPixels,
                            colorBitmap.PixelWidth * sizeof(int),
                            0);
                        colorBitmapRight.WritePixels(
                            new Int32Rect(0, 0, colorBitmapRight.PixelWidth, colorBitmapRight.PixelHeight),
                            colorPixelsRight,
                            colorBitmapRight.PixelWidth * sizeof(int),
                            0);
                    }
                });
                sensor.DepthFrameReady += ready;
            }

            {
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                var colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                var colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                var colorBitmapRight = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                ForeImage.Source = colorBitmap;
                ForeImageRight.Source = colorBitmapRight;
                var ready = new System.EventHandler<ColorImageFrameReadyEventArgs>((o, arg) =>
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
                            colorBitmapRight.WritePixels(
                                 new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                                 colorPixels,
                                 colorBitmap.PixelWidth * colorFrame.BytesPerPixel,
                                 0);
                        }
                    }
                });
                sensor.ColorFrameReady += ready;
            }
            sensor.Start();
        }
    }
}
