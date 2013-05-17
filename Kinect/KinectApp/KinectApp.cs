using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.Kinect.KinectApp
{
    class KinectApp
    {
        static void Main(string[] args)
        {
            var sensor = Microsoft.Kinect.KinectSensor.KinectSensors.FirstOrDefault(sens => sens.Status == KinectStatus.Connected);
            if (sensor == null)
                return;
            sensor.SkeletonStream.Enable();
            var skeletonData = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];
            sensor.SkeletonFrameReady += (o, arg) =>
            {
                using (SkeletonFrame frame = arg.OpenSkeletonFrame())
                {
                    if (frame == null || skeletonData == null)
                        return;
                    frame.CopySkeletonDataTo(skeletonData);
                    foreach (var skeleton in skeletonData)
                    {
                        
                    }
                }
            };
            sensor.Start();
            sensor.ElevationAngle = 0;
            sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            

            Vector<double> p0 = null, p1 = null, p2 = null;
            Func<Vector<double>, Vector<double>> transform = null;
            Func<Vector<double>> readRightHand = () => skeletonData.Where(row => row.TrackingState == SkeletonTrackingState.Tracked).Select(row => row.Joints[JointType.HandRight]).Select(row => new DenseVector(new double[] { row.Position.X, row.Position.Y, row.Position.Z })).FirstOrDefault();
            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.D0)
                    p0 = readRightHand();
                if (key.Key == ConsoleKey.D1)
                    p1 = readRightHand();
                if (key.Key == ConsoleKey.D2)
                    p2 = readRightHand();
                if (p0 != null && p1 != null && p2 != null)
                {
                    transform = WorldTransform.GenerateTransform(p0, p1, p2);
                }
                if (transform != null)
                {
                    var transformed = transform(readRightHand());
                    Console.Clear();
                    Console.WriteLine("Transformed: " + PosToString(transformed));
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Untransformed: " + PosToString(readRightHand()));
                }
            }
        }
        static string PosToString(Vector<double> p)
        {
            if (p == null)
                return "";
            return string.Format("({0:0.00}, {1:0.00}, {2:0.00})", p[0], p[1], p[2]);
        }
    }
}
