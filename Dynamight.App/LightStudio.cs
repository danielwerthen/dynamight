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

namespace Dynamight.App
{
    public class LightningStudio
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

            var program = new LightStudioProgram(0.01f);
            window.SetProgram(program);


            KinectSensor sensor = KinectSensor.KinectSensors.First();
            var format = DepthImageFormat.Resolution80x60Fps30;
            DepthCamera depthCam = new DepthCamera(sensor, format);
            KinectCalibrator kc = new KinectCalibrator(cc);
            sensor.Start();
            float[] data = Utils.DeSerializeObject<float[]>(LightningFastApp.IR2RGBFILE) ?? MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.Identity(4).ToColumnWiseArray();
            MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> D2C = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfColumnMajor(4, 4, data);
            program.SetProjection(pc, kc.GetModelView(D2C), OpenTK.Matrix4.CreateTranslation(0f, 0.14f, 0.06f));
            //program.SetProjection(pc); //, kc.GetModelView(D2C), OpenTK.Matrix4.CreateTranslation(0f, 0.14f, 0.06f));
            var rs = Range.OfDoubles(0.5, -0.5, 0.04);
            program.SetPositions(rs.SelectMany(x => rs.Select(y => new Vector3((float)x, (float)y, 1.7f))).ToArray());
            while (true)
            {
                window.RenderFrame();
                window.ProcessEvents();
            }
            var keyl = new KeyboardListener(window.Keyboard);
            SkeletonPoint[] skeletons = null;
            // Action H hide background:
            bool hideBackground = true;
            float zCutoff = 1.97f;
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
                var points = depthCam.Get(1000);
                skeletons = points.Where(p => p.HasValue).Select(p => p.Value).Select(p => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, p.Point)).ToArray();
                program.SetPositions(skeletons.Where(sp => !hideBackground || sp.Z < zCutoff).Select(sp => new Vector3(sp.X, sp.Y, sp.Z)).ToArray());
                window.RenderFrame();
                window.ProcessEvents();
            }
        }
    }
}

