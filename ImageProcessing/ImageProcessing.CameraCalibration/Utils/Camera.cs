using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{

    public class Camera
    {
        KinectSensor sensor;
        byte[] imageBuffer;
        ColorImageFormat lastImageFormat;

        public Camera(KinectSensor sensor, Microsoft.Kinect.ColorImageFormat imageFormat)
        {
            this.sensor = sensor;
            sensor.ColorStream.Enable(imageFormat);
            if (!sensor.IsRunning)
                sensor.Start();
            imageBuffer = new byte[sensor.ColorStream.FramePixelDataLength];
            lastImageFormat = imageFormat;
        }

        public Bitmap TakePicture(int wait = 1000)
        {
            Bitmap result;
            using (var frame = sensor.ColorStream.OpenNextFrame(wait))
            {
                if (frame == null)
                    return TakePicture(wait + 1000);
                //detect if the format has changed to resize buffer
                bool haveNewFormat = this.lastImageFormat != frame.Format;
                if (haveNewFormat)
                {
                    imageBuffer = new byte[frame.PixelDataLength];
                    this.lastImageFormat = frame.Format;
                }
                // We must obtain a pointer to the first scanline of the top-down data.
                // This happens to be the start of the buffer.
                unsafe
                {
                    fixed (void* p = imageBuffer)
                    {
                        IntPtr ptr = new IntPtr(p);
                        PixelFormat format = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
                        result = new Bitmap(frame.Width, frame.Height, (4 * frame.Width), format, ptr);
                    }
                }
                frame.CopyPixelDataTo(imageBuffer);
            }
            return result;
        }

        public Size Size
        {
            get { return new Size(sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight); }
        }
    }
}
