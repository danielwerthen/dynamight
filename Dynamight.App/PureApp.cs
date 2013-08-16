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
    public class PureApp
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
            Projector proj = new Projector();

            var format = DepthImageFormat.Resolution80x60Fps30;
            var inputs = KinectSensor.KinectSensors.Where(k => k.Status == KinectStatus.Connected).Select(k =>
            {
                k.Start();
                return new
                {
                    sensor = k,
                    depth = new DepthCamera(k, format),
                    skeleton = new SkeletonCamera(k),
                    calibrator = new KinectCalibrator(Utils.DeSerializeObject<CalibrationResult>(k.UniqueKinectId.Substring(k.UniqueKinectId.Length - 16) + ".xml"))
                };
            });
            //inputs.First().calibrator.ToGlobal(inputs.First().sensor, new SkeletonPoint() { X = 1, Y = 1, Z = 1 });
            while (true)
            {
                var points = inputs.Select(inp => new
                {
                    Calibrator = inp.calibrator,
                    Skeletons = inp.depth.Get(100).Where(p => p.HasValue && p.Value.Index > 0)
                        .Select(p => inp.sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, p.Value.Point))
                        .Select(sp => {
                            return inp.calibrator.ToGlobal(inp.sensor, sp);
                        })
                        .ToArray()
                });
                var tps = points.SelectMany(p => p.Skeletons).ToArray();
                if (tps.Length <= 0)
                    continue;
                var pcp = pc.Transform(tps);
                proj.DrawPoints(pcp, 1);
            }
        }
    }
}
