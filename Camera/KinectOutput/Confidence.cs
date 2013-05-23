using Microsoft.Kinect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectOutput
{
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (this)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }

    public class Confidence
    {
        public double InferredConfidence { get; set; }
        public double LengthConfidence { get; set; }
        FixedSizedQueue<double> lengths;
        public bool Active { get; set; }
        public Confidence(Skeleton skeleton, int lengthBufferLength = 5)
        {
            InferredConfidence = GetInferredConfidence(skeleton);
            double ln;
            LengthConfidence = GetLengthConfidence(skeleton, null, out ln);
            lengths = new FixedSizedQueue<double>(lengthBufferLength);
            lengths.Enqueue(ln);
            Active = true;
        }

        public void Update(Skeleton skeleton)
        {
            InferredConfidence = GetInferredConfidence(skeleton);
            double ln;
            LengthConfidence = GetLengthConfidence(skeleton, lengths.Sum() / lengths.Count, out ln);
            lengths.Enqueue(ln);
            Active = true;
        }

        public static double GetInferredConfidence(Skeleton skeleton)
        {
            return 1 - (double)skeleton.Joints.Where(row => row.TrackingState == JointTrackingState.Inferred).Count() / 
                (double)skeleton.Joints.Where(row => row.TrackingState == JointTrackingState.Tracked || row.TrackingState == JointTrackingState.Inferred).Count();
        }

        public static double GetLengthConfidence(Skeleton skeleton, double? length, out double currentLength)
        {
            Func<JointType, JointType, double> foo = (t1, t2) => Length(skeleton.Joints[t1], skeleton.Joints[t1]);
            currentLength = foo(JointType.HandLeft, JointType.WristLeft)
                + foo(JointType.WristLeft, JointType.ElbowLeft)
                + foo(JointType.ElbowLeft, JointType.ShoulderLeft)
                + foo(JointType.ShoulderLeft, JointType.ShoulderCenter)
                + foo(JointType.ShoulderCenter, JointType.ShoulderRight)
                + foo(JointType.ShoulderRight, JointType.ElbowRight)
                + foo(JointType.ElbowRight, JointType.WristRight)
                + foo(JointType.WristRight, JointType.HandRight);
            if (length == null)
                return currentLength > 0 ? 1 : 0;
            if (length > currentLength)
                return currentLength / length.Value;
            else
                return length.Value / currentLength;
        }

        private static double Length(Joint a, Joint b)
        {
            if (!(a.TrackingState == JointTrackingState.Tracked && b.TrackingState == JointTrackingState.Tracked))
                return 0;
            return Math.Sqrt(a.Position.X * b.Position.X + a.Position.Y * b.Position.Y + a.Position.Z * b.Position.Z);
        }
    }
}
