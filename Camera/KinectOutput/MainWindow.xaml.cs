using Microsoft.Kinect;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace KinectOutput
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
            var sensors = KinectSensor.KinectSensors.Where(row => row.Status == KinectStatus.Connected);
            var sensor = sensors.FirstOrDefault();
            if (sensor == null)
                return;
            Action clean = null;
            sensor.Start();
            Action setDepth = () =>
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

                            // Convert the depth to RGB
                            int colorPixelIndex = 0;
                            for (int i = 0; i < depthPixels.Length; ++i)
                            {
                                // Get the depth for this pixel
                                short depth = depthPixels[i].Depth;

                                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                                colorPixels[colorPixelIndex++] = intensity;

                                colorPixels[colorPixelIndex++] = intensity;

                                colorPixels[colorPixelIndex++] = intensity;

                                ++colorPixelIndex;
                            }

                            // Write the pixel data into our bitmap
                            colorBitmap.WritePixels(
                                new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                                colorPixels,
                                colorBitmap.PixelWidth * sizeof(int),
                                0);
                        }
                    });
                    sensor.DepthFrameReady += ready;
                    clean = () => sensor.DepthFrameReady -= ready;
                };
            Action setInfra = () =>
                {
                    sensor.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
                    var colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                    var colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Gray16, null);
                    Image.Source = colorBitmap;
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
                            }
                        }
                    });
                    sensor.ColorFrameReady += ready;
                    clean = () => sensor.ColorFrameReady -= ready;
                };
            Action setColor = () =>
            {
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                var colorPixels = new byte[sensor.ColorStream.FramePixelDataLength];
                var colorBitmap = new WriteableBitmap(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                Image.Source = colorBitmap;
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
                        }
                    }
                });
                sensor.ColorFrameReady += ready;
                clean = () => sensor.ColorFrameReady -= ready;
            };
            setColor();
            ColorButton.Click += (o, arg) =>
                {
                    if (clean != null)
                        clean();
                    setColor();
                };
            InfraButton.Click += (o, arg) =>
            {
                if (clean != null)
                    clean();
                setInfra();
            };
            DepthButton.Click += (o, arg) =>
            {
                if (clean != null)
                    clean();
                setDepth();
            };
            SaveButton.Click += (o, arg) =>
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.AddExtension = true;
                sfd.FileName = DateTime.Now.ToString("ddMMyyhhmm");
                sfd.DefaultExt = "png";
                sfd.Filter = "Image files (*.png)|*.png";
                if (sfd.ShowDialog() == true)
                {
                    using (FileStream stream5 = new FileStream(sfd.FileName, FileMode.Create))
                    {
                        PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                        encoder5.Frames.Add(BitmapFrame.Create((WriteableBitmap)Image.Source));
                        encoder5.Save(stream5);
                        stream5.Close();
                    }

                }
            };
        }
    }
}
