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

        static CalibrationResult CalibrateProjector(Camera camera, Projector projector, CalibrationResult cameraCalib, PointF[][] cacalibdata)
        {
            return StereoCalibration.CalibrateProjector(projector, camera, new Size(8, 7), cameraCalib, cacalibdata, new Size(7,4), 0.05f);
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

            sensor.Stop();
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.SkeletonStream.Enable();
            sensor.Start();
            projector.DrawBackground(System.Drawing.Color.Black);
            var peas = new float[][] {
                new float[] { 0f, 0f, 0f },
                new float[] { 0.1f, 0f, 0f },
                new float[] { 0f, 0.1f, 0f },
                new float[] { 0f, 0f, 0.1f },
            };
            var tpe = pc.Transform(peas);
            var tpp = cc.Transform(peas);
            var test = tpe.Select(row => new PointF(-row.X, -row.Y)).ToArray();
            var cp = camera.TakePicture(0);
            QuickDraw.Start(cp).DrawPoint(tpp, 5).Finish();
            window.DrawBitmap(cp);
            projector.DrawPoints(tpe, 5);

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

                    var qd = QuickDraw.Start(projector.bitmap);
                    {
                        qd.Fill(System.Drawing.Color.Black);
                        var points = skeletons.Where(row => row.TrackingState == SkeletonTrackingState.Tracked)
                            .SelectMany(skeleton => skeleton.Joints.Where(joint => joint.TrackingState == JointTrackingState.Tracked))
                            .Select(joint => sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, DepthImageFormat.Resolution640x480Fps30))
                            .ToArray();
                            
                        if (points.Length > 0)
                        {
                            //var ppoints = calib.TransformC2P(points);
                            //qd.DrawPoint(ppoints, 5);
                        }
                    }
                    qd.Finish();
                    projector.DrawBitmap(projector.bitmap);
                }
            }
        }

    }
}
