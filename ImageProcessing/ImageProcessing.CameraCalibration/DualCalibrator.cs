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
            var test = Calibrate(projector, camera, null, 1);

        }

        public static Func<Bitmap, int, Func<PointF, bool>> Classifier(Bitmap nolight)
        {
            var no = new Image<Bgr, byte>(nolight);
            return (img, step) =>
            {
                var fu = new Image<Bgr, byte>(img);
                var t = new Image<Bgr, byte>(fu.Width, fu.Height);
                var d = (t + fu - no).Split()[1];
                double[] min, max;
                Point[] minp, maxp;
                d.MinMax(out min, out max, out minp, out maxp);
                var thresh = (max.Max() - min.Min()) * 0.05 + min.Min();
                if (step < 5)
                    d = d.ThresholdBinary(new Gray(thresh), new Gray(255)).Erode(4).Dilate(5).Erode(1);
                else
                    d = d.ThresholdBinary(new Gray(thresh), new Gray(255)).Erode(2).Dilate(3).Erode(1);
                if (DebugWindow != null)
                    DebugWindow.DrawBitmap(d.Bitmap);
                return (corner) =>
                    {
                        return d[(int)corner.Y, (int)corner.X].Intensity > 0;
                    };
            };
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
            
            //var projCorners = DetectRoughCorners(cameraCorners, projector, camera);
            //projector.DrawPoints(projCorners, 10);
            return true;
        }

        public static Func<Bitmap, Func<int, int, double>> PhaseClassifier(Bitmap nolight, Bitmap fulllight, double width, double height)
        {
            var nimg = new Image<Bgr, byte>(nolight).Split()[1];
            var fimg = new Image<Bgr, byte>(fulllight).Split()[1];
            return (light) =>
            {
                var limg = new Image<Bgr, byte>(light).Split()[1];
                return (x, y) =>
                    {
                        var ni = nimg[y, x].Intensity;
                        var fi = fimg[y, x].Intensity;
                        var li = limg[y, x].Intensity;
                        return (double)(li - ni) / (double)(fi - ni);
                    };
            };
        }

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

        public static CalibrationResult Calibrate(Projector projector, Camera camera, Emgu.CV.Structure.MCvPoint3D32f[] globalCorners, int iterations = 10, int iterationTimeout = 500)
        {
            //if (PhaseModulation.DetermineAlgorithmIntegrity(8, 10000, 0.01) > 0.15)
            //    throw new Exception("Phase modulation integrity is failing");

            //projector.SetBounds(new RectangleF(0.25f, 0.25f, 0.5f, 0.5f));
            //projector.DrawBinary(3, true, Color.White);
            //Red and blue checkerboard
            //For each orientation of checkerboard:
            //float w = projector.bitmap.Width;
            //float h = projector.bitmap.Height;
            //var ps = new PointF[] {
            //        new PointF(440f, h - 324f), new PointF(640f, h - 168f)
            //    };

            ////projector.SetBounds(DetermineBounds(ps, projector.bitmap.Width, projector.bitmap.Height));
            //projector.SetBounds(DetermineBounds(ps, w, h));
            //projector.DrawBackground(Color.White);

            var datas = new CalibrationData[iterations];
            for (int i = 0; i < iterations; i++)
            {
                //Take pic of checkerboard with no proj light
                projector.DrawBackground(Color.Black);
                //Detect corners in camera space
                PointF[] cameraCorners;
                Bitmap withCorners;
                do
                {
                    var nolight = camera.TakePicture(5);
                    withCorners = camera.TakePicture();
                    cameraCorners = DetectCornersRB(nolight, new Size(7, 4));

                } while (cameraCorners == null);
                //cameraCorners = cameraCorners.Take(1).Union(cameraCorners.Skip(cameraCorners.Length - 1)).ToArray();
                if (DebugWindow != null)
                {
                    QuickDraw.Start(withCorners)
                        .Color(Color.White)
                        .DrawPoint(cameraCorners, 5)
                        .Finish();
                    DebugWindow.DrawBitmap(withCorners);
                }

                //Determine rough checkerboard coordinates in projector space with graycode or binary structured light
                //var smooth = PhaseCalib(projector, camera, cameraCorners);
                //projector.DrawPoints(smooth, 5);
                Func<PointF[]> Pass = () =>
                {
                    var rough = DetectRoughCorners(cameraCorners, projector, camera, Color.Green);

                    //var phine = PhineTune(projector, camera, cameraCorners, rough, 32);
                    var phine2 = PhineTune(projector, camera, cameraCorners, rough, 64);
                    var phine3 = PhineTune(projector, camera, cameraCorners, phine2, 128);
                    return phine3;
                };

                var ids = Range.OfInts(cameraCorners.Length);
                var passes = Range.OfInts(10).ToArray();
                var data = passes.Select(p => Pass()).ToArray();
                var result = ids.Select(row => passes.Select(p => data[p][row])).ToArray();
                var avg = ids.Select(id => new PointF(result[id].Select(r => r.X).Average(),
                    result[id].Select(r => r.Y).Average())).ToArray();
                projector.DrawPoints(avg, 5);
                var avg2 = ids.Select(id => new PointF(
                    result[id].Select(r => new { val= r.X, dist = Math.Abs(avg[id].X - r.X) })
                        .OrderByDescending(row => row.dist).Reverse().Take(5).Select(row => row.val).Average(),
                    result[id].Select(r => new { val = r.Y, dist = Math.Abs(avg[id].Y - r.Y) })
                        .OrderByDescending(row => row.dist).Reverse().Take(5).Select(row => row.val).Average())).ToArray();
                projector.DrawPoints(avg2, 5);
                //var outline = DetermineBounds(rough, projector.bitmap.Width, projector.bitmap.Height);
                //projector.SetBounds(outline);

                /*
                double[] xs, ys;
                {
                    projector.DrawBackground(Color.Black);
                    var nolight = camera.TakePicture(5);
                    projector.DrawBackground(Color.Green);
                    var fulllight = camera.TakePicture(5);
                    projector.DrawBackground(Color.Black);
                    camera.TakePicture(5).Dispose();
                    projector.Draw(PhaseModulation.DrawPhaseModulation(1, -2 * Math.PI / 3, true, Color.Green));
                    var light1 = camera.TakePicture(2);
                    projector.DrawBackground(Color.Black);
                    camera.TakePicture(5).Dispose();
                    projector.Draw(PhaseModulation.DrawPhaseModulation(1, 0, true, Color.Green));
                    var light2 = camera.TakePicture(2);
                    projector.DrawBackground(Color.Black);
                    camera.TakePicture(5).Dispose();
                    projector.Draw(PhaseModulation.DrawPhaseModulation(1, 2 * Math.PI / 3, true, Color.Green));
                    var light3 = camera.TakePicture(2);

                    var pc = PhaseClassifier(nolight, fulllight, projector.bitmap.Width, projector.bitmap.Height);
                    var pc1 = pc(light1);
                    var pc2 = pc(light2);
                    var pc3 = pc(light3);
                    xs = cameraCorners.Select(row => PhaseModulation.IntensityToPhase(pc1((int)row.X, (int)row.Y),
                        pc2((int)row.X, (int)row.Y),
                        pc3((int)row.X, (int)row.Y))).ToArray();

                }

                {
                    projector.DrawBackground(Color.Black);
                    var nolight = camera.TakePicture(5);
                    projector.DrawBackground(Color.Green);
                    var fulllight = camera.TakePicture(5);
                    projector.DrawBackground(Color.Black);
                    camera.TakePicture(5).Dispose();
                    projector.DrawPhaseMod(1, (float)(-2 * Math.PI / 3), false, Color.FromArgb(0, 255, 0));
                    var light1 = camera.TakePicture(2);
                    projector.DrawBackground(Color.Black);
                    camera.TakePicture(5).Dispose();
                    projector.DrawPhaseMod(1, (float)(0), false, Color.FromArgb(0, 255, 0));
                    var light2 = camera.TakePicture(2);
                    projector.DrawBackground(Color.Black);
                    camera.TakePicture(5).Dispose();
                    projector.DrawPhaseMod(1, (float)(2 * Math.PI / 3), false, Color.FromArgb(0, 255, 0));
                    var light3 = camera.TakePicture(2);

                    var pc = PhaseClassifier(nolight, fulllight, projector.bitmap.Width, projector.bitmap.Height);
                    var pc1 = pc(light1);
                    var pc2 = pc(light2);
                    var pc3 = pc(light3);
                    ys = cameraCorners.Select(row => PhaseModulation.IntensityToPhase(pc1((int)row.X, (int)row.Y),
                        pc2((int)row.X, (int)row.Y),
                        pc3((int)row.X, (int)row.Y))).ToArray();

                }

                var resx = xs.Select(row => PhaseModulation.AbsolutePhase(new double[] { row }, new int[] { 1 })).Select(row => row * projector.bitmap.Width).ToArray();
                var resy = ys.Select(row => PhaseModulation.AbsolutePhase(new double[] { row }, new int[] { 1 })).Select(row => row * projector.bitmap.Height).ToArray();
                var res = resx.Zip(resy, (x, y) => new PointF((float)Math.Round(x, 0), (float)Math.Round(y, 0))).ToArray();
                projector.SetBounds(new RectangleF(0, 0, 1, 1));
                projector.DrawPoints(res, 5f);
                 */
                //Determine corners in projector space
                //var projectorCorners = DetectProjectorCorners(nolight, cameraCorners, projOutline, projector, camera);
                //Save corners in camera and projector space and store along side global coordinates that matches current checkerboard
                //var data = new CalibrationData() { CameraCorners = cameraCorners, ProjectorCorners = projectorCorners, GlobalCorners = globalCorners };

                datas[i] = default(CalibrationData);
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

        public static PointF[] DetectRoughCorners(PointF[] cameraCorners, Projector projector, Camera camera, Color fullColor)
        {
            projector.DrawBackground(Color.Black);
            var nolight = camera.TakePicture(10);

            var projected = BinarySL(projector, camera, cameraCorners, nolight, fullColor, false)
                .Zip(BinarySL(projector, camera, cameraCorners, nolight, fullColor, true), (y,x) => new PointF((float)x, (float)y))
                .ToArray();
            return projected;
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
