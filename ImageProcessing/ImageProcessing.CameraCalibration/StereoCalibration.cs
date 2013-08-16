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
using Microsoft.Kinect;
using Emgu.CV.CvEnum;

namespace Dynamight.ImageProcessing.CameraCalibration
{
    public class Range
    {
        public static IEnumerable<int> OfInts(int stop, int start = 0, int step = 1)
        {
            for (var i = start; i < stop; i += step)
                yield return i;
        }

        public static IEnumerable<double> OfDoubles(double stop, double start = 0, double step = 1)
        {
            for (var i = start; i < stop; i += step)
                yield return i;
        }
    }

    public class StereoCalibration
    {
        public static Func<PointF[], PointF[]> FindHomography(PointF[] ccs, PointF[] pcs)
        {
            var hg = Emgu.CV.CameraCalibration.FindHomography(ccs, pcs, HOMOGRAPHY_METHOD.DEFAULT, 2);
            var t = ccs.Select(c => new PointF(c.X, c.Y)).ToArray();
            hg.ProjectPoints(t);
            var tes = pcs.Zip(t, (a, b) => (b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y)).ToArray();
            var sum = tes.Sum();
            return (ps) =>
            {
                var psc = ps.Select(p => new PointF(p.X, p.Y)).ToArray();
                hg.ProjectPoints(psc);
                return psc;
            };
            
        }

        public static PointF[] Undistort(CalibrationResult calib, PointF[] points)
        {
            return calib.Intrinsic.Undistort(points, null, null);
        }

        public static BitmapWindow DebugWindow { get; set; }

        public static void Test(Projector projector, Camera camera)
        {
            if (DebugWindow != null)
                for (var i = 1; i < 0; i++)
                {
                    projector.DrawBackground(Color.Black);
                    var nolight = camera.TakePicture(10);
                    projector.DrawBinary(i, false, Color.Green);
                    var fulllight = camera.TakePicture(3);
                    var n = new Image<Bgr, byte>(nolight);
                    var f = new Image<Bgr, byte>(fulllight);
                    Point p = new Point();
                    var d = (f - n).Split()[1];
                    DebugWindow.DrawBitmap(d.Bitmap);
                    double[] min, max;
                    Point[] minp, maxp;
                    d.MinMax(out min, out max, out minp, out maxp);
                    var thresh = (max.Max() - min.Min()) * 0.08 + min.Min();
                    d = d.ThresholdBinary(new Gray(thresh), new Gray(255)).Erode(2).Dilate(3).Erode(1);
                    DebugWindow.DrawBitmap(d.Bitmap);
                }
        }

        public static MCvPoint3D32f[] GenerateCheckerBoard(Size pattern, float checkerBoardSize, float z = 0f)
        {
            var globalPoints = new MCvPoint3D32f[pattern.Width * pattern.Height];
            for (var y = 0; y < pattern.Height; y++)
            {
                for (var x = 0; x < pattern.Width; x++)
                {
                    globalPoints[x + y * pattern.Width] = new Emgu.CV.Structure.MCvPoint3D32f((float)(x * checkerBoardSize), -(float)(y * checkerBoardSize), z);
                }
            }
            return globalPoints;
        }

        public static bool Different(PointF[] a, PointF[] b, float thresh = 400)
        {
            if (b == null || a == null)
                return true;
            var diff = a.Zip(b, (ap, bp) => Math.Abs(ap.X - bp.X) + Math.Abs(ap.Y - bp.Y)).Sum();
            return diff > thresh;
        }

        public static CalibrationResult CalibrateCamera(PointF[] cameraCorners, Size pattern, float checkerBoardSize, CalibrationResult useIntrinsic)
        {
            var globals = GenerateCheckerBoard(pattern, checkerBoardSize, 0);
            var globalCorners = new MCvPoint3D32f[][] { globals };
            var intrinsic = useIntrinsic.Intrinsic;

            var extrinsic = Emgu.CV.CameraCalibration.FindExtrinsicCameraParams2(globals, cameraCorners, intrinsic);
            return new CalibrationResult() { Intrinsic = intrinsic, Extrinsic = extrinsic };
        }

        public static CalibrationResult CalibrateCamera(PointF[][] cameraCorners, Camera camera, Size pattern, float checkerBoardSize)
        {
            return CalibrateCamera(cameraCorners, camera.Size, pattern, checkerBoardSize);
        }

        public static CalibrationResult CalibrateCamera(PointF[][] cameraCorners, Size cameraSize, Size pattern, float checkerBoardSize)
        {
            var globals = GenerateCheckerBoard(pattern, checkerBoardSize, 0);
            var globalCorners = cameraCorners.Select(row => globals).ToArray();
            var intrinsic = new IntrinsicCameraParameters();
            ExtrinsicCameraParameters[] cameraExtrinsicsArray;
            Emgu.CV.CameraCalibration.CalibrateCamera(globalCorners, cameraCorners, cameraSize, intrinsic, CALIB_TYPE.CV_CALIB_RATIONAL_MODEL, out cameraExtrinsicsArray);
            var extrinsic = cameraExtrinsicsArray.First();
            return new CalibrationResult() { Intrinsic = intrinsic, Extrinsic = extrinsic };
        }

        public static PointF[] FindDualPlaneCorners(Camera camera, Size pattern)
        {
            var cpattern = new Size(pattern.Width - 1, pattern.Height - 1);
            var img = camera.TakePicture(3);
            return GetCameraCorners(img, cpattern, false);
        }

