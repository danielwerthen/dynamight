using Emgu.CV;
using Emgu.CV.Structure;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    public class ThreePointCalibration
    {
        public static ExtrinsicCameraParameters Calibrate(PointF[] locals, MCvPoint3D32f[] globals, Size pattern)
        {
            PointF[] pl = new PointF[]
            {
                locals[0 + (pattern.Height -1) * pattern.Width],
                locals[0 + 0 * pattern.Width],
                locals[(pattern.Width-1) + (pattern.Height -1) * pattern.Width],
            };

            MCvPoint3D32f[] pg = new MCvPoint3D32f[]
            {
                globals[0 + (pattern.Height -1) * pattern.Width],
                globals[0 + 0 * pattern.Width],
                globals[(pattern.Width-1) + (pattern.Height -1) * pattern.Width],
            };


            //f2 = (p1 - p0).Normalize(1);
            //f1 = (p2 - p0).Normalize(1);
            //f1 = (f1 - (f1.DotProduct(f2) * f2)).Normalize(1);
            //f3 = new DenseVector(new double[] { f1[1] * f2[2] - f1[2] * f2[1], f1[2] * f2[0] - f1[0] * f2[2], f1[0] * f2[1] - f1[1] * f2[0] });
            //f3 = f3.Normalize(1);

            var plv = pl.Select(row => new DenseVector(new double[] { row.X, row.Y })).ToArray();

            return null;

        }
    }
}
