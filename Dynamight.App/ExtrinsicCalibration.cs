using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics.Input;
using Microsoft.Kinect;
using OpenTK.Input;
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
            var camIntrinsic = Utils.DeSerializeObject<CalibrationResult>(intrinsicfile);
            var projFile = args.Skip(1).FirstOrDefault() ?? Calibration.ProjectorDefaultFileName;
            var projIntrinsic = Utils.DeSerializeObject<CalibrationResult>(projFile);
            KinectSensor sensor = KinectSensor.KinectSensors.First();
            Camera cam = new Camera(sensor, ColorImageFormat.RgbResolution1280x960Fps12);
            Projector proj = new Projector();
            var keyl = new KeyboardListener(proj.window.Keyboard);
            double offsetx = 0, offsety = 0, scale = 0.5;
            bool proceed = false, quit = false;
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[0], (f) => offsety += f);
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Left, OpenTK.Input.Key.Right, new OpenTK.Input.Key[0], (f) => offsetx -= f);
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ShiftLeft }, (f) => scale += f);
            keyl.AddAction(() => proceed = true, Key.Space);
            keyl.AddAction(() => quit = proceed = true, Key.Q);
            PointF[] corners;
            proj.DrawBackground(Color.Black);
            while (true)
            {
                Console.WriteLine("Make sure the kinect can see the board");
                Console.ReadLine();
                corners = StereoCalibration.GetCameraCorners(cam.TakePicture(3), new Size(7, 4), false);
                if (corners.All(c => c != null))
                {
                    break;
                }
                else
                    Console.WriteLine("Could not find corners");
            }
            PointF[] projCorners;
            var projectedCorners = proj.DrawCheckerboard(new Size(8, 5), 0, 0, 0, scale, offsetx, offsety);
            while (true)
            {
                Console.WriteLine("Make sure the kinect can see the projection");
                while (!proceed)
                {
                    projectedCorners = proj.DrawCheckerboard(new Size(8, 5), 0, 0, 0, scale, offsetx, offsety);
                    proj.window.ProcessEvents();
                }
                projCorners = StereoCalibration.GetCameraCorners(cam.TakePicture(3), new Size(7, 4), false);
                if (corners.All(c => c != null))
                {
                    break;
                }
                else
                    Console.WriteLine("Could not find any corners, make sure the checkerboard is visible to all Kinects.");
            }

            var camResult = StereoCalibration.CalibrateCamera(corners, new Size(7, 4), 0.05f, camIntrinsic);
            var transform = StereoCalibration.FindHomography(projCorners, projectedCorners);
            var projResult = StereoCalibration.CalibrateCamera(transform(corners), new Size(7, 4), 0.05f, projIntrinsic);
            Utils.SerializeObject(camResult, intrinsicfile);
            Utils.SerializeObject(projResult, projFile);
        }

        public static void RunOld(string[] args)
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
            var results = cameras.Zip(corners, (camera, cs) => StereoCalibration.CalibrateCamera(cs, new Size(7, 4), 0.05f, intrinsic)).ToArray();
            kinects.Zip(results, (kinect, result) =>
            {
                Utils.SerializeObject(result, kinect.UniqueKinectId + ".xml");
                return true;
            }).ToArray();
        }
    }
}
