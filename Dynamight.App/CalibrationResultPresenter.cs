using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Graphics.Projection;
using Microsoft.Kinect;
using OpenTK;
using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamight.Processing;

namespace Dynamight.App
{
    public class CalibrationResultPresenter
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
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var display = new BitmapWindow(main.Bounds.Left + 50, 50, 1280, 960);
            display.Load();
            display.ResizeGraphics();

            var program = new FastPointCloudProgram(15f);
            window.SetProgram(program);
            program.Draw().All((xp, yp) =>
            {
                var x = 0.5 - xp;
                var y = 0.5 - yp;
                var i = Math.Sqrt(x * x + y * y) * 2.5;
                if (i > 1)
                    i = 1;
                i = Math.Pow(1 - i, 3);
                byte ii = (byte)(i * 255);
                return Color.FromArgb(ii, 255, 255, 255);
            }).Finish();

            KinectSensor sensor = KinectSensor.KinectSensors.First();
            Camera cam = new Camera(sensor, ColorImageFormat.RgbResolution1280x960Fps12);
            var format = DepthImageFormat.Resolution80x60Fps30;
            DepthCamera depthCam = new DepthCamera(sensor, format);
            SkeletonCamera skeletonCam = new SkeletonCamera(sensor);
            TriplexCamera triplex = new TriplexCamera(depthCam, skeletonCam);
            KinectCalibrator kc = new KinectCalibrator(cc);
            sensor.Start();
            sensor.SkeletonStream.Enable();
            program.SetProjection(pc, null); //kc.GetModelView());



            while (true)
            {
                var players = triplex.Trigger(1000);
                if (players == null)
                    continue;
                var joints = players.Where(p => p.Skeleton != null).SelectMany(s => s.Skeleton.Joints);
                var globals = joints.Select(j => kc.ToGlobal(sensor, j.Position)).ToArray();
                var points = globals.Select(p => new Vector3(p[0], p[1], p[2])).ToArray();
                if (points.Length > 0)
                    points.ToString();
                program.SetPositions(points);
                window.RenderFrame();
                window.ProcessEvents();
                var pic = cam.TakePicture(0);
                if (globals.Length > 0)
                {
                    var cpoints = cc.Transform(joints.Select(j => kc.ToGlobal(sensor, j.Position)).ToArray());
                    QuickDraw.Start(pic).Color(Color.Green).DrawPoint(cpoints, 15).Finish();
                    var tp = joints.Select(j => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(j.Position, ColorImageFormat.RgbResolution1280x960Fps12))
                        .Select(cp => new PointF(cp.X, cp.Y)).ToArray();
                    pic.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    QuickDraw.Start(pic).Color(Color.Red).DrawPoint(tp, 5).Finish();
                }
                display.DrawBitmap(pic);
            }
        }
    }
}

