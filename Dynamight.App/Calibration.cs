﻿using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Dynamight.Processing.Audio;
using Graphics;
using Microsoft.Kinect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Dynamight.App
{
    public class Calibration
    {

        static void TakePics(Camera camera, Projector projector)
        {
            while (true)
            {
                var data = StereoCalibration.GatherData(projector, camera, new Size(7, 4), 1,
                    (pass) =>
                    {
                        Console.WriteLine("Reject or Approve? (r/a).");
                        return Console.ReadLine() != "r";
                    }).First();
                Console.WriteLine("Enter filename: ");
                var name = Console.ReadLine();
                var refer = camera.TakePicture(0);
                refer.Save(name + ".bmp");
                Utils.SerializeObject(data, name + ".xml");
                Console.WriteLine("Enter command: (n)ew pic, (r)eturn");
                var command = Console.ReadLine();
                if (command == "r") return;
            }
        }

        static StereoCalibrationResult Calculate(Camera camera, Projector projector)
        {
            List<string> files = new List<string>();
            while (true)
            {
                Console.WriteLine("Enter file name with pass data: (q) when done");
                var str = Console.ReadLine();
                if (str == "q")
                    break;
                files.Add(str);
            }
            var data = files.Select(str => Utils.DeSerializeObject<CalibrationData>(str + ".xml")).ToArray();
            var calib = StereoCalibration.Calibrate(data, camera, projector, new Size(7, 4), 0.05f);
            projector.DrawBackground(System.Drawing.Color.Black);
            var peas = new float[][] {
                new float[] { 0f, 0f, 0f },
                new float[] { 0.1f, 0f, 0f },
                new float[] { 0f, 0.1f, 0f },
                new float[] { 0f, 0f, 0.1f },
            };
            var tpe = calib.TransformG2P(peas);
            var tpp = calib.TransformG2C(peas);
            var cp = camera.TakePicture(0);
            QuickDraw.Start(cp).DrawPoint(tpp, 5).Finish();
            StereoCalibration.DebugWindow.DrawBitmap(cp);
            projector.DrawPoints(tpe, 5);

            Console.WriteLine("Enter command: (d)one, (s)tart again");
            var command = Console.ReadLine();
            
            return command == "d" ? calib : null;
        }

        static StereoCalibrationResult Calibrator(Camera camera, Projector projector)
        {

            while (true)
            {
                Console.WriteLine("Enter command: take (p)ics, (c)alculate results");
                var command = Console.ReadLine();
                if (command == "p")
                    TakePics(camera, projector);
                else if (command == "c")
                {
                    var result = Calculate(camera, projector);
                    if (result != null)
                        return result;
                }
            }
        }

        static StereoCalibrationResult Calibrate(Camera camera, Projector projector
            , bool reload = true, bool reloadData = true)
        {
            string filename = "calibrationresult.xml";
            string datafile = "calibrationdata10p.xml";
            while (true)
            {
                if (File.Exists(filename) && !reload)
                    return Utils.DeSerializeObject<StereoCalibrationResult>(filename);
                CalibrationData[] data;
                if (File.Exists(datafile) && !reloadData)
                    data = Utils.DeSerializeObject<CalibrationData[]>(datafile);
                else
                {
                    data = StereoCalibration.GatherData(projector, camera, new Size(7, 4), 10,
                        (pass) =>
                        {
                            Console.WriteLine("Pass: " + pass + " done. Reject or Approve? (r/a).");
                            return Console.ReadLine() != "r";
                        });
                    Utils.SerializeObject(data, datafile);
                }
                var result = StereoCalibration.Calibrate(data, camera, projector, new Size(7, 4), 0.05f);
                Utils.SerializeObject(result, filename);
                return result;
                Console.WriteLine("Proceed? (y/n)");
                var command = Console.ReadLine();
                if (command == "y")
                    return result;
            }
        }

        static CalibrationResult CalibrateCamera(Camera camera, Projector projector, out PointF[][] data, bool reloadData = false, bool reloadCalc = true, bool save = true)
        {
            string datafile = "cameradata.xml";
            string calcfile = "cameracalibration.xml";
            Size pattern = new Size(7,4);

            projector.DrawBackground(Color.Black);
            var list = new List<PointF[]>();
            if (!reloadData && File.Exists(datafile))
                list = Utils.DeSerializeObject<List<PointF[]>>(datafile);
            else
            {
                while (true)
                {
                    Console.WriteLine("Place checkerboard at the origo position and press enter.");
                    Console.ReadLine();
                    var cc = StereoCalibration.GetCameraCorners(projector, camera, new Size(7, 4), false);
                    if (cc != null)
                    {
                        list.Add(cc);
                        break;
                    }
                    else
                        Console.WriteLine("Could not find any corners, make sure the checkerboard is visible to the camera.");
                }
                Console.Write("First image OK. Enter number of required passes: ");
                var str = Console.ReadLine();
                int passes;
                if (!int.TryParse(str, out passes))
                    passes = 15;
                ConcurrentQueue<Bitmap> pics = new ConcurrentQueue<Bitmap>();
                ConcurrentQueue<PointF[]> corners = new ConcurrentQueue<PointF[]>();
                for (int i = 255; i >= 0; i--)
                {
                    projector.DrawBackground(Color.FromArgb(i, i, i));
                }
                var cornerTask = Task.Run(() =>
                {
                    Console.Write("Progress: ");
                    while (corners.Count < passes)
                    {
                        Bitmap img;
                        if (pics.TryDequeue(out img))
                        {
                            var cc = StereoCalibration.GetCameraCorners(img, pattern);
                            if (cc != null)
                            {
                                corners.Enqueue(cc);
                                Console.Write("=");
                            }
                            img.Dispose();
                        }
                        Thread.Yield();
                    }
                    Console.Write("> Done!\n");
                });
                while (corners.Count < passes)
                {
                    if (pics.Count < passes * 2)
                    {
                        Thread.Sleep(200);
                        projector.DrawBackground(Color.Black);
                        var pic = camera.TakePicture();
                        projector.DrawBackground(Color.White);
                        Thread.Sleep(50);
                        projector.DrawBackground(Color.Black);
                        pics.Enqueue(pic);
                    }
                    else
                    {
                        while (pics.Count > 4 && corners.Count < passes)
                        {
                            Thread.Sleep(1000);
                            Thread.Yield();
                        }
                        if (corners.Count < passes)
                        {
                            for (int i = 255; i >= 0; i--)
                            {
                                projector.DrawBackground(Color.FromArgb(i, i, i));
                            }
                        }
                    }
                    Thread.Yield();
                }
                cornerTask.Wait();
                foreach (var cc in corners)
                    list.Add(cc);
                if (save)
                    Utils.SerializeObject(list, datafile);
                foreach (var img in pics)
                    img.Dispose();

                //while (true)
                //{
                //    projector.DrawBackground(Color.Black);
                //    Thread.Sleep(120);
                //    var cc = StereoCalibration.GetCameraCorners(projector, camera, new Size(7, 4), false);
                //    if (cc != null)
                //    {
                //        list.Add(cc);
                //        projector.DrawBackground(Color.Green);
                //        Thread.Sleep(300);
                //    }
                //    else
                //    {
                //        projector.DrawBackground(Color.Red);
                //        Thread.Sleep(300);
                //    }

                //    if (list.Count > 25)
                //    {
                //        if (save)
                //            SerializeObject(list, datafile);
                //        break;
                //    }
                //}
                Console.WriteLine("Data gather done. Press enter to calculate calibration.");
                Console.ReadLine();
            }

            CalibrationResult calib;
            if (!reloadCalc && File.Exists(calcfile))
                calib = Utils.DeSerializeObject<CalibrationResult>(calcfile);
            else
            {
            
                calib = StereoCalibration.CalibrateCamera(list.ToArray()
                    , camera, new Size(7, 4), 0.05f);
                if (save)
                    Utils.SerializeObject(calib, calcfile);
            }
            data = list.ToArray();
            return calib;
        }

        static CalibrationResult CalibrateProjector(KinectSensor sensor, Camera camera, Projector projector, CalibrationResult cameraCalib, PointF[][] cacalibdata, bool reload = false, bool save = true)
        {
            CalibrationResult result;
            string datafile = "projectorcalibration.xml";
            if (!reload && File.Exists(datafile))
                result = Utils.DeSerializeObject<CalibrationResult>(datafile);
            else
            {
                VoiceCommander commander = new VoiceCommander(sensor);
                commander.LoadChoices("Shoot", "Ready", "Next", "Continue", "Carry on", "Smaller", "Bigger", "Go", "Stop");
                List<PointF[]> cam = new List<PointF[]>(), proj = new List<PointF[]>();
                Size pattern = new Size(8, 7);
                PointF[] cc;
                PointF[] pc;
                double size = 0.5;
                pc = projector.DrawCheckerboard(pattern, 0, 0, 0, size);
                while (true)
                {
                    var word = commander.Recognize("Ready?");
                    if (word == "Bigger")
                    {
                        size += 0.1;
                        pc = projector.DrawCheckerboard(pattern, 0, 0, 0, size);
                        continue;
                    }
                    else if (word == "Smaller")
                    {
                        size -= 0.1;
                        pc = projector.DrawCheckerboard(pattern, 0, 0, 0, size);
                        continue;
                    }
                    if (word == "Stop")
                        break;
                    cc = StereoCalibration.FindDualPlaneCorners(camera, pattern);
                    if (cc != null && pc != null)
                    {
                        cam.Add(cc);
                        proj.Add(pc);
                    }
                }
                projector.DrawBackground(Color.Black);
                result = StereoCalibration.CalibrateProjector(projector, cacalibdata, cam.ToArray(), proj.ToArray(), new Size(7, 4), 0.05f);
                //result = StereoCalibration.CalibrateProjector(projector, camera, new Size(8, 7), cameraCalib, cacalibdata, new Size(7, 4), 0.05f);
                if (save)
                    Utils.SerializeObject(result, datafile);
            }
            return result;
        }

        public const string KinectDefaultFileName = "kinectCalib.xml";
        public const string ProjectorDefaultFileName = "projCalib.xml";

        public static void Calibrate(string[] args)
        {
            var camfile = args.FirstOrDefault() ?? KinectDefaultFileName;
            var projfile = args.Skip(1).FirstOrDefault() ?? ProjectorDefaultFileName;
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var window = new BitmapWindow(main.Bounds.Left + main.Width / 2 + 50, 50, 640, 480);
            window.Load();
            window.ResizeGraphics();
            StereoCalibration.DebugWindow = window;
            KinectSensor sensor = KinectSensor.KinectSensors.First();
            sensor.Start();

            Camera camera = new Camera(sensor, ColorImageFormat.RgbResolution1280x960Fps12);
            Projector projector = new Projector();
            PointF[][] data;
            bool proceed = false;
            CalibrationResult cc, pc;
            do
            {
                cc = CalibrateCamera(camera, projector, out data, false, true, true);

                pc = CalibrateProjector(sensor, camera, projector, cc, data, true, false);

                var peas = new float[][] {
                    new float[] { 0f, 0f, 0.0f },
                    new float[] { 0.5f, 0f, 0.0f },
                    new float[] { 0f, -0.5f, 0.0f },
                    new float[] { 0.5f, -0.5f, 0.0f },
                };
                var tpe = pc.Transform(peas);
                var tpp = cc.Transform(peas);
                projector.DrawPoints(tpe, 25);
                var pic2 = camera.TakePicture(5);
                QuickDraw.Start(pic2).Color(Color.Green).DrawPoint(tpp, 15).Finish();
                window.DrawBitmap(pic2);
                Console.WriteLine("Save result? (y/n)");
                proceed = Console.ReadLine() == "y";
            } while (!proceed);

            Utils.SerializeObject(cc, camfile);
            Utils.SerializeObject(pc, projfile);
            window.Close();
            window.Dispose();
            projector.Close();
            camera.Dispose();
            sensor.Stop();
        }

        public static float[] MapSkeletonPoint(KinectSensor sensor, Joint joint)
        {
            var depth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, DepthImageFormat.Resolution640x480Fps30);
            var color = sensor.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution1280x960Fps12);
            return new float[] { depth.X / 1000f, -depth.Y / 1000f, -depth.Depth / 1000 };
        }

    }
}
