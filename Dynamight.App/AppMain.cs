using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Microsoft.Kinect;
using System;
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
    class AppMain
    {
        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializableObject"></param>
        /// <param name="fileName"></param>
        public static void SerializeObject<T>(T serializableObject, string fileName)
        {
            if (serializableObject == null) { return; }

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, serializableObject);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(fileName);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                //Log exception here
            }
        }


        /// <summary>
        /// Deserializes an xml file into an object list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static T DeSerializeObject<T>(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default(T); }

            T objectOut = default(T);

            try
            {
                string attributeXml = string.Empty;

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(fileName);
                string xmlString = xmlDocument.OuterXml;

                using (StringReader read = new StringReader(xmlString))
                {
                    Type outType = typeof(T);

                    XmlSerializer serializer = new XmlSerializer(outType);
                    using (XmlReader reader = new XmlTextReader(read))
                    {
                        objectOut = (T)serializer.Deserialize(reader);
                        reader.Close();
                    }

                    read.Close();
                }
            }
            catch (Exception ex)
            {
                //Log exception here
            }

            return objectOut;
        }

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
                SerializeObject(data, name + ".xml");
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
            var data = files.Select(str => DeSerializeObject<CalibrationData>(str + ".xml")).ToArray();
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
                    return DeSerializeObject<StereoCalibrationResult>(filename);
                CalibrationData[] data;
                if (File.Exists(datafile) && !reloadData)
                    data = DeSerializeObject<CalibrationData[]>(datafile);
                else
                {
                    data = StereoCalibration.GatherData(projector, camera, new Size(7, 4), 10,
                        (pass) =>
                        {
                            Console.WriteLine("Pass: " + pass + " done. Reject or Approve? (r/a).");
                            return Console.ReadLine() != "r";
                        });
                    SerializeObject(data, datafile);
                }
                var result = StereoCalibration.Calibrate(data, camera, projector, new Size(7, 4), 0.05f);
                SerializeObject(result, filename);
                return result;
                Console.WriteLine("Proceed? (y/n)");
                var command = Console.ReadLine();
                if (command == "y")
                    return result;
            }
        }

        static CalibrationResult CalibrateCamera(Camera camera, Projector projector, out PointF[][] data, bool reloadData = false, bool reloadCalc = true)
        {
            string datafile = "cameradata.xml";
            string calcfile = "cameracalibration.xml";


            projector.DrawBackground(Color.Black);
            var list = new List<PointF[]>();
            if (!reloadData && File.Exists(datafile))
                list = DeSerializeObject<List<PointF[]>>(datafile);
            else
            {
                while (true)
                {
                    var cc = StereoCalibration.GetCameraCorners(projector, camera, new Size(7, 4), false);
                    if (cc != null)
                    {
                        list.Add(cc);
                        break;
                    }
                }
                Console.WriteLine("First image OK. Press enter to continue with manual step.");
                Console.ReadLine();
                while (true)
                {
                    projector.DrawBackground(Color.White);
                    Thread.Sleep(120);
                    var cc = StereoCalibration.GetCameraCorners(projector, camera, new Size(7, 4), false);
                    if (cc != null)
                    {
                        list.Add(cc);
                        projector.DrawBackground(Color.Green);
                        Thread.Sleep(300);
                    }
                    else
                    {
                        projector.DrawBackground(Color.Red);
                        Thread.Sleep(300);
                    }

                    if (list.Count > 25)
                    {
                        SerializeObject(list, datafile);
                        break;
                    }
                }
                projector.DrawBackground(Color.Orange);
                Thread.Sleep(120);
                Console.WriteLine("Data gather done. Press enter to calculate calibration.");
                Console.ReadLine();
            }

            CalibrationResult calib;
            if (!reloadCalc && File.Exists(calcfile))
                calib = DeSerializeObject<CalibrationResult>(calcfile);
            else
            {
            
                calib = StereoCalibration.CalibrateCamera(list.ToArray()
                    , camera, new Size(7, 4), 0.05f);
            }
            data = list.ToArray();
            projector.DrawBackground(System.Drawing.Color.Black);
            var peas = new float[][] {
                new float[] { 0f, 0f, 0f },
                new float[] { 0.1f, 0f, 0f },
                new float[] { 0f, 0.1f, 0f },
                new float[] { 0f, 0f, 0.1f },
            };
            var t = calib.Transform(peas);
            var cp = camera.TakePicture(0);
            QuickDraw.Start(cp).DrawPoint(t, 5).Finish();
            StereoCalibration.DebugWindow.DrawBitmap(cp);
            return calib;
        }

        static CalibrationResult CalibrateProjector(Camera camera, Projector projector, CalibrationResult cameraCalib, PointF[][] cacalibdata, bool reload = false)
        {
            CalibrationResult result;
            string datafile = "projectorcalibration.xml";
            if (!reload && File.Exists(datafile))
                result = DeSerializeObject<CalibrationResult>(datafile);
            else
            {
                result = StereoCalibration.CalibrateProjector(projector, camera, new Size(8, 7), cameraCalib, cacalibdata, new Size(7, 4), 0.05f);
                SerializeObject(result, datafile);
            }
            return result;
        }

        public static CalibrationResult CalibrateIR(KinectSensor sensor, Camera camera, Projector projector, CalibrationResult cameraResult, PointF[][] cacalibdata)
        {
            return StereoCalibration.CalibrateIR(sensor, camera, projector, new Size(7,4), 0.05f);
        }

        static void Main(string[] args)
        {
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var window = new BitmapWindow(main.Bounds.Left + main.Width / 2 + 50, 50, 640, 480);
            window.Load();
            window.ResizeGraphics();
            StereoCalibration.DebugWindow = window;

            KinectSensor sensor = KinectSensor.KinectSensors.First();
            
            Camera camera = new Camera(sensor, ColorImageFormat.RgbResolution1280x960Fps12);

            
            Projector projector = new Projector();

            PointF[][] data;
            var cc = CalibrateCamera(camera, projector, out data);
            var pc = CalibrateProjector(camera, projector, cc, data);
            var ic = CalibrateIR(sensor, camera, projector, cc, data);


            if (true)
            {

                var joints = DeSerializeObject<SkeletonPoint[]>("skeleton.xml");
                var kc2 = new KinectCalibrator(sensor, cc);
                var gour = joints.Select(p => kc2.ToGlobal(p)).ToArray();
                var ourway = cc.Transform(gour);
                var apiway = joints.Select(p => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(p, ColorImageFormat.RgbResolution1280x960Fps12))
                    .Select(p => new PointF(p.X, p.Y)).ToArray();
                var projway = pc.Transform(gour);
                var pic = new Bitmap("reference.bmp");
                QuickDraw.Start(pic).Color(Color.Green).DrawPoint(ourway, 5)
                    .Color(Color.Red).DrawPoint(apiway, 5).Finish();
                window.DrawBitmap(pic);
                projector.DrawPoints(projway, 5);
                var globals = kc2.ToGlobal(joints).ToArray();
                var campoints = cc.Transform(globals);
                var projpoints = pc.Transform(globals);
                projector.DrawBackground(Color.Black);
                projector.DrawPoints(projpoints, 5);
                while (true)
                    Thread.Sleep(500);
            }



            sensor.Stop();
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            //sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.SkeletonStream.Enable();
            sensor.Start();
            projector.DrawBackground(System.Drawing.Color.Black);
            var peas = new float[][] {
                new float[] { 0f, 0f, 0.0f },
                new float[] { 0.5f, 0f, 0.0f },
                new float[] { 0f, -0.5f, 0.0f },
                new float[] { 0.5f, -0.5f, 0.0f },
            };
            var tpe = pc.Transform(peas);
            var tpp = cc.Transform(peas);
            projector.DrawPoints(tpe, 25);
            var pic2 = camera.TakePicture(0);
            QuickDraw.Start(pic2).Color(Color.Green).DrawPoint(tpp, 15).Finish();
            window.DrawBitmap(pic2);

            var kc = new KinectCalibrator(sensor, ic);

            while (sensor.IsRunning)
            {
                var skeletons = new Skeleton[0];
                using (var frame = sensor.SkeletonStream.OpenNextFrame(1000))
                {
                    if (frame != null)
                    {
                        skeletons = new Skeleton[frame.SkeletonArrayLength];
                        frame.CopySkeletonDataTo(skeletons);
                    }
                    else
                        continue;
                    var allJoints = skeletons.Where(row => row.TrackingState == SkeletonTrackingState.Tracked)
                        .SelectMany(skeleton => skeleton.Joints.Where(joint => joint.TrackingState == JointTrackingState.Tracked)).ToArray();// && joint.JointType == JointType.HandRight));
                    var jpoints = allJoints.Select(j => j.Position).ToArray();
                    var points = kc.ToGlobal(jpoints).ToArray();
                    if (points.Length > 0)
                    {
                        //Console.Clear();
                        //for (var i = 0; i < allJoints.Length; i++)
                        //{
                        //    Console.WriteLine("{0}: ({1}, {2}, {3})",
                        //        allJoints[i].JointType.ToString(),
                        //        points[i][0],
                        //        points[i][1],
                        //        points[i][2]);
                        //}
                        var projp = pc.Transform(points.Concat(new float[][] { new float[] {0f,0f,0f} }).ToArray());
                        var camp = jpoints.Select(p => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(p, sensor.ColorStream.Format))
                            .Select(ip => new PointF(1280 - ip.X, ip.Y)).ToArray();
                        
                        var camp2 = cc.Transform(points);
                        projector.DrawPoints(projp, 10);
                        var cp = camera.TakePicture(0);
                        QuickDraw.Start(cp).Color(Color.Red).DrawPoint(camp2, 5)
                            .Color(Color.Green).DrawPoint(camp, 5).Finish();
                        window.DrawBitmap(cp);
                        //var ppoints = calib.TransformC2P(points);
                        //qd.DrawPoint(ppoints, 5);
                    }
                }
            }
        }

        public static float[] MapSkeletonPoint(KinectSensor sensor, Joint joint)
        {
            var depth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, DepthImageFormat.Resolution640x480Fps30);
            var color = sensor.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution640x480Fps30, depth, ColorImageFormat.RgbResolution1280x960Fps12);
            return new float[] { depth.X / 1000f, -depth.Y / 1000f, -depth.Depth / 1000 };
        }

    }
}
