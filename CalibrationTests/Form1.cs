using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Microsoft.Kinect;
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
            BitmapWindow window = BitmapWindow.Make();
            Projector proj = new Projector();
            Camera camera = new Camera(KinectSensor.KinectSensors.First(row => row.Status == KinectStatus.Connected), ColorImageFormat.RgbResolution640x480Fps30);

            window.RenderFrame();
            proj.DrawBackground(System.Windows.Media.Colors.Red);
            Thread.Sleep(1000);

            var pic = camera.TakePicture();
            window.LoadBitmap(pic);
            window.RenderFrame();
            proj.DrawBackground(System.Windows.Media.Colors.Blue);
            pic = camera.TakePicture();
            pic = camera.TakePicture();
            pic = camera.TakePicture();
            pic = camera.TakePicture();
            pic = camera.TakePicture();
            pic = camera.TakePicture();
            pic = camera.TakePicture();
            pic = camera.TakePicture();
            pic = camera.TakePicture();
            window.LoadBitmap(pic);
            window.RenderFrame();
            //proj.Renderer.RenderBitmap(bitm);
            //BitmapWindow window = BitmapWindow.Make();
            
            //window.RenderFrame();
            //Projector proj = new Projector();
            ////proj.DrawPoints((new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }).Select(row => new PointF(row * 10, row * 10)).ToArray(), 10);
            ////proj.DrawPoints(new PointF[] { new PointF(50f, 50f) }, 10);
            //Camera camera = new Camera(KinectSensor.KinectSensors.First(row => row.Status == KinectStatus.Connected), ColorImageFormat.RgbResolution640x480Fps30);
            //bool res = false;
            //Application.Idle += (o, e) =>
            //{
            //    if (res)
            //        return;
            //    Bitmap map;
            //    res = DualCalibrator.DrawCorners(proj, camera, window, out map);
            //    //DualCalibrator.DrawNoFull(proj, camera, out map);
            //    //res = true;
            //    pictureBox1.Image = map;
            //};
        }
    }
}
