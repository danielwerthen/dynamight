using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics.Input;
using Microsoft.Kinect;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public struct GrabbedPictureSet
    {
        public Bitmap Camera;
        public Bitmap Projector;
        public PointF[] ProjCorners;
    }
    public class PictureGrabber
    {
        public const string CAM_FILE = "camPass";
        public const string PROJ_FILE = "projPass";
        public const string PROJ_CORNER_FILE = "projCornerPass";
        public static IEnumerable<GrabbedPictureSet> GetBitmaps(string folder)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), folder);
            return Directory.GetFiles(dir, CAM_FILE + "*.bmp").Zip(Directory.GetFiles(dir, PROJ_FILE + "*.bmp"), (a, b) => new Bitmap[] {
                new Bitmap(Path.Combine(dir, a)),
                new Bitmap(Path.Combine(dir, b)),
            }).Zip(Directory.GetFiles(dir, PROJ_CORNER_FILE + "*.xml"), (a, b) => new GrabbedPictureSet() {
                Camera = a[0],
                Projector = a[1],
                ProjCorners = Utils.DeSerializeObject<PointF[]>(Path.Combine(dir, b)),  
            });
        }

        public static void Run(string[] args)
        {
            string folderName = args.FirstOrDefault();
            if (folderName == null)
            {
                Console.Write("Please enter a valid folder name for output pictures: ");
                folderName = Console.ReadLine();
            }
            var kinects = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).ToArray();
            var camera = kinects.Select(s =>
            {
                s.Start();
                return new Camera(s, ColorImageFormat.RgbResolution1280x960Fps12);
            }).First();
            Projector projector = new Projector();
            var dir = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var files = GetBitmaps(dir);
            if (files.Count() > 0)
            {
                Console.WriteLine("There already are a couple files in this folder, would you like to delete them? (y/n)");
                var cohice = Console.ReadLine();
                if (cohice != "y")
                    return;
            }
            foreach (var file in Directory.GetFiles(dir))
                File.Delete(file);
            int passes = 0;
            var keyl = new KeyboardListener(projector.window.Keyboard);
            double offsetx = 0, offsety = 0, scale = 0.5;
            bool proceed = false, quit = false;
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[0], (f) => offsety += f);
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Left, OpenTK.Input.Key.Right, new OpenTK.Input.Key[0], (f) => offsetx -= f);
            keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ShiftLeft }, (f) => scale += f);
            keyl.AddAction(() => proceed = true, Key.Space);
            keyl.AddAction(() => quit = proceed = true, Key.Q);

            Console.WriteLine("Outputting pictures into {0}", dir);
            while (true)
            {
                string camFile = string.Format("{0}/{1}{2:000}.bmp", dir, CAM_FILE, passes);
                string projFile = string.Format("{0}/{1}{2:000}.bmp", dir, PROJ_FILE, passes);
                string projCornerFile = string.Format("{0}/{1}{2:000}.xml", dir, PROJ_CORNER_FILE, passes);
                projector.DrawBackground(Color.Black);
                Console.WriteLine("Press space to do cam pass with corners, q to exit");
                while (!proceed) projector.window.ProcessEvents();
                proceed = false;
                if (quit)
                    break;
                camera.TakePicture(0).Save(camFile);
                Console.WriteLine("Adjust projection then press space");
                while (!proceed)
                {
                    projector.DrawCheckerboard(new System.Drawing.Size(8, 5), 0, 0, 0, scale, offsetx, offsety);
                    projector.window.ProcessEvents();
                }
                proceed = false;
                var points = projector.DrawCheckerboard(new System.Drawing.Size(8, 5), 0, 0, 0, scale, offsetx, offsety);
                camera.TakePicture(4).Save(projFile);
                Utils.SerializeObject(points, projCornerFile);
                passes++;
            }
            projector.Close();
        }
    }
}
