using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace KinectOutput
{
    public struct CalibrationResult
    {
        public Vector<double> P0;
        public Vector<double> F1;
        public Vector<double> F2;
        public Vector<double> F3;
    }
}
