using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Single.Factorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration.Maths
{
    public class RigidTransformation
    {
        public static OpenTK.Matrix4 ToGLMatrix(Matrix<float> M)
        {
            var rt = M;
            return new OpenTK.Matrix4(
                rt[0, 0], rt[1, 0], rt[2, 0], rt[3, 0],
                rt[0, 1], rt[1, 1], rt[2, 1], rt[3, 1],
                rt[0, 2], rt[1, 2], rt[2, 2], rt[3, 2],
                rt[0, 3], rt[1, 3], rt[2, 3], rt[3, 3]);
        }
        public static Matrix<float> FindTransform(float[][] A, float[][] B, out float error)
        {
            var a = A.Select(v => new DenseVector(v)).ToArray();
            var b = B.Select(v => new DenseVector(v)).ToArray();
            var Ca = FindCentroid(a);
            var Cb = FindCentroid(b);

            var H = (DenseMatrix)a.Zip(b, (ai, bi) => (ai - Ca).ToColumnMatrix() * (bi - Cb).ToRowMatrix()).Aggregate((ai, bi) => ai + bi);
            var svd = new DenseSvd(H, true);
            var R = svd.VT().Transpose() * svd.U().Transpose();
            if (R.Determinant() < 0)
                R *= DenseMatrix.OfRows(3, 3, new float[][] { new float[] { 1, 1, 1 }, new float[] { 1, 1, 1 }, new float[] { -1, -1, -1 } });
            var T = -R * Ca + Cb;
            var bp = a.Select(ai => R * ai + T).ToArray();
            error = bp.Zip(b, (bpi, bi) => (bpi - bi).DotProduct(bpi - bi)).Sum();
            error = (float)Math.Sqrt(error) / (float)bp.Length;
            var result = DenseMatrix.OfColumns(4, 4, new float[][] { 
                R.Column(0).Concat(new float[] { 0 }).ToArray(),
                R.Column(1).Concat(new float[] { 0 }).ToArray(),
                R.Column(2).Concat(new float[] { 0 }).ToArray(),
                T.Concat(new float[] { 1 }).ToArray(), });

            //var test = a.Select(ai => result * new DenseVector(ai.Concat(new float[] { 1 }).ToArray())).ToArray();
            //var err2 = test.Zip(b.Select(v => new DenseVector(v.Concat(new float[] { 1 }).ToArray())), (bpi, bi) => (bpi - bi).DotProduct(bpi - bi)).Sum();
            
            return result;
        }


        public static Vector<float> FindCentroid(Vector<float>[] set)
        {
            return set.Aggregate((v1, v2) => v1 + v2) / set.Length;
        }
    }
}
