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
            var display = new BitmapWindow(main.Bounds.Left + main.Bounds.Width / 2 + 50, 50, 640, 480);
            display.Load();
            display.ResizeGraphics();

            var program = new FastPointCloudProgram(15f);
            window.SetProgram(program);


            KinectSensor sensor = KinectSensor.KinectSensors.First();
            Camera cam = new Camera(sensor, ColorImageFormat.RgbResolution640x480Fps30);
            var format = DepthImageFormat.Resolution80x60Fps30;
            DepthCamera depthCam = new DepthCamera(sensor, format);
            SkeletonCamera skeletonCam = new SkeletonCamera(sensor);
            TriplexCamera triplex = new TriplexCamera(depthCam, skeletonCam);
            KinectCalibrator kc = new KinectCalibrator(cc);
            sensor.Start();
            sensor.SkeletonStream.Enable();
            program.SetProjection(pc, null); //kc.GetModelView());

            {
                double[] xs = Range.OfDoubles(0.35, 0.0, 0.05).ToArray();
                PointF[] globalPoints = xs.SelectMany(x => xs.Select(y => new PointF((float)x, (float)y))).ToArray();
                program.SetPositions(globalPoints.Select(gp => new Vector3(gp.X, -gp.Y, 0.0f)).ToArray());
                window.RenderFrame();
                var kinectLocal = cc.Transform(globalPoints.Select(gp => new float[] { gp.X, -gp.Y, 0.0f }).ToArray()).Select(p => new PointF((p.X / 1280f) * 640f, (p.Y / 960f) * 480f)).ToArray();
                var pict = cam.TakePicture(5);
                QuickDraw.Start(pict).Color(Color.Red).DrawPoint(kinectLocal, 2.5f).Finish();
                display.DrawBitmap(pict);
            }
            Console.WriteLine("If the red dots are within the white dots, the calibration is presumably good (enough). Press enter to continue.");
            Console.ReadLine();
            float offset = 1.5f;

            while (true)
            {
                var players = triplex.Trigger(1000);
                if (players == null)
                    continue;
                var joints = players.Where(p => p.Skeleton != null).SelectMany(s => s.Skeleton.Joints);
                var globals = joints.Select(j => kc.ToGlobal(sensor, j.Position, offset)).ToArray();
                
                var points = globals.Select(p => new Vector3(p[0], p[1], p[2])).ToArray();
                if (points.Length > 0)
                    points.ToString();
                program.SetPositions(points);
                window.RenderFrame();
                display.ProcessEvents();
                var pic = cam.TakePicture(0);
                if (globals.Length > 0)
                {
                    //var cpoints = cc.Transform(joints.Select(j => kc.ToGlobal(sensor, j.Position)).ToArray());
                    //QuickDraw.Start(pic).Color(Color.Green).DrawPoint(cpoints, 15).Finish();
                    var tp = joints.Select(j => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(j.Position, ColorImageFormat.RgbResolution640x480Fps30))
                        .Select(cp => new PointF(cp.X, cp.Y)).ToArray();
                    pic.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    QuickDraw.Start(pic).Color(Color.Red).DrawPoint(tp, 5).Finish();
                }
                display.DrawBitmap(pic);
            }
        }
    }
}

