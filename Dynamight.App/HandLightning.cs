﻿using Dynamight.ImageProcessing.CameraCalibration;
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
            DepthCamera cam = new DepthCamera(sensor, DepthImageFormat.Resolution80x60Fps30);
            KinectCalibrator kc = new KinectCalibrator(sensor, cc);
            sensor.Start();

            while (true)
            {
                var test = cam.GetDepth(10000);
                var sp = test.Select(f => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(DepthImageFormat.Resolution80x60Fps30, f));
                var globals = kc.ToGlobal(sp).ToArray();
                {
                    var projected = pc.Transform(globals.Where(g => g[2] > -0.0f).ToArray());
                    projector.DrawPoints(projected,2, Color.Gray);
                }
            }
        }
    }
}
