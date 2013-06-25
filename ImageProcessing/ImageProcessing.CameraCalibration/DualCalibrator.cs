using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Dynamight.ImageProcessing.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV.Structure;
using Emgu.CV;
using Graphics;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    public class DualCalibrator
    {
        public static void DrawNoFull(Projector proj, Camera camera, out Bitmap result)
        {
            var range = new int[10];
            proj.DrawBackground(Color.Black);
            var nolight = range.Select(r => new Image<Gray, byte>(camera.TakePicture())).Last();
            proj.DrawBackground(Color.Black);
            var fulllight = range.Select(r => new Image<Gray, byte>(camera.TakePicture())).Last();
            result = (fulllight - nolight).Bitmap;
        }

        public static BitmapWindow DebugWindow { get; set; }

        public static void Test(Projector projector, Camera camera, BitmapWindow output)
        {
            projector.DrawBackground(Color.Black);
            PointF[] cameraCorners;
            Bitmap nolight, withCorners;
            do
            {
                nolight = camera.TakePicture();
                withCorners = camera.TakePicture();
                cameraCorners = DetectCornersRB(nolight, new Size(7, 4)).Take(7).ToArray();
            } while (cameraCorners == null);
            QuickDraw.Start(withCorners)
                .Color(Color.White)
                .DrawPoint(cameraCorners, 5)
                .Finish();
            output.DrawBitmap(withCorners);
            //var v = BinarySL(projector, camera, cameraCorners, nolight, false);
            //var hor = BinarySL(projector, camera, cameraCorners, nolight, true);

            projector.DrawBackground(Color.White);
            var full = camera.TakePicture();
            output.DrawBitmap(full);
            //var t = BinarySL(projector, camera, cameraCorners, nolight, true);
            var t = new double[] { 0.1, 0.3, 0.5, 0.7, 0.9 };
            var ys = t.Select(row => projector.bitmap.Height / 2 + (row - 0.5) * projector.bitmap.Height / 4);
            cameraCorners = ys.Select(y => new PointF(250f, (float)y)).ToArray();
            var projected = BinarySL(projector, camera, cameraCorners, nolight);
            projector.DrawPoints(projected.Select(row => new PointF(row.X, row.Y)).ToArray(), 5);
            Func<Emgu.CV.Image<Gray, byte>> differ = () =>
                {
                    var pic = camera.TakePicture();
                    var diff = (new Image<Gray, byte>(pic) - new Image<Gray, byte>(nolight));
                    return diff;
                };

            int[] horizontal = new int[cameraCorners.Length];
            for (var step = 1; step <= Math.Log(1024, 2) - 3; step++)
            {
                projector.DrawBinary(step, true, Color.White);
                var steppic = differ();
                int idx = 0;
                foreach (var corner in cameraCorners)
                {
                    var hit = steppic[(int)corner.Y, (int)corner.X].Intensity > 0;
                    var h = horizontal[idx];
                    h = h << 1;
                    h = h | (hit ? 1 : 0);
                    horizontal[idx] = h;
                    withCorners = steppic.Bitmap;
                    QuickDraw.Start(withCorners)
                        .Color(hit ? Color.Gray : Color.White)
                        .DrawPoint(corner.X, corner.Y, 5)
                        .Finish();
                    idx++;
                }
                output.DrawBitmap(withCorners);
            }
            horizontal = horizontal.Select(row => row << 3).ToArray();
            using (var bitmap = new Bitmap(projector.bitmap.Width, projector.bitmap.Height))
            {
                using (var fast = new FastBitmap(bitmap))
                {
                    for (var x = 0; x < bitmap.Width; x++)
                        for (var y = 0; y < bitmap.Height; y++)
                            if (horizontal.Contains(x))
                                fast[x, y] = Color.FromArgb(255, 255, 255, 255);
                            else
                                fast[x, y] = Color.FromArgb(255, 0,0,0);
                    
                }
                projector.DrawBitmap(bitmap);
            }
        }

        public static Point[] BinarySL(Projector projector, Camera camera, PointF[] corners, Bitmap nolight)
        {
            return BinarySL(projector, camera, corners, nolight, false)
                .Zip(BinarySL(projector, camera, corners, nolight, true), (y,x) => new Point(x, y))
                .ToArray();
        }

        public static int[] BinarySL(Projector projector, Camera camera, PointF[] corners, Bitmap nolight, bool vertical)
        {
            Func<Emgu.CV.Image<Gray, byte>> differ = () =>
            {
                var pic = camera.TakePicture();
                var diff = (new Image<Gray, byte>(pic) - new Image<Gray, byte>(nolight));
                return diff;
            };

            int[] horizontal = new int[corners.Length];
            int max = (int)Math.Floor(Math.Log((vertical ? projector.bitmap.Width : projector.bitmap.Height), 2));
            for (var step = 1; step <= 7; step++)
            {
                projector.DrawBinary(step, vertical, Color.White);
                var steppic = differ();
                int idx = 0;
                Bitmap withCorners = null;
                foreach (var corner in corners)
                {
                    var hit = steppic[(int)(corner.Y), (int)corner.X].Intensity > 0;
                    var h = horizontal[idx];
                    h = h << 1;
                    h = h | (hit ? 1 : 0);
                    horizontal[idx] = h;
                    idx++;
                    withCorners = steppic.Bitmap;
                    QuickDraw.Start(withCorners)
                        .Color(hit ? Color.Gray : Color.White)
                        .DrawPoint(corner.X, corner.Y, 5)
                        .Finish();
                }
                DebugWindow.DrawBitmap(withCorners);
            }

            horizontal = horizontal.Select(row => row << (3)).ToArray();
            using (var bitmap = new Bitmap(projector.bitmap.Width, projector.bitmap.Height))
            {
                using (var fast = new FastBitmap(bitmap))
                {
                    for (var x = 0; x < bitmap.Width; x++)
                        for (var y = 0; y < bitmap.Height; y++)
                            if (horizontal.Contains(vertical ? x : y))
                                fast[x, y] = Color.FromArgb(255, 255, 255, 255);
                            else
                                fast[x, y] = Color.FromArgb(255, 0, 0, 0);

                }
                projector.DrawBitmap(bitmap);
            }
            return horizontal;
        }

        public static bool DrawCorners(Projector projector, Camera camera, out Bitmap nolight)
        {
            projector.DrawBackground(Color.Black);
            PointF[] cameraCorners;
            do
            {
                nolight = camera.TakePicture();
                //cameraCorners = DetectCorners(nolight, new Size(7, 4));
                cameraCorners = Emgu.CV.CameraCalibration.FindChessboardCorners(new Image<Gray, byte>(nolight), new Size(7, 4), Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
            } while (cameraCorners == null);
            
            var projCorners = DetectRoughCorners(cameraCorners, projector, camera);
            projector.DrawPoints(projCorners, 10);
            return true;
        }

        public static CalibrationResult Calibrate(Projector projector, Camera camera, Emgu.CV.Structure.MCvPoint3D32f[] globalCorners, int iterations = 10, int iterationTimeout = 500)
        {
            //Red and blue checkerboard
            //For each orientation of checkerboard:
           
            var datas = new CalibrationData[iterations];
            for (int i = 0; i < iterations; i++)
            {
                //Take pic of checkerboard with no proj light
                projector.DrawBackground(Color.Black);
                Bitmap nolight = camera.TakePicture();
                //Detect corners in camera space
                PointF[] cameraCorners = DetectCornersRB(nolight, new Size(7,4));
                //Determine rough checkerboard coordinates in projector space with graycode or binary structured light
                var projOutline = DetectRoughCorners(cameraCorners, projector, camera);
                //Determine corners in projector space
                var projectorCorners = DetectProjectorCorners(nolight, cameraCorners, projOutline, projector, camera);
                //Save corners in camera and projector space and store along side global coordinates that matches current checkerboard
                var data = new CalibrationData() { CameraCorners = cameraCorners, ProjectorCorners = projectorCorners, GlobalCorners = globalCorners };
                
                datas[i] = data;
                Thread.Sleep(iterationTimeout);
            }
            return CalibrationResult.Make(datas);
        }

        public static PointF[] DetectCornersBW(Bitmap picture, Size patternSize)
        {
            var image = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(picture);
            var corners = Emgu.CV.CameraCalibration.FindChessboardCorners(image, patternSize, Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
            if (corners == null)
                return null;
            var cc = new PointF[][] { corners };
            image.FindCornerSubPix(cc, new System.Drawing.Size(11, 11), new System.Drawing.Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(30, 0.1));
            return corners;
        }

        public static PointF[] DetectCornersRB(Bitmap picture, Size patternSize)
        {
            var image = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(picture);
            Emgu.CV.Image<Emgu.CV.Structure.Gray, byte> grayimage = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(new byte[image.Height, image.Width, 1]);
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var r = image[y, x].Red;
                    var b = image[y, x].Blue;
                    var g = image[y, x].Green;
                    var rd = Distance(new double[] { r, b, g }, new double[] { 255, 0, 0 });
                    if (rd < 175)
                        grayimage[y, x] = new Emgu.CV.Structure.Gray(0);
                    else
                        grayimage[y, x] = new Emgu.CV.Structure.Gray(255);
                }
            }
            DebugWindow.DrawBitmap(grayimage.Erode(2).Dilate(2).Bitmap);
            var corners = Emgu.CV.CameraCalibration.FindChessboardCorners(grayimage, patternSize, Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
            if (corners == null)
                return null;
            var cc = new PointF[][] { corners };
            grayimage.FindCornerSubPix(cc, new System.Drawing.Size(11, 11), new System.Drawing.Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(30, 0.1));
            return corners;
        }

        public static double Distance(double[] a, double[] b)
        {
            return Math.Sqrt(Math.Pow((a[0] - b[0]), 2) +
                Math.Pow((a[1] - b[1]), 2) +
                Math.Pow((a[2] - b[2]), 2));
        }

        public static PointF[] DetectRoughCorners(PointF[] cornersToInclude, Projector projector, Camera camera)
        {
            Func<Bitmap> takePic = () =>
                {
                    var bits = camera.TakePicture();
                    DebugWindow.DrawBitmap(bits);
                    return bits;
                };
            projector.DrawBackground(Color.Black);
            var nolight = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
            projector.DrawBackground(Color.White);
            var fullLight = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(takePic());
            int linewidth = 20;
            List<double[]> rowIntensities = new List<double[]>();
            List<double[]> colIntensities = new List<double[]>();
            for (var steps = 0; steps < (double)projector.Size.Height / (double)linewidth; steps++)
            {
                projector.DrawScanLine(steps, linewidth, true);
                var pic = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
                rowIntensities.Add(cornersToInclude.Select(corner => pic[(int)corner.X, (int)corner.Y].Intensity - nolight[(int)corner.X, (int)corner.Y].Intensity).ToArray());
            }
            for (var steps = 0; steps < (double)projector.Size.Width / (double)linewidth; steps++)
            {
                projector.DrawScanLine(steps, linewidth, false);
                var pic = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
                colIntensities.Add(cornersToInclude.Select(corner => pic[(int)corner.X, (int)corner.Y].Intensity).ToArray());
            }
            PointF[] result = new PointF[cornersToInclude.Length];
            var test = rowIntensities.Select(row => row[0]).ToArray();
            var order = test.OrderByDescending(row => row).ToArray();
            var max = order.First();
            for (var i = 0; i < result.Length; i++)
            {
                int idx = 0;
                var x = rowIntensities.Select(row => new { intensity = row[i], index = idx++ }).OrderByDescending(row => row.intensity).Select(row => row.index).First() * linewidth;
                idx = 0;
                var y = colIntensities.Select(row => new { intensity = row[i], index = idx++ }).OrderByDescending(row => row.intensity).Select(row => row.index).First() * linewidth;
                result[i] = new PointF(x, y);
            }
            projector.DrawPoints(result, 5);
            return result;
        }

        public static PointF[] DetectProjectorCorners(Bitmap nolight, PointF[] cameraCorners, PointF[] outline, Projector projector, Camera camera)
        {
            //Take pic of full proj light
            //Take pics of each structured light iteration
            return new PointF[0];
        }
    }

    public struct CalibrationData
    {
        public PointF[] CameraCorners;
        public PointF[] ProjectorCorners;
        public Emgu.CV.Structure.MCvPoint3D32f[] GlobalCorners;
    }

    public class CalibrationResult
    {
        private CalibrationResult() { }
        public Func<Emgu.CV.Structure.MCvPoint3D32f, PointF> TransformToCamera { get; private set; }
        public Func<Emgu.CV.Structure.MCvPoint3D32f, PointF> TransformToProjector { get; private set; }
        public static CalibrationResult Make(CalibrationData[] data)
        {
            return new CalibrationResult()
            {
                TransformToCamera = BuildTransform(),
                TransformToProjector = BuildTransform()
            };
        }

        private static Func<Emgu.CV.Structure.MCvPoint3D32f, PointF> BuildTransform()
        {
            return (p) => new PointF();
        }
    }
}
