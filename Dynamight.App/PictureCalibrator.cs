using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class PictureCalibrator
    {

        public static void Run(string[] args)
        {
            //var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            //var window = new BitmapWindow(main.Bounds.Left + main.Width / 2 + 50, 50, 640, 480);
            //window.Load();
            //window.ResizeGraphics();
            //var window2 = new BitmapWindow(main.Bounds.Left + 50, 50, 640, 480);
            //window2.Load();
            //window2.ResizeGraphics();
            CalibrationResult kinectcalib = null;
            if (args.Length > 0)
            {
                var file = args.First();
                kinectcalib = Utils.DeSerializeObject<CalibrationResult>(file);
            }
            Console.Write("Enter the folders you'd like to use: ");
            var folders = Console.ReadLine().Split(' ').ToArray();
            var maps = folders.SelectMany(f => PictureGrabber.GetBitmaps(f)).ToArray();
            Size pattern = new Size(7, 4);
            float chsize = 0.05f;
            var kcorners = maps.Select(ms => StereoCalibration.GetCameraCorners(ms.Camera, pattern)).ToArray();
            var pcorners = maps.Select(ms => StereoCalibration.GetCameraCorners(ms.Projector, pattern)).Take(0).ToArray();
            kcorners = kcorners.Zip(pcorners, (a, b) => a != null && b != null ? a : null).Where(a => a != null).ToArray();
            pcorners = kcorners.Zip(pcorners, (a, b) => a != null && b != null ? b : null).Where(a => a != null).ToArray();
            if (kcorners.Count() != pcorners.Count())
                Console.WriteLine("Number of good shots did not match.");
            if (kinectcalib == null)
            {
                if (kcorners.Length == 0)
                {
                    Console.WriteLine("Could not find camera corners");
                    return;
                }
                kinectcalib = StereoCalibration.CalibrateCamera(kcorners, maps.First().Camera.Size, pattern, chsize);
            }
            var tkcorners = kcorners.Select(points => StereoCalibration.Undistort(kinectcalib, points)).ToArray();
            var tpcorners = pcorners.Select(points => StereoCalibration.Undistort(kinectcalib, points)).ToArray();
            var hgraphs = tpcorners.Zip(maps.Select(m => m.ProjCorners), (c, p) => StereoCalibration.FindHomography(c, p));
            var ptkcorners = tkcorners.Zip(hgraphs, (ps, hg) => hg(ps)).ToArray();
            //bool proceed = false;
            //window.Keyboard.KeyDown += (o, e) =>
            //{
            //    proceed = true;
            //};
            //window2.Keyboard.KeyDown += (o, e) =>
            //{
            //    proceed = true;
            //};
            //for (int i = 0; i < maps.Length; i++)
            //{
            //    Projector proj = new Projector();
            //    var map1 = (Bitmap)maps[i].Camera.Clone();
            //    QuickDraw.Start(map1).DrawPoint(kcorners[i], 5).Finish();
            //    var map2 = (Bitmap)maps[i].Projector.Clone();
            //    QuickDraw.Start(map2).DrawPoint(pcorners[i], 5).Finish();
            //    window.DrawBitmap(map1);
            //    window2.DrawBitmap(map2);
            //    proj.DrawPoints(ptkcorners[i], 5f);
            //    while (!proceed)
            //    {
            //        window.ProcessEvents();
            //        window2.ProcessEvents();
            //    }
            //    proceed = false;
            //}
            Projector proj = new Projector();
            var projcalib = StereoCalibration.CalibrateCamera(ptkcorners, proj.Size, pattern, chsize);
            proj.Close();
            Console.WriteLine("Save result?");
            Console.ReadLine();
            Utils.SerializeObject(kinectcalib, Calibration.KinectDefaultFileName);
            Utils.SerializeObject(projcalib, Calibration.ProjectorDefaultFileName);
            
        }
    }
}
