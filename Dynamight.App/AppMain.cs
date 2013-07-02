using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
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

        static StereoCalibrationResult Calibrate(Camera camera, Projector projector, bool reload = false)
        {
            string filename = "calibrationresult.xml";
            while (true)
            {
                if (File.Exists(filename) && !reload)
                    return DeSerializeObject<StereoCalibrationResult>(filename);
                var result = StereoCalibration.Calibrate(projector, camera, new System.Drawing.Size(7, 4), 0.05f, 10);
                SerializeObject(result, filename);
                return result;
                Console.WriteLine("Proceed? (y/n)");
                var command = Console.ReadLine();
                if (command == "y")
                    return result;
            }
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
            var calib = Calibrate(camera, projector, true);
            sensor.Stop();
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.SkeletonStream.Enable();
            sensor.Start();
            projector.DrawBackground(System.Drawing.Color.Black);
            var peas = new float[][] {
                new float[] { 0f, 0f, 0f },
                new float[] { 1f, 0f, 0f },
                new float[] { 0f, 1f, 0f },
                new float[] { 0f, 0f, 1f },
            };
            var tpe = calib.TransformG2P(peas);
            var tpp = calib.TransformG2C(peas);
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
                            var ppoints = calib.TransformC2P(points);
                            qd.DrawPoint(ppoints, 5);
                        }
                    }
                    qd.Finish();
                    projector.DrawBitmap(projector.bitmap);
                }
            }
        }

    }
}
