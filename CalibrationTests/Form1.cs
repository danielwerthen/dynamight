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
            Camera camera = new Camera(KinectSensor.KinectSensors.First(), ColorImageFormat.RgbResolution1280x960Fps12);
            Projector proj = new Projector();
            bool res = false;
            Application.Idle += (o, e) =>
            {
                if (res)
                    return;
                Bitmap map;
                res = DualCalibrator.DrawCorners(proj, camera, out map);
                pictureBox1.Image = map;
            };
        }
    }
}
