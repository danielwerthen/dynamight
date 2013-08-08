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
using Graphics.Input;
using OpenTK.Input;

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
            float[] data = Utils.DeSerializeObject<float[]>(LightningFastApp.IR2RGBFILE) ?? MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.Identity(4).ToColumnWiseArray();
            MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> D2C = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfColumnMajor(4, 4, data);
            var keyl = new KeyboardListener(display.Keyboard);
            double offsetx = 0, offsety = 0.08, offsetz = 0.12;
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[0], (f) => offsety += f);
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Left, OpenTK.Input.Key.Right, new OpenTK.Input.Key[0], (f) => offsetx -= f);
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ShiftLeft }, (f) => offsetz += f);
            var om = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfColumns(4, 4, new float[][] {
                    new float[] { 1, 0, 0, (float)offsetx },
                    new float[] { 0, 1, 0, (float)offsety },
                    new float[] { 0, 0, 1, (float)offsetz },
                    new float[] { 0, 0, 0, 1 },
                });
            program.SetProjection(pc, null, Make(om)); //kc.GetModelView());

            {
                double[] xs = Range.OfDoubles(0.5, -0.5, 0.05).ToArray();
                PointF[] globalPoints = xs.SelectMany(x => xs.Select(y => new PointF((float)x, (float)y))).ToArray();
                program.SetPositions(globalPoints.Select(gp => new Vector3(gp.X, -gp.Y, 0.0f)).ToArray());
                window.RenderFrame();
                var kinectLocal = cc.Transform(globalPoints.Select(gp => new float[] { gp.X, -gp.Y, 0.0f }).ToArray()).Select(p => new PointF((p.X / 1280f) * 640f, (p.Y / 960f) * 480f)).ToArray();
                var pict = cam.TakePicture(5);
                QuickDraw.Start(pict).Color(Color.Red).DrawPoint(kinectLocal, 2.5f).Finish();
                display.DrawBitmap(pict);
            }
            //Console.WriteLine("If the red dots are within the white dots, the calibration is presumably good (enough). Press enter to continue.");
            //Console.ReadLine();
            float offset = 1.0f;
            program.SetProjection(pc, kc.GetModelView(D2C));

            Vector3[] points = new Vector3[0];
            SkeletonPoint[] joints = new SkeletonPoint[0];
            bool proceed = true;
            while (true)
            {
                om = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfColumns(4, 4, new float[][] {
                    new float[] { 1, 0, 0, (float)offsetx },
                    new float[] { 0, 1, 0, (float)offsety },
                    new float[] { 0, 0, 1, (float)offsetz },
                    new float[] { 0, 0, 0, 1 },
                });
                program.SetProjection(pc, kc.GetModelView(D2C), Make(om));
                if (proceed || points.Length == 0)
                {
                    var players = triplex.Trigger(1000);
                    if (players == null)
                        continue;
                    joints = players.Where(p => p.Skeleton != null).SelectMany(s => s.Skeleton.Joints.Select(j => j.Position)).ToArray();
                    var globals = joints.Select(p => kc.ToGlobal(sensor, p, offset)).ToArray();

                    points = globals.Select(p => new Vector3(p[0], p[1], p[2])).ToArray();
                    points = joints.Select(p => new Vector3(p.X, p.Y, p.Z)).ToArray();
                }
                program.SetPositions(points);
                window.RenderFrame();
                display.ProcessEvents();
                var pic = cam.TakePicture(0);
                if (joints.Length > 0)
                {
                    //var cpoints = cc.Transform(joints.Select(j => kc.ToGlobal(sensor, j.Position)).ToArray());
                    //QuickDraw.Start(pic).Color(Color.Green).DrawPoint(cpoints, 15).Finish();
                    var tp = joints.Select(j => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(j, ColorImageFormat.RgbResolution640x480Fps30))
                        .Select(cp => new PointF(cp.X, cp.Y)).ToArray();
                    pic.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    QuickDraw.Start(pic).Color(Color.Red).DrawPoint(tp, 5).Finish();
                }
                display.DrawBitmap(pic);
            }
        }

        public static OpenTK.Matrix4 Make(MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> mat)
        {
            var rt = mat;
            var res = new OpenTK.Matrix4(
                rt[0, 0], rt[1, 0], rt[2, 0], rt[3, 0],
                rt[0, 1], rt[1, 1], rt[2, 1], rt[3, 1],
                rt[0, 2], rt[1, 2], rt[2, 2], rt[3, 2],
                rt[0, 3], rt[1, 3], rt[2, 3], rt[3, 3]);
            res.Transpose();
            return res;
        }
    }
}

