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
    public class KinectCalibrator
    {
        CalibrationResult calib;
        KinectSensor sensor;
        Matrix<float> IR2RGB;
        public Matrix<float> K2G;
        public KinectCalibrator(KinectSensor sensor, CalibrationResult calib)
        {
            this.calib = calib;
            this.sensor = sensor;

            var ir2rgbExtrin = new CV.ExtrinsicCameraParameters(
              new CV.RotationVector3D(new double[] { 0.56 * Math.PI / 180.0, 0.07 * Math.PI / 180.0, +0.05 * Math.PI / 180.0 }),
              new CV.Matrix<double>(new double[,] { { -0.0256 }, { 0.00034 }, { 0.00291 } })).ExtrinsicMatrix;

            IR2RGB = DenseMatrix.OfArray(new float[,] 
            {
                { (float)ir2rgbExtrin.Data[0,0], (float)ir2rgbExtrin.Data[0,1], (float)ir2rgbExtrin.Data[0,2], (float)ir2rgbExtrin.Data[0,3] },
                { (float)ir2rgbExtrin.Data[1,0], (float)ir2rgbExtrin.Data[1,1], (float)ir2rgbExtrin.Data[1,2], (float)ir2rgbExtrin.Data[1,3] },
                { (float)ir2rgbExtrin.Data[2,0], (float)ir2rgbExtrin.Data[2,1], (float)ir2rgbExtrin.Data[2,2], (float)ir2rgbExtrin.Data[2,3] },
                {0,0,0,1}
            });

            var rt = calib.Extrinsic.ExtrinsicMatrix;
            K2G = DenseMatrix.OfArray(new Single[,] 
            {
                { (float)rt.Data[0,0], (float)rt.Data[0,1], (float)rt.Data[0,2], (float)rt.Data[0,3] },
                { (float)rt.Data[1,0], (float)rt.Data[1,1], (float)rt.Data[1,2], (float)rt.Data[1,3] },
                { (float)rt.Data[2,0], (float)rt.Data[2,1], (float)rt.Data[2,2], (float)rt.Data[2,3] },
                {0,0,0,1}
            }).Inverse();
        }

        public float[] ToGlobal(SkeletonPoint point)
        {
            var colorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(point, ColorImageFormat.RgbResolution1280x960Fps12);
            var cpp = new System.Drawing.PointF(1280 - colorPoint.X, colorPoint.Y);
            var depth = new DenseVector(new float[] { point.X, point.Y, point.Z, 1 });
            var tdepth = K2G.Multiply(IR2RGB.Multiply(depth));
            var irt = calib.InverseExtrinsic(tdepth[2] / tdepth[3]);
            var it = calib.InverseTransform(cpp, irt);
            return it;
        }

        public float[] ToGlobal(Joint joint)
        {
            return ToGlobal(joint.Position);
        }

        public IEnumerable<float[]> ToGlobal(IEnumerable<SkeletonPoint> points)
        {
            foreach (var p in points)
                yield return ToGlobal(p);
        }
    }
}
