using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Dynamight.Processing;
using Dynamight.RemoteSlave;
using Graphics;
using Graphics.Projection;
using Microsoft.Kinect;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class OverviewApp
    {
        public static void Run(string[] args)
        {

            var camfile = args.FirstOrDefault() ?? Calibration.KinectDefaultFileName;
            if (!File.Exists(camfile))
            {
                Console.WriteLine("Calib file could not be found.");
                return;
            }
            var cc = Utils.DeSerializeObject<CalibrationResult>(camfile);
            var overview = new ProgramWindow(750, 50, 640, 480);
            overview.Load();
            overview.ResizeGraphics();
            OverviewProgram program = new OverviewProgram();
            overview.SetProgram(program);
            KinectSensor sensor = KinectSensor.KinectSensors.First();
            var format = DepthImageFormat.Resolution80x60Fps30;
            DepthCamera depthCam = new DepthCamera(sensor, format);
            SkeletonCamera skeletonCam = new SkeletonCamera(sensor);
            TriplexCamera triplex = new TriplexCamera(sensor, depthCam, skeletonCam);
            KinectCalibrator kc = new KinectCalibrator(sensor, cc);
            sensor.Start();


            while (true)
            {
                var players = triplex.Trigger(1000);
                if (players.Length > 0)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        program.SetPointCloud(i, players[i].DepthPoints.Select(dp =>
                        {
                            var sp = sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, dp);
                            var gp = kc.ToGlobal(sp);
                            return new DynamicVertex(new Vector3(gp[0], gp[1], gp[2]));
                        }).ToArray());
                    }
                }
                overview.ProcessEvents();
                overview.RenderFrame();
            }
        }
    }
}
