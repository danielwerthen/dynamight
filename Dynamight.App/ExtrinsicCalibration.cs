using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class ExtrinsicCalibration
    {
        public static void Run(string[] args)
        {
            var intrinsicfile = args.FirstOrDefault() ?? Calibration.KinectDefaultFileName;
            var intrinsic = Utils.DeSerializeObject<CalibrationResult>(intrinsicfile);

            KinectSensor sensor = KinectSensor.KinectSensors.First();
            var kinects = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).ToArray();
            var cameras = kinects.Select(s =>
            {
                s.Start();
                return new Camera(s, ColorImageFormat.RgbResolution1280x960Fps12);
            }).ToArray();
            Projector projector = new Projector();
            PointF[][] corners;
            while (true)
            {
                Console.WriteLine("Place checkerboard at the origo position, make sure it is visible to all connected Kinects, and press enter.");
                Console.ReadLine();
                corners = cameras.Select(camera => StereoCalibration.GetCameraCorners(projector, camera, new Size(7, 4), false)).ToArray();
                if (corners.All(c => c != null))
                {
                    break;
                }
                else
                    Console.WriteLine("Could not find any corners, make sure the checkerboard is visible to all Kinects.");
            }
            var results = cameras.Zip(corners, (camera, cs) => StereoCalibration.CalibrateCamera(cs, camera, new Size(7, 4), 0.05f, intrinsic)).ToArray();
            kinects.Zip(results, (kinect, result) =>
            {
                Utils.SerializeObject(result, kinect.UniqueKinectId + ".xml");
                return true;
            }).ToArray();
        }
    }
}