        public static CalibrationResult CalibrateProjector(Projector projector, PointF[][] cacalibdata, PointF[][] cameraCorners, PointF[][] projectorCorners, Size cameraPattern, float checkerboardSize)
        {
            var hm = CreateHomography(cameraCorners, projectorCorners);
            var globals = GenerateCheckerBoard(cameraPattern, checkerboardSize, 0);
            var globalCorners = cacalibdata.Select(row => globals).ToArray();
            var proj = cacalibdata.Select(cs => cs.Select(c => new PointF(c.X, c.Y)).ToArray()).ToArray();
            foreach (var p in proj)
                hm.ProjectPoints(p);
            IntrinsicCameraParameters projIntrin = new IntrinsicCameraParameters();
            ExtrinsicCameraParameters[] projExtrins;
            Emgu.CV.CameraCalibration.CalibrateCamera(globalCorners, proj, projector.Size, projIntrin, Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_ZERO_TANGENT_DIST | CALIB_TYPE.CV_CALIB_FIX_K3, out projExtrins);

            return new CalibrationResult() { Intrinsic = projIntrin, Extrinsic = projExtrins.First() };
        }

        public static CalibrationResult CalibrateProjector(Projector projector, Camera camera, Size pattern, CalibrationResult cameraCalib, PointF[][] cacalibdata, Size cameraPattern, float checkerboardSize)
        {
            List<PointF[]> cameraCorners = new List<PointF[]>();
            List<PointF[]> projectorCorners = new List<PointF[]>();
            var cpattern = new Size(pattern.Width - 1, pattern.Height - 1);
            int steps = 15;
            double rotx = 0, roty = 0, rotz = 0;
            for (int i = 0; i < steps; i++)
            {
                var di = (double)i / (double)steps;
                rotx = 0;
                roty = Math.Sin(di * Math.PI / 2) * 0.8;
                rotz = Math.Sin(di * Math.PI / 2) * 0.6;
                var pcs = projector.DrawCheckerboard(pattern, 
                    rotx,
                    roty,
                    rotz, 0.5);
                var img = camera.TakePicture(3);
                var ccs = GetCameraCorners(img, cpattern, false);
                if (ccs != null)
                {

                    projectorCorners.Add(pcs);
                    cameraCorners.Add(ccs);
                    var withCorners = camera.TakePicture(0);
                    if (DebugWindow != null)
                    {
                        QuickDraw.Start(withCorners)
                            .Color(Color.White)
                            .DrawPoint(ccs, 5)
                            .Finish();
                        DebugWindow.DrawBitmap(withCorners);
                    }
                }
            }
            //for (int i = 0; i < steps; i++)
            //{
            //    var di = (double)i / (double)steps;
            //    rotx += 0.04;
            //    roty *= 0.90;
            //    var pcs = projector.DrawCheckerboard(pattern,
            //        rotx,
            //        roty,
            //        rotz, 0.7);
            //    var ccs = GetCameraCorners(camera, cpattern, false);
            //    if (ccs != null)
            //    {

            //        projectorCorners.Add(pcs);
            //        cameraCorners.Add(ccs);
            //        var withCorners = camera.TakePicture(0);
            //        if (DebugWindow != null)
            //        {
            //            QuickDraw.Start(withCorners)
            //                .Color(Color.White)
            //                .DrawPoint(ccs, 5)
            //                .Finish();
            //            DebugWindow.DrawBitmap(withCorners);
            //        }
            //    }
            //}

            var hm = CreateHomography(cameraCorners.ToArray(), projectorCorners.ToArray());
            var globals = GenerateCheckerBoard(cameraPattern, checkerboardSize, 0);
            var globalCorners = cacalibdata.Select(row => globals).ToArray();
            var proj = cacalibdata.Select(cs => cs.Select(c => new PointF(c.X, c.Y)).ToArray()).ToArray();
            foreach (var p in proj)
                hm.ProjectPoints(p);
            IntrinsicCameraParameters projIntrin = new IntrinsicCameraParameters();
            ExtrinsicCameraParameters[] projExtrins;
            Emgu.CV.CameraCalibration.CalibrateCamera(globalCorners, proj, projector.Size, projIntrin, Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_RATIONAL_MODEL, out projExtrins); 
            
            return new CalibrationResult() { Intrinsic = projIntrin, Extrinsic = projExtrins.First() };
        }

        public static HomographyMatrix CreateHomography(PointF[][] camera, PointF[][] projector)
        {
            return Emgu.CV.CameraCalibration.FindHomography(camera.SelectMany(c => c).ToArray(), projector.SelectMany(c => c).ToArray(),
                Emgu.CV.CvEnum.HOMOGRAPHY_METHOD.DEFAULT, 2);
        }

        public static PointF[] FlipX(PointF[] arr, Size size)
        {
            var res = new PointF[arr.Length];
            for (var y = 0; y < size.Height; y++)
                for (var x = 0; x < size.Width; x++)
                    res[(size.Width -1- x) + y * size.Width] = arr[x + y * size.Width];
            return res;
        }

