using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class HandLightning
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
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.Start();
            double rot = 0.0;
            while (true)
            {
                using (var frame = sensor.DepthStream.OpenNextFrame(1200))
                {
                    projector.DrawCheckerboard(new System.Drawing.Size(8, 7), rot, 0, rot, 0.7);
                    rot += 0.01;
                }
            }
        }
    }
}
