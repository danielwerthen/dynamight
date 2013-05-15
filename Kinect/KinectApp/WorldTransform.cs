using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.Kinect.KinectApp
{
    public class WorldTransform
    {
        public static Func<Vector<double>, Vector<double>> GenerateTransform(Vector<double> p0, Vector<double> p1, Vector<double> p2)
        {
            Vector<double> f2 = (p1 - p0).Normalize(1);
            Vector<double> f1 = (p2 - p0).Normalize(1);
            f1 = (f1 - (f1.DotProduct(f2) * f2)).Normalize(1);
            var f3 = new DenseVector(new double[] { f1[1] * f2[2] - f1[2] * f2[1], f1[2]*f2[0] - f1[0] * f2[2], f1[0]*f2[1] - f1[1]*f2[0] });
            var transform = DenseMatrix.OfColumns(3, 3, new Vector<double>[] { f1, f2, f3 });
            return (x) => {
                if (x == null)
                    return null;
                var res = x - p0;
                return transform.Multiply(res);
            };

        }
    }
}
