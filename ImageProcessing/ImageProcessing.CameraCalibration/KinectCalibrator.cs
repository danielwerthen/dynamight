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
        Matrix<float> IR2RGB;
        public Matrix<float> K2G;
        Matrix<float> EXTRA;

        public OpenTK.Matrix4 GetModelView()
        {
            var rt = EXTRA;
            return new OpenTK.Matrix4(
                rt[0, 0], rt[1, 0], rt[2, 0], rt[3, 0],
                rt[0, 1], rt[1, 1], rt[2, 1], rt[3, 1],
                rt[0, 2], rt[1, 2], rt[2, 2], rt[3, 2],
                rt[0, 3], rt[1, 3], rt[2, 3], rt[3, 3]);
        }

        public KinectCalibrator(CalibrationResult calib)
        {
            this.calib = calib;

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

            var flip = DenseMatrix.OfArray(new Single[,] 
            {
                { -1,0,0,0 },
                { 0,-1,0,0 },
                { 0,0,1,0 },
                { 0,0,0,1 }
            });
            EXTRA = K2G * IR2RGB * flip;
        }

        public float[] ToGlobal(KinectSensor sensor, SkeletonPoint point, float offset = 0)
        {
            var cp = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(point, ColorImageFormat.RgbResolution1280x960Fps12);
            var iext = calib.InverseExtrinsic(point.Z + offset);
            return calib.InverseTransform(new System.Drawing.PointF(1280 - cp.X, cp.Y), iext);
        }

        public float[] ToGlobal(SkeletonPoint point)
        {
            
            var depth = new DenseVector(new float[] { point.X, point.Y, point.Z, 1 });
            //var tdepth = K2G.Multiply(IR2RGB.Multiply(IR2RGB.Multiply(depth)));
            var tdepth = EXTRA.Multiply(depth);
            return tdepth.ToArray();
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
