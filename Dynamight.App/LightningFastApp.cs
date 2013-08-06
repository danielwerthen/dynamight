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
    public class LightningFastApp
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
            program.SetProjection(pc, kc.GetC2GModelView());

            MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> D2C = null;
            TimedBlockRecorder rec = new TimedBlockRecorder();
            while (true)
            {
                CompositePlayer[] players;
                DepthImagePoint[] depths;
                using (var block = rec.GetBlock("Triplex trigger"))
                    players = triplex.Trigger(1000);
                if (players == null)
                    continue;

                using (var block = rec.GetBlock("Player sorting into depths"))
                    depths = players.Where(p => p.Skeleton != null).SelectMany(s => s.DepthPoints).ToArray();
                if (depths.Count() > 0)
                {
                    if (D2C == null)
                    {
                        using (var block = rec.GetBlock("Find Rigid transform"))
                        {
                            var points = kc.ToColorSpace(sensor.CoordinateMapper, depths, format);
                            var B = points.Select(p => new float[] { p.X, p.Y, p.Z }).ToArray();
                            var A = depths.Select(d => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, d)).Select(sp => new float[] { sp.X, sp.Y, sp.Z }).ToArray();
                            D2C = Dynamight.ImageProcessing.CameraCalibration.Maths.RigidTransformation.FindTransform(A, B);
                            program.SetProjection(pc, kc.GetModelView(D2C));
                        }
                    }
                    Vector3[] ps;
                    using (var block = rec.GetBlock("Map to skeleton space"))
                        ps = depths.Select(d => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, d)).Select(d => new Vector3(d.X, d.Y, d.Z)).ToArray();
                    using (var block = rec.GetBlock("Set positions"))
                        program.SetPositions(ps);
                }
                else
                    program.SetPositions(new Vector3[0]);
                using (var block = rec.GetBlock("Render frame"))
                    window.RenderFrame();
                window.ProcessEvents();
                Console.Clear();
                Console.WriteLine(rec.ToString());
                Console.WriteLine(rec.AverageAll());
            }
        }
    }
}