        public static CalibrationData[] GatherData(Projector projector, Camera camera, Size pattern, int passes = 1, Func<int, bool> perPass = null)
        {
            var datas = new List<CalibrationData>();

            PointF[] corners;
            for (int i = 0; i < passes; i+= 0)
            {
                corners = GetCameraCorners(projector, camera, pattern);
                var data = GetLocalCorners(corners, projector, camera, pattern);
                if (perPass != null && perPass(i))
                {
                    datas.Add(data);
                    i++;
                }
            }
            return datas.ToArray();
        }

        public static PointF[] Properize(PointF[] points, Size pattern)
        {
            var res = new PointF[points.Length];
            var hs = new int[pattern.Height];
            for (var i = 0; i < pattern.Height; i++) hs[i] = i;
            var heighted = hs.Select(h => points.Skip(h * pattern.Width).Take(pattern.Width).OrderBy(p => p.X).ToArray())
                .OrderBy(r => r.OrderBy(p => p.X).Select(p => p.Y).First()).ToArray();
            for (var y = 0; y < pattern.Height; y++)
            {
                for (var x = 0; x < pattern.Width; x++)
                    res[x + y * pattern.Width] = heighted[y][x];
            }
            return res;
        }

        
        public static StereoCalibrationResult Calibrate(CalibrationData[] data, Camera camera, Projector projector, Size pattern, float checkerBoardSize)
        {
            var globals = GenerateCheckerBoard(pattern, checkerBoardSize);
            var datas = data;
            var globalCorners = datas.Select(row => globals).ToArray();
            var cameraCorners = datas.Select(row => Properize(row.CameraCorners, pattern)
                )
                .ToArray();
            var projectorCorners = datas.Select(row => Properize(row.ProjectorCorners, pattern)

                ).ToArray();

            //for (var i = 0; i < datas.Length; i++)
            //{
            //    var withCorners = camera.TakePicture(0);
            //    QuickDraw.Start(withCorners)
            //        .Color(Color.White)
            //        .DrawPoint(cameraCorners[i].Take(11).ToArray(), 5)
            //        .Finish();
            //    DebugWindow.DrawBitmap(withCorners);
            //    projector.DrawPoints(projectorCorners[i].Take(11).ToArray(), 4);
            //}

            IntrinsicCameraParameters cameraIntrinsics = new IntrinsicCameraParameters();
            cameraIntrinsics.IntrinsicMatrix = new Matrix<double>(new double[,] 
            { 
            { 4.884, 0, 0.032 }, 
            { 0, 4.884 * 3.0 / 4.0, -0.037 }, 
            { 0, 0, 1 } });
            cameraIntrinsics.DistortionCoeffs = new Matrix<double>(new double[,] {
                {-0.00575},
                {0.000442},
                {-0.000107},
                {0},
                {0},
                {0},
                {0},
                {0},
            });
            ExtrinsicCameraParameters[] cameraExtrinsicsArray;
            var cerr = Emgu.CV.CameraCalibration.CalibrateCamera(globalCorners, cameraCorners,
                camera.Size, cameraIntrinsics, Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_USE_INTRINSIC_GUESS,
                out cameraExtrinsicsArray);




            IntrinsicCameraParameters projectorIntrinsics = new IntrinsicCameraParameters();
            //projectorIntrinsics.IntrinsicMatrix = new Matrix<double>(new double[,] { { 531.15f * 4f / 3f, 0, 1 }, { 0, 531.15f, 1 }, { 0, 0, 1 } });
            projectorIntrinsics.IntrinsicMatrix = new Matrix<double>(new double[,] 
            {
                {2151.0712684548571, 0, projector.Size.Width / 2},
                {0, 1974.541465816948, projector.Size.Height / 2},
                {0,0,1}
            });

            ExtrinsicCameraParameters[] projectorExtrinsicsArray;
            var perr = Emgu.CV.CameraCalibration.CalibrateCamera(globalCorners, projectorCorners,
                projector.Size, projectorIntrinsics,
                Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_RATIONAL_MODEL,
                out projectorExtrinsicsArray);

            //Matrix<double> foundamental, essential;
            //ExtrinsicCameraParameters camera2projectorExtrinsics;
            //Emgu.CV.CameraCalibration.StereoCalibrate(globalCorners,
            //    cameraCorners,
            //    projectorCorners,
            //    cameraIntrinsics,
            //    projectorIntrinsics,
            //    camera.Size,
            //    Emgu.CV.CvEnum.CALIB_TYPE.DEFAULT,
            //    new MCvTermCriteria(16, 0.01),
            //    out camera2projectorExtrinsics,
            //    out foundamental,
            //    out essential);

            return new StereoCalibrationResult()
            {
                cameraIntrinsic = cameraIntrinsics,
                cameraExtrinsic = cameraExtrinsicsArray.First(),
                projectorIntrinsic = projectorIntrinsics,
                projectorExtrinsic = projectorExtrinsicsArray.First(),
                cameraToProjectorExtrinsic = null
            };
        }


        #region PhaseModulation

