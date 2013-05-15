using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Threading;

namespace Dynamight.Tests.KinectTest
{
    class Program
    {
        static Skeleton[] skeletonData;
        static void Main(string[] args)
        {
            var sensor = Microsoft.Kinect.KinectSensor.KinectSensors.FirstOrDefault(sens => sens.Status == KinectStatus.Connected);
            if (sensor == null)
                return;
            sensor.SkeletonStream.Enable();
            skeletonData = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];
            sensor.SkeletonFrameReady += (o, arg) =>
            {
                using (SkeletonFrame frame = arg.OpenSkeletonFrame())
                {
                    if (frame == null || skeletonData == null)
                        return;
                    frame.CopySkeletonDataTo(skeletonData);
                }
            };
            sensor.Start();
            sensor.ElevationAngle = 0;
            while (true)
            {
                Draw();
                Thread.Sleep(10);
            }
        }


        static void Draw()
        {
            Console.Clear();
            if (skeletonData == null)
            {
                Console.WriteLine("No data");
                return;
            }
            foreach (var skeleton in skeletonData.Where(row => row != null))
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    Console.WriteLine("Skelett " + skeleton.TrackingId + ": Tracked at " + PosToString(skeleton.Position));
                else
                    continue;
                var rightHand = skeleton.Joints[JointType.HandRight];
                Console.WriteLine("Hand is at position: " + PosToString(rightHand.Position));
                
            }
        }

        static string PosToString(SkeletonPoint p)
        {
            return string.Format("({0:0.00}, {1:0.00}, {2:0.00})", p.X, p.Y, p.Z);
        }
    }
}
