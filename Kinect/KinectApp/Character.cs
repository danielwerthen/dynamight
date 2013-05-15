using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Dynamight.Kinect.KinectApp
{
    public class Character
    {
        private Character()
        {
        }

        public Vector<double> Position { get; set; }
        public Vector<double> RightHand { get; set; }

        public static Character CreateCharacter(Skeleton skeleton, Matrix<double> kinectToGlobal)
        {
            Character res = new Character();
            res.Position = kinectToGlobal.Multiply(new DenseVector(new double[] { skeleton.Position.X, skeleton.Position.Y, skeleton.Position.Z }));
            var rightHand = skeleton.Joints[JointType.HandRight];
            res.Position = kinectToGlobal.Multiply(new DenseVector(new double[] { rightHand.Position.X, rightHand.Position.Y, rightHand.Position.Z }));
            return res;
        }
    }
}
