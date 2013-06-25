using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Microsoft.Kinect;
using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CalibrationTests
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            double[] xs = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            var t1 = xs.Select(row => Math.Sign(Math.Sin((row / 20) * Math.PI * 2))).ToArray();
            var main = DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var window = new BitmapWindow(main.Bounds.Left + main.Width / 2 + 50, 50, 640, 480);
            window.Load();
            window.ResizeGraphics();
            Projector proj = new Projector();
            Camera camera = new Camera(KinectSensor.KinectSensors.First(row => row.Status == KinectStatus.Connected), ColorImageFormat.RgbResolution1280x960Fps12);
            DualCalibrator.DebugWindow = window;
            DualCalibrator.Test(proj, camera, window);
            //proj.Renderer.RenderBitmap(bitm);
            //BitmapWindow window = BitmapWindow.Make();
            
            //window.RenderFrame();
            //Projector proj = new Projector();
            //var main = DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            //var window = new BitmapWindow(main.Bounds.Left + main.Width / 2 + 50, 50, 640, 480);
            //window.Load();
            //window.ResizeGraphics();
            //DualCalibrator.DebugWindow = window;
            ////proj.DrawPoints((new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }).Select(row => new PointF(row * 10, row * 10)).ToArray(), 10);
            ////proj.DrawPoints(new PointF[] { new PointF(50f, 50f) }, 10);
            //Camera camera = new Camera(KinectSensor.KinectSensors.First(row => row.Status == KinectStatus.Connected), ColorImageFormat.RgbResolution640x480Fps30);
            //bool res = false;
            //Application.Idle += (o, e) =>
            //{
            //    if (res)
            //        return;
            //    Bitmap map;
            //    res = DualCalibrator.DrawCorners(proj, camera, out map);
            //    //DualCalibrator.DrawNoFull(proj, camera, out map);
            //    //res = true;
            //    pictureBox1.Image = map;
            //};
        }
    }
}
