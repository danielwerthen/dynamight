using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{
    public class ExCamera : IDisposable
    {
        Capture capture;
        private Size size = new Size(1920, 1080);
        public ExCamera()
        {
            capture = new Capture(0);
            capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, size.Width);
            capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, size.Height);
            capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_AUTO_EXPOSURE, 0);

        }

        public Size Size
        {
            get { return size; }
            set
            {
                size = value;
                capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, size.Width);
                capture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, size.Height);
            }
        }

        public Bitmap TakePicture(int takes = 3)
        {
            Bitmap bits = null;
            for (int i = 0; i < takes; i++)
            {
                while (!capture.Grab()) {
                }
                bits = capture.QueryFrame().ToBitmap();
            }
            
            return bits;
        }

        public void Dispose()
        {
            capture.Stop();
            capture.Dispose();
        }
    }
}
