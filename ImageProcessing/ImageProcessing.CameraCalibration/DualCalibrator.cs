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
            proj.DrawBackground(System.Windows.Media.Colors.Black);
            var nolight = range.Select(r => new Image<Gray, byte>(camera.TakePicture())).Last();
            proj.DrawBackground(System.Windows.Media.Colors.White);
            var fulllight = range.Select(r => new Image<Gray, byte>(camera.TakePicture())).Last();
            result = (fulllight - nolight).Bitmap;
        }

        public static bool DrawCorners(Projector projector, Camera camera, BitmapWindow display, out Bitmap nolight)
        {
            projector.DrawBackground(System.Windows.Media.Colors.Black);
            PointF[] cameraCorners;
            do
            {
                Thread.Sleep(500);
                nolight = camera.TakePicture();
                cameraCorners = DetectCorners(nolight, new Size(7, 4));
            } while (cameraCorners == null);
            int pointSize = 5;
            if (cameraCorners == null)
            {
                var image = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(nolight);
                Emgu.CV.Image<Emgu.CV.Structure.Gray, byte> grayimage = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(new byte[image.Height, image.Width, 1]);
                for (var y = 0; y < image.Height; y++)
                {
                    for (var x = 0; x < image.Width; x++)
                    {
                        var r = image[y, x].Red;
                        var b = image[y, x].Blue;
                        if (r > b)
                            grayimage[y, x] = new Emgu.CV.Structure.Gray(255);
                        else if (b > 0)
                            grayimage[y, x] = new Emgu.CV.Structure.Gray(0);
                        else
                            grayimage[y, x] = new Emgu.CV.Structure.Gray(50);
                    }
                }
                nolight = grayimage.Bitmap;
                return false;
            }
            foreach (var corner in cameraCorners)
            {
                for (var y = -pointSize; y <= pointSize; y++)
                {
                    for (var x = -pointSize; x <= pointSize; x++)
                        nolight.SetPixel(x + (int)corner.X, y + (int)corner.Y, Color.Red);
                }
            }
            var projCorners = DetectRoughCorners(cameraCorners, projector, camera, display);
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
                projector.DrawBackground(System.Windows.Media.Colors.Black);
                Bitmap nolight = camera.TakePicture();
                //Detect corners in camera space
                PointF[] cameraCorners = DetectCorners(nolight, new Size(7,4));
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

        public static PointF[] DetectCorners(Bitmap picture, Size patternSize)
        {
            var image = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(picture);
            Emgu.CV.Image<Emgu.CV.Structure.Gray, byte> grayimage = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(new byte[image.Height, image.Width, 1]);
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var r = image[y, x].Red;
                    var b = image[y, x].Blue;
                    if (r > b)
                        grayimage[y, x] = new Emgu.CV.Structure.Gray(255);
                    else if (b > 0)
                        grayimage[y, x] = new Emgu.CV.Structure.Gray(0);
                    else
                        grayimage[y, x] = new Emgu.CV.Structure.Gray(50);
                }
            }
            var corners = Emgu.CV.CameraCalibration.FindChessboardCorners(grayimage, patternSize, Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
            if (corners == null)
                return null;
            var cc = new PointF[][] { corners };
            grayimage.FindCornerSubPix(cc, new System.Drawing.Size(11, 11), new System.Drawing.Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(30, 0.1));
            return corners;
        }

        public static PointF[] DetectRoughCorners(PointF[] cornersToInclude, Projector projector, Camera camera, BitmapWindow display = null)
        {
            Func<Bitmap> takePic = () =>
                {
                    var bits = camera.TakePicture();
                    if (display != null)
                    {
                        display.LoadBitmap(bits);
                        display.RenderFrame();
                    }
                    return bits;
                };
            projector.DrawBackground(System.Windows.Media.Colors.Black);
            var nolight = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
            projector.DrawBackground(System.Windows.Media.Colors.White);
            var fullLight = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(takePic());
            int linewidth = 20;
            List<double[]> rowIntensities = new List<double[]>();
            List<double[]> colIntensities = new List<double[]>();
            for (var steps = 0; steps < (double)projector.Size.Height / (double)linewidth; steps++)
            {
                projector.DrawScanLine(steps, linewidth, true);
                var pic = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
                pic = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
                pic = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
                rowIntensities.Add(cornersToInclude.Select(corner => pic[(int)corner.X, (int)corner.Y].Intensity - nolight[(int)corner.X, (int)corner.Y].Intensity).ToArray());
            }
            for (var steps = 0; steps < (double)projector.Size.Width / (double)linewidth; steps++)
            {
                projector.DrawScanLine(steps, linewidth, false);
                var pic = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
                pic = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
                pic = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(takePic());
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
