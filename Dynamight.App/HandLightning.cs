using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Graphics.Projection;
using Microsoft.Kinect;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class HandLightning
    {
        public static void Run(string[] args)
        {
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

            var program = new PointCloudProgram(15f);
            window.SetProgram(program);
            program.Draw().All((xp, yp) => {
                var x = 0.5 - xp;
                var y = 0.5 - yp;
                var i = Math.Sqrt(x * x + y * y) *2.5;
                if ( i > 1)
                    i = 1;
                i = Math.Pow(1 - i, 3);
                byte ii = (byte)(i * 255);
                return Color.FromArgb(ii, 255, 255, 255);
            }).Finish();
            program.SetProjection(pc);

            KinectSensor sensor = KinectSensor.KinectSensors.First();
            DepthCamera cam = new DepthCamera(sensor, DepthImageFormat.Resolution80x60Fps30);
            KinectCalibrator kc = new KinectCalibrator(sensor, cc);
            sensor.Start();

            //program.SetPositions(new Vector3[] { new Vector3(0.0f, -0.1f, 0) });
            //window.RenderFrame();

            while (true)
            {
                var test = cam.GetDepth(10000);
                var sp = test.Select(f => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(DepthImageFormat.Resolution80x60Fps30, f));
                var globals = kc.ToGlobal(sp).Where(g => g[2] > -0.5f).Select(v => new Vector3(v[0], v[1], v[2])).ToArray();
                program.SetPositions(globals);
                window.RenderFrame();
                //{
                //    var projected = pc.Transform(globals.Where(g => g[2] > -0.0f).ToArray());
                //    projector.DrawPoints(projected,2, Color.Gray);
                //}
            }
        }
    }
}
