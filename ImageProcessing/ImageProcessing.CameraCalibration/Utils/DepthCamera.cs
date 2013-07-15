using Graphics;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{
    public class DepthCamera
    {
        KinectSensor sensor;
        public DepthCamera(KinectSensor sensor, DepthImageFormat format)
        {
            this.sensor = sensor;
            sensor.DepthStream.Enable(format);
        }

        public DepthImagePixel[] GetDepth(int wait, out Size? pixelSize)
        {
            pixelSize = null;
            using (var frame = sensor.DepthStream.OpenNextFrame(wait))
            {
                if (frame == null)
                    return null;
                var depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
                
                frame.CopyDepthImagePixelDataTo(depthPixels);
                if (sensor.DepthStream.Format == DepthImageFormat.Resolution640x480Fps30)
                {
                    pixelSize = new Size(640, 480);
                }
                else if (sensor.DepthStream.Format == DepthImageFormat.Resolution320x240Fps30)
                {
                    pixelSize = new Size(320, 240);
                }
                else if (sensor.DepthStream.Format == DepthImageFormat.Resolution80x60Fps30)
                {
                    pixelSize = new Size(80, 60);
                }
                else
                    throw new NotImplementedException();
                
                return depthPixels;
            }
        }

        public DepthImagePoint[] GetDepth(int wait)
        {
            Size? size;
            var data = GetDepth(wait, out size);
            if (data == null)
                return null;
            var pixelSize = size.Value;
            return Range.OfInts(pixelSize.Height).SelectMany(y => Range.OfInts(pixelSize.Width).Select((x) =>
            {
                var b = data[x + y * pixelSize.Width];

                var dip = new DepthImagePoint()
                {
                    X = x,
                    Y = y,
                    Depth = b.Depth
                };
                return dip;
            })).ToArray();
        }

        public DepthImagePoint[] GetForeground(DepthImagePixel[] background, Size pixelSize, short thresh = 30, int wait = 10000)
        {

            Size? pixels;
            var newData = GetDepth(wait, out pixels);
            if (newData == null)
                return null;
            if (pixels.Value.Height != pixelSize.Height || pixels.Value.Width != pixels.Value.Width)
                throw new Exception("Different pixel sizes in GetForeground");
            return Range.OfInts(pixelSize.Height).SelectMany(y => Range.OfInts(pixelSize.Width).Select((x) =>
            {
                var b = background[x + y * pixelSize.Width];
                var f = newData[x + y * pixelSize.Width];
                if (f.Depth - b.Depth <= thresh)
                    return null;

                var dip = new DepthImagePoint()
                {
                    X = x,
                    Y = y,
                    Depth = f.Depth
                };
                return (DepthImagePoint?)dip;
            })).Where(p => p.HasValue).Select(p => p.Value).ToArray();
        }

        public DepthCameraImage TakeImage(int wait = 10000)
        {
            using (var frame = sensor.DepthStream.OpenNextFrame(wait))
            {
                if (frame == null)
                    return null;
                var depthPixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
                frame.CopyDepthImagePixelDataTo(depthPixels);
                int width = 0, height = 0;
                if (sensor.DepthStream.Format == DepthImageFormat.Resolution640x480Fps30)
                {
                    width = 640;
                    height = 480;
                }
                else
                    throw new NotImplementedException();
                ColorImagePoint[] colorpoints = new ColorImagePoint[width * height];
                sensor.CoordinateMapper.MapDepthFrameToColorFrame(sensor.DepthStream.Format
                    , depthPixels, ColorImageFormat.RgbResolution1280x960Fps12, colorpoints);
                var points = Range.OfInts(height).SelectMany( y => Range.OfInts(width).Select((x) =>
                {
                    var pix = depthPixels[x + y * width];
                    
                    var dip = new DepthImagePoint()
                    {
                        X = x,
                        Y = y,
                        Depth = pix.Depth
                    };
                    return dip;
                })).ToArray();
                return new DepthCameraImage(width, height, frame.MaxDepth, frame.MinDepth, points)
                    {
                        ColorPoints = colorpoints,
                        DepthFormat = sensor.DepthStream.Format,
                        SkeletonPoints = points.Select(p => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(sensor.DepthStream.Format, p)).ToArray()
                    };
            }
        }
    }

    public class DepthCameraImage
    {
        public int Width;
        public int Height;
        public int MaxDepth;
        public int MinDepth;
        public DepthImageFormat DepthFormat;
        public DepthCameraImage(int w, int h, int maxd, int mind, DepthImagePoint[] points)
        {
            Width = w;
            Height = h;
            MaxDepth = maxd;
            MinDepth = mind;
            this.points = points;
        }
        private DepthImagePoint[] points;
        public ColorImagePoint[] ColorPoints;
        public SkeletonPoint[] SkeletonPoints;

        public DepthImagePoint this[int x, int y]
        {
            get
            {
                return points[y + x*Height];
            }
        }

        public Bitmap ConvertToBitmap()
        {
            var bitmap = new Bitmap(Width, Height);
            using (var fast = new FastBitmap(bitmap))
            {
                foreach (var dip in points)
                {
                    var depth = dip.Depth;
                    byte intensity = (byte)(depth >= MinDepth && depth <= MaxDepth ? depth : 0);
                    fast[dip.X, dip.Y] = Color.FromArgb(intensity, intensity, intensity);
                }
            }
            return bitmap;
        }

        public DepthImagePoint GetClosest(ColorImagePoint point)
        {
            DepthImagePoint closest = default(DepthImagePoint);
            double dist = double.MaxValue;
            for (var i = 0; i < points.Length; i++)
            {
                var cp = ColorPoints[i];
                double d = Math.Sqrt((cp.X - point.X) * (cp.X - point.X) + (cp.Y - point.Y) * (cp.Y - point.Y));
                if (d == 0)
                    return points[i];
                else if (d < 0)
                    continue;
                else if (d < dist)
                {
                    closest = points[i];
                    dist = d;
                }
            }
            return closest;
        }
    }
}
