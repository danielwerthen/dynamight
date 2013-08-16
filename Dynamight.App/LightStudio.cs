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
    public class LightningStudio
    {

        public static void Run(string[] args)
        {

            var projfile = args.Skip(1).FirstOrDefault() ?? Calibration.ProjectorDefaultFileName;
            if (!File.Exists(projfile))
            {
                Console.WriteLine("Either calib file could not be found.");
                return;
            }

            var pc = Utils.DeSerializeObject<CalibrationResult>(projfile);

            var window = ProgramWindow.OpenOnSecondary();

            var program = new LightStudioProgram(0.08f);
            window.SetProgram(program);

            var overviewWindow = new ProgramWindow(750, 50, 1280, 960);
            overviewWindow.Load();
            overviewWindow.ResizeGraphics();
            OverviewProgram overview = new OverviewProgram(program);
            overviewWindow.SetProgram(overview);
            
            var format = DepthImageFormat.Resolution80x60Fps30;
            var inputs = KinectSensor.KinectSensors.Where(k => k.Status == KinectStatus.Connected).Select(k =>
            {
                k.Start();
                return new
                {
                    sensor = k,
                    depth = new DepthCamera(k, format),
                    skeleton = new SkeletonCamera(k),
                    calibrator = new KinectCalibrator(Utils.DeSerializeObject<CalibrationResult>(k.UniqueKinectId + ".xml"))
                };
            });
            float[] data = Utils.DeSerializeObject<float[]>(LightningFastApp.IR2RGBFILE) ?? MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.Identity(4).ToColumnWiseArray();
            MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> D2C = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfColumnMajor(4, 4, data);
            
            var keyl = new KeyboardListener(window.Keyboard);
            SkeletonPoint[] skeletons = null;
            // Action H hide background:
            bool hideBackground = false;
            float zCutoff = 3.4f;
            float xadj = 0, yadj = 0.0f, zadj = 0;
            float incr = 0.01f;
            bool adjust = true;
            program.SetProjection(pc);
            Action<float> xfoo = (f) =>
            {
                if (!adjust)
                    return;
                xadj += f;
                program.SetProjection(pc);
            };
            Action<float> yfoo = (f) =>
            {
                if (!adjust)
                    return;
                yadj += f;
                program.SetProjection(pc);
            };
            Action<float> zfoo = (f) =>
            {
                if (!adjust)
                    return;
                zadj += f;
                program.SetProjection(pc);
            };
            keyl.AddAction(() => adjust = !adjust, Key.A);
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, null, (i) => yfoo(i * incr));
            keyl.AddBinaryAction(1, -1, Key.Left, Key.Right, null, (i) => xfoo(i * incr));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ShiftLeft }, (i) => zfoo(i * incr));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ControlLeft }, (i) => yfoo(i * incr * 3));
            keyl.AddBinaryAction(1, -1, Key.Left, Key.Right, new Key[] { Key.ControlLeft }, (i) => xfoo(i * incr * 3));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ShiftLeft, Key.ControlLeft }, (i) => zfoo(i * incr * 3));

            keyl.AddAction(() =>
            {
                hideBackground = !hideBackground;
                if (skeletons != null && skeletons.Length > 0)
                {
                    zCutoff = skeletons.Min(sp => sp.Z);
                }
                else
                    zCutoff = 0;
            }, OpenTK.Input.Key.H);
            while (true)
            {
                var points = inputs.Select(inp => new
                {
                    Calibrator = inp.calibrator,
                    Skeletons = inp.depth.Get(100).Where(p => p.HasValue && p.Value.Index > 0)
                        .Select(p => inp.sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, p.Value.Point))
                        .ToArray()
                });
                
                program.SetPositions(points.Select(v => v.Skeletons.Where(sp => !hideBackground || sp.Z < zCutoff).Select(sp => new Vector3(sp.X, sp.Y, sp.Z)).ToArray()).ToArray(), points.Select(v =>
                    v.Calibrator.GetModelView(D2C) * OpenTK.Matrix4.CreateTranslation(xadj, yadj, zadj)).ToArray());

                overview.SetPointCloud(0, points.SelectMany(p =>
                    {
                        return p.Skeletons.Select(sp =>
                        {
                            var gp = p.Calibrator.ToGlobal(sp);
                            return new DynamicVertex(new Vector3(gp[0], gp[1], gp[2]));
                        });
                    }).ToArray());

                window.RenderFrame();
                overviewWindow.RenderFrame();
                window.ProcessEvents();
                overviewWindow.ProcessEvents();
            }
        }

        public static void RunOld(string[] args)
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

            var program = new LightStudioProgram(0.08f);
            window.SetProgram(program);

            var overviewWindow = new ProgramWindow(750, 50, 1280, 960);
            overviewWindow.Load();
            overviewWindow.ResizeGraphics();
            OverviewProgram overview = new OverviewProgram(program);
            overviewWindow.SetProgram(overview);


            KinectSensor sensor = KinectSensor.KinectSensors.First();
            var format = DepthImageFormat.Resolution80x60Fps30;
            DepthCamera depthCam = new DepthCamera(sensor, format);
            KinectCalibrator kc = new KinectCalibrator(cc);
            SkeletonCamera scam = new SkeletonCamera(sensor);
            sensor.Start();
            float[] data = Utils.DeSerializeObject<float[]>(LightningFastApp.IR2RGBFILE) ?? MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.Identity(4).ToColumnWiseArray();
            MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> D2C = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfColumnMajor(4, 4, data);
            //var adjustment = OpenTK.Matrix4.CreateTranslation(0f, 0.14f, 0.00f);
            //program.SetProjection(pc, kc.GetModelView(D2C), adjustment);

            //program.SetProjection(pc); //, kc.GetModelView(D2C), OpenTK.Matrix4.CreateTranslation(0f, 0.14f, 0.06f));
            //var rs = Range.OfDoubles(0.5, -0.5, 0.04);
            //program.SetPositions(rs.SelectMany(x => rs.Select(y => new Vector3((float)x, (float)y, 1.7f))).ToArray());
            //while (true)
            //{
            //    window.RenderFrame();
            //    window.ProcessEvents();
            //}
            var keyl = new KeyboardListener(window.Keyboard);
            SkeletonPoint[] skeletons = null;
            // Action H hide background:
            bool hideBackground = false;
            float zCutoff = 3.4f;
            float xadj = 0, yadj = 0.0f, zadj = 0;
            float incr = 0.01f;
            bool adjust = true;
            program.SetProjection(pc, kc.GetModelView(D2C), OpenTK.Matrix4.CreateTranslation(xadj, yadj, zadj));
            Action<float> xfoo = (f) =>
            {
                if (!adjust)
                    return;
                xadj += f;
                program.SetProjection(pc, kc.GetModelView(D2C), OpenTK.Matrix4.CreateTranslation(xadj, yadj, zadj));
            };
            Action<float> yfoo = (f) =>
            {
                if (!adjust)
                    return;
                yadj += f;
                program.SetProjection(pc, kc.GetModelView(D2C), OpenTK.Matrix4.CreateTranslation(xadj, yadj, zadj));
            };
            Action<float> zfoo = (f) =>
            {
                if (!adjust)
                    return;
                zadj += f;
                program.SetProjection(pc, kc.GetModelView(D2C), OpenTK.Matrix4.CreateTranslation(xadj, yadj, zadj));
            };
            keyl.AddAction(() => adjust = !adjust, Key.A);
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, null, (i) => yfoo(i * incr));
            keyl.AddBinaryAction(1, -1, Key.Left, Key.Right, null, (i) => xfoo(i * incr));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ShiftLeft }, (i) => zfoo(i * incr));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ControlLeft }, (i) => yfoo(i * incr * 3));
            keyl.AddBinaryAction(1, -1, Key.Left, Key.Right, new Key[] { Key.ControlLeft }, (i) => xfoo(i * incr * 3));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ShiftLeft, Key.ControlLeft }, (i) => zfoo(i * incr * 3));

            keyl.AddAction(() =>
            {
                hideBackground = !hideBackground;
                if (skeletons != null && skeletons.Length > 0)
                {
                    zCutoff = skeletons.Min(sp => sp.Z);
                }
                else
                    zCutoff = 0;
            }, OpenTK.Input.Key.H);
            while (true)
            {
                var points = depthCam.Get(1000).Where(p => p.HasValue && p.Value.Index > 0).Select(p => p.Value);
                skeletons = points.Select(p => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, p.Point)).ToArray();
                program.SetPositions(skeletons.Where(sp => !hideBackground || sp.Z < zCutoff).Select(sp => new Vector3(sp.X, sp.Y, sp.Z)).ToArray());
                overview.SetPointCloud(0, skeletons.Select(sp =>
                {
                    var gp = kc.ToGlobal(sp);
                    return new DynamicVertex(new Vector3(gp[0], gp[1], gp[2]));
                }).ToArray());
                window.RenderFrame();
                overviewWindow.RenderFrame();
                window.ProcessEvents();
                overviewWindow.ProcessEvents();
            }
        }
    }
}

