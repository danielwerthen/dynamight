using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalibrationTests
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Projector proj = new Projector();
            //proj.DrawPoints((new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }).Select(row => new PointF(row * 10, row * 10)).ToArray(), 10);
            //proj.DrawPoints(new PointF[] { new PointF(50f, 50f) }, 10);
            Camera camera = new Camera(KinectSensor.KinectSensors.First(), ColorImageFormat.RgbResolution1280x960Fps12);
            bool res = false;
            Application.Idle += (o, e) =>
            {
                if (res)
                    return;
                Bitmap map;
                res = DualCalibrator.DrawCorners(proj, camera, out map);
                //DualCalibrator.DrawNoFull(proj, camera, out map);
                //res = true;
                pictureBox1.Image = map;
            };
        }
    }
}
