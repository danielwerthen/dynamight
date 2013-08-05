using Dynamight.RemoteSlave;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{
    public interface ISkeletonCamera
    {
        Skeleton[] GetSkeletons(int wait);
    }

    public class SkeletonCamera : ISkeletonCamera
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

    public class RemoteSkeletonCamera : ISkeletonCamera
    {
        RemoteKinect kinect;

        private Skeleton[] data;
        public RemoteSkeletonCamera(RemoteKinect kinect)
        {
            this.kinect = kinect;
            kinect.ReceivedSkeletons += kinect_ReceivedSkeletons;
        }

        void kinect_ReceivedSkeletons(object sender, SkeletonsEventArgs e)
        {
            data = e.Skeletons;
        }

        public Skeleton[] GetSkeletons(int wait = 1000)
        {
            return data;
        }
    }
}
