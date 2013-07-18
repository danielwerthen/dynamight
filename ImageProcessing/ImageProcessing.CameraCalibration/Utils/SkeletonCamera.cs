using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{
    public class SkeletonCamera
    {
        KinectSensor sensor;
        public SkeletonCamera(KinectSensor sensor)
        {
            this.sensor = sensor;
            sensor.SkeletonStream.Enable();
        }

        public Skeleton[] GetSkeletons(int wait = 1000)
        {
            using (var frame = sensor.SkeletonStream.OpenNextFrame(wait))
            {
                if (frame == null)
                    return null;
                var skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);
                return skeletons;
            }
        }
    }
}
