using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Graphics.Projection;
using Graphics.Textures;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class MovingHeadsApp
    {
        public static void Run(string[] args)
        {
            //var debug = new BitmapWindow(550, 50, 640, 480);
            //debug.Load();
            //debug.ResizeGraphics();
            //debug.DrawBitmap(test);

            //while (true)
            //    debug.ToString();

            var camfile = args.FirstOrDefault() ?? Calibration.KinectDefaultFileName;
            var projfile = args.Skip(1).FirstOrDefault() ?? Calibration.ProjectorDefaultFileName;
            if (!File.Exists(camfile) || !File.Exists(projfile))
            {
                Console.WriteLine("Either calib file could not be found.");
                return;
            }
            var cc = Utils.DeSerializeObject<CalibrationResult>(camfile);
            var pc = Utils.DeSerializeObject<CalibrationResult>(projfile);
            var window = ProgramWindow.OpenOnSecondary();
            KinectSensor sensor = KinectSensor.KinectSensors.First();
            SkeletonCamera skeletonCam = new SkeletonCamera(sensor);
            KinectCalibrator kc = new KinectCalibrator(sensor, cc);
            sensor.Start();
            var parameters = new TransformSmoothParameters
            {     
                Smoothing = 0.5f,     
                Correction = 0.1f,     
                Prediction = 1.1f,     
                JitterRadius = 0.05f,     
                MaxDeviationRadius = 0.05f
            };
            sensor.SkeletonStream.Enable(parameters);
            

            var program = new MovingHeadsProgram();
            window.SetProgram(program);
            program.SetProjection(pc);

            //program.Draw().All((xp, yp) =>
            //{
            //    var x = 0.5 - xp;
            //    var y = 0.5 - yp;
            //    var i = Math.Sqrt(x * x + y * y) * 2.5;
            //    if (i > 1)
            //        i = 1;
            //    i = Math.Pow(1 - i, 3);
            //    byte ii = (byte)(i * 255);
            //    return Color.FromArgb(ii, ii, ii, ii);
            //}).Finish();

            while (true)
            {
                var skeletons = skeletonCam.GetSkeletons(1000);
                if (skeletons != null && skeletons.Length > 0)
                {
                    var r = skeletons.Where(sk => sk.TrackingState == SkeletonTrackingState.Tracked).SelectMany(sk => sk.Joints)
                        .Where(j => j.TrackingState == JointTrackingState.Tracked).Where(j => j.JointType == JointType.HandLeft)
                        .Select(j => j.Position).FirstOrDefault();
                    var p = skeletons.Where(sk => sk.TrackingState != SkeletonTrackingState.NotTracked).Select(sk => sk.Position).FirstOrDefault();
                    if (r != null)
                    {
                        var tran = kc.ToGlobal(r);
                        program.SetPosition(new OpenTK.Vector3(tran[0], tran[1], tran[2]));
                    }
                }
                window.RenderFrame();
                window.ProcessEvents();
            }
        }
    }
}
