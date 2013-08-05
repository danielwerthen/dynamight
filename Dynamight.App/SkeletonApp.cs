using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Microsoft.Kinect;
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
    public class SkeletonApp
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
            Projector projector = new Projector();
            KinectSensor sensor = KinectSensor.KinectSensors.First();
            
            sensor.SkeletonStream.Enable();
            KinectCalibrator kc = new KinectCalibrator(cc);
            sensor.Start();

            while (true)
            {
                using (var frame = sensor.SkeletonStream.OpenNextFrame(10000))
                {
                    var skeletons = new Microsoft.Kinect.Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(skeletons);
                    var points = skeletons.Where(sk => sk.TrackingState == SkeletonTrackingState.Tracked)
                        .SelectMany(sk => sk.Joints).Where(j => j.TrackingState != JointTrackingState.NotTracked)
                        .Select(j => j.Position).ToArray();
                    if (points.Length > 0)
                    {
                        var globals = kc.ToGlobal(points).ToArray();
                        var projected = pc.Transform(globals);
                        projector.DrawPoints(projected, 25, Color.White);
                    }
                    else
                    {
                        projector.DrawBackground(Color.Black);
                    }
                }
            }
        }
    }
}
