using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CV = Emgu.CV;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    public static class TransformExhaust
    {
        public static double[] _FindMin(KinectSensor sensor, SkeletonPoint[] points, KinectCalibrator calibrator, CalibrationResult calib, double[] center, double[] min, double[] max, double[] inc, out double ferr)
        {
            double[][] iterationValues = new double[][] {
            Range.OfDoubles(max[0], min[0], inc[0]).ToArray(),
            Range.OfDoubles(max[1], min[1], inc[1]).ToArray(),
            Range.OfDoubles(max[2], min[2], inc[2]).ToArray(),
            Range.OfDoubles(max[3], min[3], inc[3]).ToArray(),
            Range.OfDoubles(max[4], min[4], inc[4]).ToArray(),
            Range.OfDoubles(max[5], min[5], inc[5]).ToArray(),
            };
            Func<double[], Matrix<Single>> makeIR2RGB = (values) =>
            {

                var ir2rgbExtrin = new CV.ExtrinsicCameraParameters(
                  new CV.RotationVector3D(new double[] { values[0] * Math.PI / 180.0, values[1] * Math.PI / 180.0, values[2] * Math.PI / 180.0 }),
                  new CV.Matrix<double>(new double[,] { { values[3] }, { values[4] }, { values[5] } })).ExtrinsicMatrix;

                return DenseMatrix.OfArray(new float[,] 
                {
                    { (float)ir2rgbExtrin.Data[0,0], (float)ir2rgbExtrin.Data[0,1], (float)ir2rgbExtrin.Data[0,2], (float)ir2rgbExtrin.Data[0,3] },
                    { (float)ir2rgbExtrin.Data[1,0], (float)ir2rgbExtrin.Data[1,1], (float)ir2rgbExtrin.Data[1,2], (float)ir2rgbExtrin.Data[1,3] },
                    { (float)ir2rgbExtrin.Data[2,0], (float)ir2rgbExtrin.Data[2,1], (float)ir2rgbExtrin.Data[2,2], (float)ir2rgbExtrin.Data[2,3] },
                    {0,0,0,1}
                });
            };
            Func<double[], double> findError = (vals) =>
            {
                var IR2RBG = makeIR2RGB(vals);
                var transformed = points.Select(p => new DenseVector(new float[] { p.X, p.Y, p.Z, 1 }))
                    .Select(v => IR2RBG.Multiply(v))
                    .Select(v => calibrator.K2G.Multiply(v))
                    .Select(v => v.ToArray()).ToArray();
                var back = calib.Transform(transformed);
                var compareWith = points.Select(p => sensor.CoordinateMapper.MapSkeletonPointToColorPoint(p, ColorImageFormat.RgbResolution1280x960Fps12))
                    .Select(cp => new System.Drawing.PointF(cp.X, cp.Y)).ToArray();
                return back.Zip(compareWith, (p1, p2) => Math.Sqrt((double)((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y)))).Average();
            };
            var all = CartesianProduct(iterationValues).ToArray();
            var final = all.Select(vals => vals.ToArray()).Select(vals => new { err = findError(vals), vals = vals }).OrderBy(e => e.err).First();
            ferr = final.err;
            return final.vals;
            //var c = 0;
            //var ids = iterationValues.Select(_ => c++);
            //var final = ids.Select(i => iterationValues[i].Select(val => new { err = findError(GetSparse(val, i).ToArray()), val = val }).OrderBy(e => e.err).Select(e => e.val).First()).ToArray();
            //ferr = findError(final);
            //return final;
        }

        public static double[] __FindMin(KinectSensor sensor, SkeletonPoint[] points, KinectCalibrator calibrator, CalibrationResult calib, double[] center, out double err)
        {
            double intial = 5;
            double[] max = new double[] { intial, intial, intial, intial, intial, intial };
            double[] min = new double[] { -intial, -intial, -intial, -intial, -intial, -intial };
            max = center.Zip(max, (c, m) => c + m / 5.0).ToArray();
            min = center.Zip(min, (c, m) => c + m / 5.0).ToArray();
            var inc = max.Zip(min, (ma, mi) => (double)(ma - mi) / 4.0).ToArray();
            err = 1000;
            for (var i = 1; i < 5; i++)
            {
                center = _FindMin(sensor, points, calibrator, calib, center, min, max, inc, out err);
                max = center.Zip(max, (c, m) => c + m / 5.0).ToArray();
                min = center.Zip(min, (c, m) => c + m / 5.0).ToArray();
                inc = max.Zip(min, (ma, mi) => (double)(ma - mi) / 4.0).ToArray();
            }
            return center;
        }

        public static double[] FindMin(KinectSensor sensor, SkeletonPoint[] points, KinectCalibrator calibrator, CalibrationResult calib)
        {
            var c1 = new double[6];
            double err;
            Random r = new Random();
            while (true)
            {
                var center = c1.Select(_ => r.NextDouble() * 4 - 2).ToArray();
                center = __FindMin(sensor, points, calibrator, calib, center, out err);
            }
        }

        private static IEnumerable<double> GetSparse(double val, int index, double def = 0, int count = 6)
        {
            for (var i = 0; i < count; i++)
            {
                if (i == index)
                    yield return val;
                yield return def;
            }
        }

        private static IEnumerable<int[]> GetRange(double[][] iterationValues, int[] currentPtr, int[][] directions, int subdivs)
        {
            foreach (var dir in directions)
            {
                int c = 0;
                var ids = dir.Select(_ => c++).ToArray();

                var variations = ids.Select(i => dir.Select(d =>
                {
                    if (dir[i] != 0)
                    {
                        var vals = iterationValues[i];
                        var idx = currentPtr[i] + (int)Math.Round(vals.Length / 2.0, 0);
                        var range = Math.Min((vals.Length - 1) - idx, idx);
                        return Range.OfDoubles(subdivs, 1).Select(r => (int)(r * (range / (double)subdivs))).ToArray();   
                    }
                    return new int[0];
                }).First()).ToArray();

                foreach (var var in CartesianProduct(variations))
                    yield return var.ToArray();

            }
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
              emptyProduct,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item }));
        }
    }
}
