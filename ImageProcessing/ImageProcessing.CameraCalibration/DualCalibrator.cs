using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Dynamight.ImageProcessing.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    public class DualCalibrator
    {
        public static bool DrawCorners(Projector projector, Camera camera, out Bitmap nolight)
        {
            projector.DrawBackground(System.Windows.Media.Colors.Black);
            nolight = camera.TakePicture();
            PointF[] cameraCorners = DetectCorners(nolight, new Size(7,4));
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
            var projCorners = DetectRoughCorners(cameraCorners, projector, camera);
            projector.DrawPoints(projCorners, 5);
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

        public static PointF[] DetectRoughCorners(PointF[] cornersToInclude, Projector projector, Camera camera)
        {
            int linewidth = 10;
            List<Bitmap> maps = new List<Bitmap>();
            List<Bitmap> mapsv = new List<Bitmap>();
            for (var steps = 0; steps < (double)projector.Size.Height / (double)linewidth; steps++)
            {
                projector.DrawScanLine(steps, linewidth, true);
                maps.Add(camera.TakePicture());
            }
            for (var steps = 0; steps < (double)projector.Size.Width / (double)linewidth; steps++)
            {
                projector.DrawScanLine(steps, linewidth, false);
                mapsv.Add(camera.TakePicture());
            }
            var imgs = maps.Select(map => new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(map)).ToArray();
            var imgsv = mapsv.Select(map => new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(map)).ToArray();
            return cornersToInclude.Select(corner =>
                {
                    int idx = 0;
                    var maxh = imgs.Select(img => new { index = idx++, intenisty = img[(int)corner.Y, (int)corner.X].Intensity }).OrderByDescending(row => row.intenisty).Select(row => row.index).First();
                    idx = 0;
                    var maxv = imgsv.Select(img => new { index = idx++, intenisty = img[(int)corner.Y, (int)corner.X].Intensity }).OrderByDescending(row => row.intenisty).Select(row => row.index).First();
                    return new PointF(maxh * linewidth, maxv * linewidth);
                }).ToArray();
            //return new PointF[] { new PointF(0, 0), new PointF(projector.Size.Width, 0), new PointF(projector.Size.Width, projector.Size.Height), new PointF(0, projector.Size.Height) };
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