        public static PointF[] PhaseCalib(Projector projector, Camera camera, PointF[] cameraCorners, int steps = 7)
        {
            var color = Color.Green;
            projector.DrawBackground(Color.Black);
            var nolight = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
            
            projector.DrawBackground(color);
            var fulllight = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
            Func<int, bool, Image<Gray, byte>[]> takePics = (step, vertical) =>
            {
                Image<Gray, byte>[] pics = new Image<Gray, byte>[3];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, (float)(-2.0f * Math.PI / 3.0f), vertical, color);
                pics[0] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, 0f, vertical, color);
                pics[1] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, (float)(2.0f * Math.PI / 3.0f), vertical, color);
                pics[2] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                return pics;
            };
            Func<Image<Gray, byte>[], double[]> itop = (pics) =>
            {
                return cameraCorners.Select(c =>
                {
                    var mini = nolight[(int)c.Y, (int)c.X].Intensity;
                    var maxi = fulllight[(int)c.Y, (int)c.X].Intensity;
                    var i1 = pics[0][(int)c.Y, (int)c.X].Intensity;
                    i1 = (i1 - mini) / (i1 - maxi);
                    var i2 = pics[1][(int)c.Y, (int)c.X].Intensity;
                    i2 = (i2 - mini) / (i2 - maxi);
                    var i3 = pics[2][(int)c.Y, (int)c.X].Intensity;
                    i3 = (i3 - mini) / (i3 - maxi);
                    return PhaseModulation.IntensityToPhase(i1, i2, i3);
                }).ToArray();
            };

            int w = projector.Size.Width;
            int h = projector.Size.Height;
            int subdiv = 1;
            int[] xs = new int[cameraCorners.Length];
            int[] ys = new int[cameraCorners.Length];
            int[] ids = Range.OfInts(cameraCorners.Length).ToArray();
            for (int i = 0; i < steps; i++)
            {
                var xpics = takePics(subdiv, true);
                var xphs = itop(xpics);
                var xh = ids.Select(id => xphs[id] > 0.5).ToArray();

                var qd = QuickDraw.Start(xpics[1].Bitmap);
                var thrash = ids.Select(id =>
                {
                    qd.Color(xh[id] ? Color.White : Color.Gray);
                    qd.DrawPoint(cameraCorners[id].X, cameraCorners[id].Y, 5);
                    return id;
                }).ToArray();
                qd.Finish();
                DebugWindow.DrawBitmap(xpics[1].Bitmap);

                var ypics = takePics(subdiv, false);
                var yphs = itop(ypics);
                var yh = ids.Select(id => yphs[id] > 0.5).ToArray();

                qd = QuickDraw.Start(ypics[1].Bitmap);
                thrash = ids.Select(id =>
                {
                    qd.Color(yh[id] ? Color.White : Color.Gray);
                    qd.DrawPoint(cameraCorners[id].X, cameraCorners[id].Y, 5);
                    return id;
                }).ToArray();
                qd.Finish();
                DebugWindow.DrawBitmap(ypics[1].Bitmap);


                xs = ids.Select(id => (xs[id] << 1) | (xh[id] ? 1 : 0)).ToArray();
                ys = ids.Select(id => (ys[id] << 1) | (yh[id] ? 1 : 0)).ToArray();


                subdiv = subdiv << 1;
            }
            var fxs = ids.Select(id => ((double)xs[id] / (double)subdiv) * w).ToArray();
            var fys = ids.Select(id => ((double)ys[id] / (double)subdiv) * h).ToArray();
            return fxs.Zip(fys, (x,y) => new PointF((float)x,(float)y)).ToArray();
        }

        public static PointF[] PhineTune(Projector projector, Camera camera, PointF[] cameraCorners, PointF[] rough, int subdiv)
        {
            var color = Color.Green;
            projector.DrawBackground(Color.Black);
            var nolight = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
            projector.DrawBackground(color);
            var fulllight = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
            Func<int, bool, Image<Gray, byte>[]> takePics = (step, vertical) =>
            {
                Image<Gray, byte>[] pics = new Image<Gray, byte>[3];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, (float)(-2.0f * Math.PI / 3.0f), vertical, color);
                pics[0] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, 0f, vertical, color);
                pics[1] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, (float)(2.0f * Math.PI / 3.0f), vertical, color);
                pics[2] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                return pics;
            };
            Func<Image<Gray, byte>[], double[]> itop = (pics) =>
            {
                return cameraCorners.Select(c =>
                {
                    var mini = nolight[(int)c.Y, (int)c.X].Intensity;
                    var maxi = fulllight[(int)c.Y, (int)c.X].Intensity;
                    var i1 = pics[0][(int)c.Y, (int)c.X].Intensity;
                    i1 = (i1 - mini) / (maxi - mini);
                    var i2 = pics[1][(int)c.Y, (int)c.X].Intensity;
                    i2 = (i2 - mini) / (maxi - mini);
                    var i3 = pics[2][(int)c.Y, (int)c.X].Intensity;
                    i3 = (i3 - mini) / (maxi - mini);
                    return PhaseModulation.IntensityToPhase(i1, i2, i3);
                }).ToArray();
            };

            int w = projector.Size.Width;
            int h = projector.Size.Height;
            var xphs = itop(takePics(subdiv, true));
            var yphs = itop(takePics(subdiv, false));

            var ids = new int[cameraCorners.Length];
            int idx = 0;
            ids = ids.Select(i => idx++).ToArray();

            return ids.Select(i =>
                {
                    var v = rough[i];
                    var denom = (double)subdiv;
                    var xph = xphs[i] * (w / denom);
                    var yph = yphs[i] * (h / denom);
                    var phsx = Math.Floor(v.X / denom) * denom;
                    var phsy = Math.Floor(v.Y / denom) * denom;
                    double apx = v.X / w;
                    double phx = xphs[i];

                    apx = Math.Floor(apx / (1.0 / denom)) * (1.0 / denom) + (phx < 1 ? phx / denom : 0);
                    apx = Math.Round(apx, 5) * w;

                    double apy = v.Y / h;
                    double phy = yphs[i];

                    apy = Math.Floor(apy / (1.0 / denom)) * (1.0 / denom) + (phy < 1 ? phy / denom : 0);
                    apy = Math.Round(apy, 5) * h;

                    return new PointF((float)(apx), (float)(apy));

                }).ToArray();

        }

        public static PointF[] PhaseMod(Projector projector, Camera camera, PointF[] cameraCorners)
        {
            var color = Color.FromArgb(0, 255, 0);
            projector.DrawBackground(Color.Black);
            var nolight = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
            projector.DrawBackground(color);
            var fulllight = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
            Func<int, bool, Image<Gray, byte>[]> takePics = (step, vertical) =>
            {
                Image<Gray, byte>[] pics = new Image<Gray, byte>[3];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, (float)(-2.0f * Math.PI / 3.0f), vertical, color);
                pics[0] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, 0f, vertical, color);
                pics[1] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawPhaseMod(step, (float)(2.0f * Math.PI / 3.0f), vertical, color);
                pics[2] = new Image<Bgr, byte>(camera.TakePicture(2)).Split()[1];
                return pics;
            };

            Func<Image<Gray, byte>[], PointF[], double[]> itop = (pics, corners) =>
            {
                return corners.Select(c =>
                    {
                        var mini = nolight[(int)c.Y, (int)c.X].Intensity;
                        var maxi = fulllight[(int)c.Y, (int)c.X].Intensity;
                        var i1 = pics[0][(int)c.Y, (int)c.X].Intensity;
                        i1 = (i1 - mini) / (i1 - maxi);
                        var i2 = pics[1][(int)c.Y, (int)c.X].Intensity;
                        i2 = (i2 - mini) / (i2 - maxi);
                        var i3 = pics[2][(int)c.Y, (int)c.X].Intensity;
                        i3 = (i3 - mini) / (i3 - maxi);
                        return PhaseModulation.IntensityToPhase(i1, i2, i3);
                    }).ToArray();
            };
            var ids = new int[cameraCorners.Length];
            int idx = 0;
            ids = ids.Select(i => idx++).ToArray();
            int steps = 7;
            int[] stepa = new int[steps];
            idx = 1;
            stepa = stepa.Select(i => idx++).ToArray();
            int[] stepx = stepa.Select(row => (int)Math.Pow(2, row - 1)).ToArray();
            double[][] verticals = new double[steps][];
            for (var i = 1; i <= steps; i++)
            {
                var pics = takePics(stepx[i - 1], true);
                verticals[i - 1] = itop(pics, cameraCorners);
            }
            var apv = ids.Select(i => PhaseModulation.AbsolutePhase(
                stepa.Select(s => verticals[s - 1][i]).ToArray(), stepa))
                .Select(ph => ph * projector.Size.Width)
                .ToArray();

            double[][] horizontals = new double[steps][];
            for (var i = 1; i <= steps; i++)
            {
                var pics = takePics(stepx[i - 1], false);
                horizontals[i - 1] = itop(pics, cameraCorners);
            }
            var aph = ids.Select(i => PhaseModulation.AbsolutePhase(
                stepa.Select(s => horizontals[s - 1][i]).ToArray(), stepx))
                .Select(ph => ph * projector.Size.Height)
                .ToArray();
            return ids.Select(i => new PointF((float)apv[i], (float)aph[i])).ToArray();
        }

        #endregion

        public static PointF[] SortLRUB(PointF[] points)
        {
            return points.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
        }

        public static PointF[] GetCameraCorners(Bitmap image, Size pattern, bool RB = false)
        {
            PointF[] cameraCorners;
            if (RB)
                cameraCorners = DetectCornersRB(image, pattern);
            else
                cameraCorners = DetectCornersBW(image, pattern);
            return cameraCorners;
        }

        public static PointF[] GetCameraCorners(Projector projector, Camera camera, Size pattern, bool RB = true)
        {

            //Take pic of checkerboard with no proj light
            projector.DrawBackground(Color.Gray);
            //Detect corners in camera space
            PointF[] cameraCorners;
            Bitmap withCorners;
            withCorners = camera.TakePicture();
            var img = camera.TakePicture(2);
            cameraCorners = GetCameraCorners(img, pattern, RB);
            if (cameraCorners == null)
                return null;

            if (DebugWindow != null)
            {
                QuickDraw.Start(withCorners)
                    .Color(Color.White)
                    .DrawPoint(cameraCorners, 5)
                    .Finish();
                DebugWindow.DrawBitmap(withCorners);
            }
            return cameraCorners;
        }

        public static CalibrationData GetLocalCorners(PointF[] cameraCorners, Projector projector, Camera camera, Size pattern)
        {

            if (false)
            {
                projector.DrawBackground(Color.Black);
                var noi = new Image<Bgr, byte>(camera.TakePicture(5));
                projector.DrawBackground(Color.FromArgb(0, 255, 0));
                var fulli = new Image<Bgr, byte>(camera.TakePicture(5));
                projector.DrawPhaseMod(4, 0f, true, Color.FromArgb(0, 255, 0));
                var img = new Image<Bgr, byte>(camera.TakePicture(5));

                var max = (fulli - noi).Split()[1];
                var cur = (img - noi).Split()[1];
                var map = new Bitmap(cur.Width, cur.Height);
                DebugWindow.DrawBitmap(max.Bitmap);
                DebugWindow.DrawBitmap(cur.Bitmap);
                using (var fast = new FastBitmap(map))
                {
                    for (int y = 0; y < cur.Height; y++)
                    {
                        for (int x = 0; x < cur.Width; x++)
                        {
                            var ii = cur[(int)y, (int)x].Intensity / max[(int)y, (int)x].Intensity;
                            if (ii > 1)
                                ii = 1;
                            var i = (byte)(ii * 255);
                            fast[x, y] = Color.FromArgb(i, i, i);
                        }
                    }
                }
                if (DebugWindow != null)
                    DebugWindow.DrawBitmap(map);
            }


            //Determine rough checkerboard coordinates in projector space with graycode or binary structured light
                
            var rough = DetectRoughCorners(cameraCorners, projector, camera, Color.Green);

            //Improve accuracy with PhaseModulation
            //var phine = PhineTune(projector, camera, cameraCorners, rough, 32);
            //var phine2 = PhineTune(projector, camera, cameraCorners, rough, 64);
            //var phine3 = PhineTune(projector, camera, cameraCorners, phine2, 128);
            
            projector.DrawPoints(rough, 5);




            //Determine corners in projector space
            //var projectorCorners = DetectProjectorCorners(nolight, cameraCorners, projOutline, projector, camera);
            //Save corners in camera and projector space and store along side global coordinates that matches current checkerboard
            var data = new CalibrationData() { CameraCorners = cameraCorners, ProjectorCorners = rough };
            return data;
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
            Emgu.CV.Image<Emgu.CV.Structure.Gray, byte> gray = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(new byte[image.Height, image.Width, 1]);
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var r = image[y, x].Red;
                    var b = image[y, x].Blue;
                    var g = image[y, x].Green;
                    var rd = Distance(new double[] { r, b, g }, new double[] { 255, 0, 0 });
                    if (rd < 200)
                        gray[y, x] = new Emgu.CV.Structure.Gray(0);
                    else
                        gray[y, x] = new Emgu.CV.Structure.Gray(255);
                }
            }
            var corners = Emgu.CV.CameraCalibration.FindChessboardCorners(gray, patternSize, Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
            if (corners == null)
                return null;
            var cc = new PointF[][] { corners };
            gray.FindCornerSubPix(cc, new System.Drawing.Size(11, 11), new System.Drawing.Size(-1, -1), new Emgu.CV.Structure.MCvTermCriteria(30, 0.1));
            return corners;
        }

        public static double Distance(double[] a, double[] b)
        {
            return Math.Sqrt(Math.Pow((a[0] - b[0]), 2) +
                Math.Pow((a[1] - b[1]), 2) +
                Math.Pow((a[2] - b[2]), 2));
        }

        public static PointF[] DetectRoughCorners(PointF[] cameraCorners, Projector projector, Camera camera, Color fullColor)
        {
            projector.DrawBackground(Color.Black);
            var nolight = camera.TakePicture(10);
            var ids = new int[cameraCorners.Length];
            for (var i = 0; i < cameraCorners.Length; i++) ids[i] = i;
            var points = new List<MathNet.Numerics.LinearAlgebra.Double.DenseVector[]>();
            for (int i = 0; i < 18; i += 6)
            {
                var projected = GreySL(projector, camera, cameraCorners, nolight, fullColor, false, i)
                    .Zip(GreySL(projector, camera, cameraCorners, nolight, fullColor, true, i), 
                    (y, x) => new MathNet.Numerics.LinearAlgebra.Double.DenseVector(new double[] { x - i, y - i }))
                    .ToArray();
                points.Add(projected);
            }
            return ids.Select(i => points.Select(row => row[i])
                .Aggregate((v1, v2) => v1 + v2) / points.Count)
                .Select(r => new PointF((float)r[0], (float)r[1]))
                .ToArray();
        }

        public static Func<Bitmap, int, Func<PointF, bool>> Classifier(Bitmap nolight)
        {
            var no = new Image<Bgr, byte>(nolight);
            return (img, step) =>
            {
                var fu = new Image<Bgr, byte>(img);
                var d = (fu - no).Split()[1];
                double[] min, max;
                Point[] minp, maxp;
                d.MinMax(out min, out max, out minp, out maxp);
                var thresh = (max.Max() - min.Min()) * 0.05 + min.Min();
                if (step < 5)
                    d = d.ThresholdBinary(new Gray(thresh), new Gray(255)).Erode(4).Dilate(8).Erode(8).Dilate(4);
                else
                    d = d.ThresholdBinary(new Gray(thresh), new Gray(255)).Erode(2).Dilate(4).Erode(4).Dilate(2);
                if (DebugWindow != null)
                {
                    DebugWindow.DrawBitmap(d.Bitmap);
                }
                return (corner) =>
                {
                    return d[(int)corner.Y, (int)corner.X].Intensity > 0;
                };
            };
        }


        public static double[] GreySL(Projector projector, Camera camera, PointF[] corners, Bitmap nolight, Color fullColor, bool vertical, int offset)
        {
            uint[] horizontal = new uint[corners.Length];
            int max = (int)Math.Floor(Math.Log((vertical ? projector.bitmap.Width : projector.bitmap.Height), 2)) + 1;
            int subdivisions = 1;
            var nol = Classifier(nolight);
            for (var step = 0; step < max - 4; step++)
            {
                projector.DrawBackground(Color.Black);
                camera.TakePicture(2).Dispose();
                projector.DrawGrey(step, vertical, offset, fullColor);
                var light = camera.TakePicture(2);
                var classifier = nol(light, step);
                int idx = 0;
                Bitmap withCorners = null;
                foreach (var point in corners)
                {
                    var hit = classifier(point);
                    var h = horizontal[idx];
                    h = h << 1;
                    h = h | (hit ? (uint)1 : (uint)0);
                    horizontal[idx] = h;
                    idx++;
                    if (DebugWindow != null)
                    {
                        withCorners = light;
                        QuickDraw.Start(withCorners)
                            .Color(hit ? Color.Gray : Color.White)
                            .DrawPoint(point.X, point.Y, 5)
                            .Finish();
                    }
                }
                if (DebugWindow != null)
                    DebugWindow.DrawBitmap(withCorners);
                light.Dispose();
                subdivisions++;
            }
            
            var result = horizontal
                .Select(h =>
                    {
                        uint num = h;
                        uint mask;
                        for (mask = num >> 1; mask != 0; mask = mask >> 1)
                        {
                            num = num ^ mask;
                        }
                        return num;
                    })
                .Select(row => (1 - (double)row / Math.Pow(2, max - 4)) * (vertical ? projector.bitmap.Width : projector.bitmap.Height)).ToArray();
         
            return result;
        }

        public static double[] BinarySL(Projector projector, Camera camera, PointF[] corners, Bitmap nolight, Color fullColor, bool vertical)
        {
            int[] horizontal = new int[corners.Length];
            int max = (int)Math.Floor(Math.Log((vertical ? projector.bitmap.Width : projector.bitmap.Height), 2)) + 1;
            int subdivisions = 1;
            var nol = Classifier(nolight);
            for (var step = 1; step <= max - 4; step++)
            {
                projector.DrawBackground(Color.Black);
                camera.TakePicture(5).Dispose();
                projector.DrawBinary(step, vertical, fullColor);
                var light = camera.TakePicture(2);
                var classifier = nol(light, step);
                int idx = 0;
                Bitmap withCorners = null;
                foreach (var point in corners)
                {
                    var hit = classifier(point);
                    var h = horizontal[idx];
                    h = h << 1;
                    h = h | (hit ? 1 : 0);
                    horizontal[idx] = h;
                    idx++;
                    if (DebugWindow != null)
                    {
                        withCorners = light;
                        QuickDraw.Start(withCorners)
                            .Color(hit ? Color.Gray : Color.White)
                            .DrawPoint(point.X, point.Y, 5)
                            .Finish();
                    }
                }
                if (DebugWindow != null)
                    DebugWindow.DrawBitmap(withCorners);
                light.Dispose();
                subdivisions++;
            }

            var result = horizontal.Select(row => ((double)row / Math.Pow(2, max - 4)) * (vertical ? projector.bitmap.Width : projector.bitmap.Height)).ToArray();
            using (var bitmap = new Bitmap(projector.bitmap.Width, projector.bitmap.Height))
            {
                using (var fast = new FastBitmap(bitmap))
                {
                    for (var x = 0; x < bitmap.Width; x++)
                        for (var y = 0; y < bitmap.Height; y++)
                            if (result.Contains(vertical ? x : y))
                                fast[x, y] = Color.FromArgb(255, 255, 255, 255);
                            else
                                fast[x, y] = Color.FromArgb(255, 0, 0, 0);

                }
                projector.DrawBitmap(bitmap);
            }
            return result;
        }

        private static RectangleF DetermineBounds(PointF[] points, float width, float height)
        {
            var ulx = points.OrderBy(row => row.X).First().X / width;
            var uly = (height - points.OrderByDescending(row => row.Y).First().Y) / height;
            var brx = points.OrderByDescending(row => row.X).First().X / width;
            var bry = (height - points.OrderBy(row => row.Y).First().Y) / height;
            var w = brx - ulx;
            var h = bry - uly;
            return new RectangleF(new PointF(ulx - w * 0.2f, uly - h * 0.2f), new SizeF(w * 1.4f, h * 1.4f));
        }
    }

    public struct CalibrationData
    {
        public PointF[] CameraCorners;
        public PointF[] ProjectorCorners;
    }

    public class CalibrationResult
    {
        public IntrinsicCameraParameters Intrinsic;
        public ExtrinsicCameraParameters Extrinsic;

        public PointF[] Transform(float[][] points)
        {
            var res = Emgu.CV.CameraCalibration.ProjectPoints(points.Select(row => new MCvPoint3D32f(row[0], row[1], row[2])).ToArray(), Extrinsic, Intrinsic);
            return res;
        }

        public MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> InverseExtrinsic(float depth)
        {
            float z = depth;
            if (z == 0)
                z = 0.0001f;
            var rt = Extrinsic.ExtrinsicMatrix;

            MathNet.Numerics.LinearAlgebra.Single.DenseMatrix rt2 = MathNet.Numerics.LinearAlgebra.Single.DenseMatrix.OfArray(new float[,] 
            {
                { (float)rt.Data[0,0], (float)rt.Data[0,1], (float)rt.Data[0,2], (float)rt.Data[0,3] },
                { (float)rt.Data[1,0], (float)rt.Data[1,1], (float)rt.Data[1,2], (float)rt.Data[1,3] },
                { (float)rt.Data[2,0], (float)rt.Data[2,1], (float)rt.Data[2,2], (float)rt.Data[2,3] },
                {0,0,1 / z,0}
            });
            return rt2.Inverse();
        }

        public float[] InverseTransform(PointF point, float depth)
        {
            var irt = InverseExtrinsic(depth);
            return InverseTransform(point, irt);
        }

        public float[] InverseTransform(PointF point, MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> irt)
        {
            var points = new PointF[] { point };
            var undistorted = Intrinsic.Undistort(points, null, null);
            var ids = new int[undistorted.Length];
            for (var i = 0; i < ids.Length; i++) ids[i] = i;
            var threes = ids.Select(i => new MathNet.Numerics.LinearAlgebra.Single.DenseVector(new float[] { undistorted[i].X, undistorted[i].Y, 1, 1 })).ToArray();
            var tt = threes.Select(v => irt.Multiply(v)).ToArray();
            return tt.Select(v => new float[] { (v[0] / v[3]), (v[1] / v[3]), (v[2] / v[3]) }).First();
        }

        public float[][] InverseTransform(PointF[] points, MathNet.Numerics.LinearAlgebra.Generic.Matrix<float> irt)
        {
            var undistorted = Intrinsic.Undistort(points, null, null);
            var ids = new int[undistorted.Length];
            for (var i = 0; i < ids.Length; i++) ids[i] = i;
            var threes = ids.Select(i => new MathNet.Numerics.LinearAlgebra.Single.DenseVector(new float[] { undistorted[i].X, undistorted[i].Y, 1, 1 })).ToArray();
            var tt = threes.Select(v => irt.Multiply(v)).ToArray();
            return tt.Select(v => new float[] { (v[0] / v[3]), (v[1] / v[3]), (v[2] / v[3]) }).ToArray();
        }

        public float[][] InverseTransform(PointF[] points, float z)
        {
            var undistorted = Intrinsic.Undistort(points, null, null);
            var rt = Extrinsic.ExtrinsicMatrix;
            var t = Extrinsic.TranslationVector;

            MathNet.Numerics.LinearAlgebra.Double.DenseMatrix rt2 = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix.OfArray(new double[,] 
            {
                { rt.Data[0,0], rt.Data[0,1], rt.Data[0,2], rt.Data[0,3] },
                { rt.Data[1,0], rt.Data[1,1], rt.Data[1,2], rt.Data[1,3] },
                { rt.Data[2,0], rt.Data[2,1], rt.Data[2,2], rt.Data[2,3] },
                {0,0,1 / z,0}
            });
            var irt = rt2.Inverse();
            var ids = new int[undistorted.Length];
            for (var i = 0; i < ids.Length; i++) ids[i] = i;
            var threes = ids.Select(i => new MathNet.Numerics.LinearAlgebra.Double.DenseVector(new double[] { undistorted[i].X, undistorted[i].Y, 1, 1 })).ToArray();
            var tt = threes.Select(v => irt.Multiply(v)).ToArray();
            return tt.Select(v => new float[] { (float)(v[0] / v[3]), (float)(v[1] / v[3]), (float)(v[2] / v[3]) }).ToArray();
            //return ids.Select(i => new float[] { (float)tr[i][0], (float)tr[i][1], (float)tr[i][2] }).ToArray();
        }

        public double Intersect(MathNet.Numerics.LinearAlgebra.Generic.Vector<double> p0,
            MathNet.Numerics.LinearAlgebra.Generic.Vector<double> l0,
            MathNet.Numerics.LinearAlgebra.Generic.Vector<double> n,
            MathNet.Numerics.LinearAlgebra.Generic.Vector<double> l)
        {
            return (p0 - l0).DotProduct(n) / l.DotProduct(n);
        }
    }

    public class StereoCalibrationResult
    {
        public IntrinsicCameraParameters cameraIntrinsic;
        public ExtrinsicCameraParameters cameraExtrinsic;
        public IntrinsicCameraParameters projectorIntrinsic;
        public ExtrinsicCameraParameters projectorExtrinsic;
        public ExtrinsicCameraParameters cameraToProjectorExtrinsic;

        private PointF[] TransformC2P(MCvPoint3D32f[] cpoints)
        {
            return Emgu.CV.CameraCalibration.ProjectPoints(cpoints, cameraToProjectorExtrinsic, projectorIntrinsic);
        }

        public PointF[] TransformC2P(DepthImagePoint[] points)
        {
            var undistored = cameraIntrinsic.Undistort(points.Select(row => new PointF(row.X, row.Y)).ToArray(), null, null);

            var t = undistored.Zip(points, (p, pz) => new MCvPoint3D32f(p.X, p.Y, pz.Depth / 1000.0f)).ToArray();
            return TransformC2P(t);
        }

        public PointF[] TransformG2P(float[][] points)
        {
            var res = Emgu.CV.CameraCalibration.ProjectPoints(points.Select(row => new MCvPoint3D32f(row[0], row[1], row[2])).ToArray(), projectorExtrinsic, projectorIntrinsic);
            return res;
        }

        public PointF[] TransformG2C(float[][] points)
        {
            var res = Emgu.CV.CameraCalibration.ProjectPoints(points.Select(row => new MCvPoint3D32f(row[0], row[1], row[2])).ToArray(), cameraExtrinsic, cameraIntrinsic);
            return res;
        }
    }
}
