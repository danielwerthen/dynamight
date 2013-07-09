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
                sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30
                    , depthPixels, ColorImageFormat.RgbResolution1280x960Fps12, colorpoints);
                var points = Range.OfInts(width).SelectMany( x => Range.OfInts(height).Select((y) =>
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
                        DepthFormat = sensor.DepthStream.Format 
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
