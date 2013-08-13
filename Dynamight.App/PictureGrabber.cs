using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
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
        public static IEnumerable<GrabbedPictureSet> GetManualBitmaps(string folder)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), folder);
            return Directory.GetFiles(dir, PROJ_FILE + "*.bmp").Select(a => 
                new Bitmap(Path.Combine(dir, a))
            ).Zip(Directory.GetFiles(dir, PROJ_CORNER_FILE + "*.xml"), (a, b) => new GrabbedPictureSet()
            {
                Camera = a,
                Projector = null,
                ProjCorners = Utils.DeSerializeObject<PointF[]>(Path.Combine(dir, b)),
            });
        }
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
        public static void RunManual(string[] args)
        {
            string folderName = args.FirstOrDefault();
            if (folderName == null)
            {
                Console.Write("Please enter a valid folder name for output pictures: ");
                folderName = Console.ReadLine();
            }
            Func<Bitmap> cam;
            var kinects = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).ToArray();
            var xcam = new ExCamera();
            if (true)
            {
                var camera = kinects.Select(s =>
                {
                    s.Start();
                    return new Camera(s, ColorImageFormat.RgbResolution1280x960Fps12);
                }).First();
                cam = () => camera.TakePicture(3);
            }
            else
            {
                cam = () => xcam.TakePicture();
            }
            Projector projector = new Projector();
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var display = new BitmapWindow(main.Bounds.Left + 50, 50, 1280, 960);
            display.Load();
            display.ResizeGraphics();
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
            double offsetx = 0, offsety = 0, scalex = 0.5, scaley = 0.5;
            double rotx = 0, roty = 0, rotz = 0;
            bool proceed = false, quit = false;
            double incr = 0.005;
            List<PointF> points = new List<PointF>();
            projector.window.Mouse.ButtonDown += (o, e) =>
            {
                points.Add(new PointF(e.X, e.Y));
            };
            keyl.AddAction(() => points.RemoveAt(points.Count - 1), Key.BackSpace);

            keyl.AddAction(() => proceed = true, Key.Space);
            keyl.AddAction(() => quit = proceed = true, Key.Q);

            Console.WriteLine("Outputting pictures into {0}", dir);
            while (true)
            {
                string camFile = string.Format("{0}/{1}{2:000}.bmp", dir, CAM_FILE, passes);
                string projFile = string.Format("{0}/{1}{2:000}.bmp", dir, PROJ_FILE, passes);
                string projCornerFile = string.Format("{0}/{1}{2:000}.xml", dir, PROJ_CORNER_FILE, passes);
                projector.DrawBackground(Color.Black);

                Console.WriteLine("Adjust projection then press space");
                while (!proceed)
                {
                    projector.DrawPoints(points.ToArray(), 5f);
                    //projector.DrawCheckerboard(new System.Drawing.Size(8, 5), rotx, roty, rotz, scalex, scaley, offsetx, offsety);
                    display.DrawBitmap(cam());
                    projector.window.ProcessEvents();
                }
                if (quit)
                    break;
                proceed = false;
                //var points = projector.DrawCheckerboard(new System.Drawing.Size(8, 5), rotx, roty, rotz, scalex, scaley, offsetx, offsety);
                projector.DrawBackground(Color.Black);
                cam().Save(projFile);
                Utils.SerializeObject(points.ToArray(), projCornerFile);
                points.Clear();
                passes++;
            }
            projector.Close();
            display.Close();
        }

        public static void RunPassing(string[] args)
        {
            string folderName = args.FirstOrDefault();
            if (folderName == null)
            {
                Console.Write("Please enter a valid folder name for output pictures: ");
                folderName = Console.ReadLine();
            }
            Func<Bitmap> cam;
            var kinects = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).ToArray();
            var xcam = new ExCamera();
            if (true)
            {
                var camera = kinects.Select(s =>
                {
                    s.Start();
                    return new Camera(s, ColorImageFormat.RgbResolution1280x960Fps12);
                }).First();
                cam = () => camera.TakePicture(3);
            }
            else
            {
                cam = () => xcam.TakePicture();
            }
            Projector projector = new Projector();
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var display = new BitmapWindow(main.Bounds.Left + 50, 50, 1280, 960);
            display.Load();
            display.ResizeGraphics();
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
            double offsetx = 0, offsety = 0, scalex = 0.5, scaley = 0.5;
            double rotx = 0, roty = 0, rotz = 0;
            bool proceed = false, quit = false;
            double incr = 0.005;
            keyl.AddBinaryAction(0.0001, -0.0001, Key.O, Key.L, null, (f) => incr += f);
            int? state = null;
            keyl.AddAction(() => state = (state != 1 ? (int?)1 : null), Key.T);
            keyl.AddAction(() => state = (state != 2 ? (int?)2 : null), Key.S);
            keyl.AddAction(() => state = (state != 3 ? (int?)3 : null), Key.R);
            keyl.AddAction(() => state = (state != 4 ? (int?)4 : null), Key.Z);
            keyl.AddAction(() =>
            {
                rotx = 0;
                roty = 0;
                rotz = 0;
            }, Key.Number0);

            Action<double> up = (f) =>
            {
                if (state == 1)
                    offsety += f;
                else if (state == 2)
                    scaley += f;
                else if (state == 3)
                    roty += f * 2;
                else if (state == 4)
                    rotz += f * 2;
            };
            Action<double> left = (f) =>
            {
                if (state == 1)
                    offsetx -= f;
                else if (state == 2)
                    scalex += f;
                else if (state == 3)
                    rotx += f * 2;
            };

            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, null, (i) => up(i * incr));
            keyl.AddBinaryAction(1, -1, Key.Left, Key.Right, null, (i) => left(i * incr));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ShiftLeft }, (i) => up(i * incr * 2));
            keyl.AddBinaryAction(1, -1, Key.Left, Key.Right, new Key[] { Key.ShiftLeft }, (i) => left(i * incr * 2));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ControlLeft }, (i) => up(i * incr * 3));
            keyl.AddBinaryAction(1, -1, Key.Left, Key.Right, new Key[] { Key.ControlLeft }, (i) => left(i * incr * 3));
            keyl.AddBinaryAction(1, -1, Key.Up, Key.Down, new Key[] { Key.ShiftLeft, Key.ControlLeft }, (i) => up(i * incr * 2 * 3));
            keyl.AddBinaryAction(1, -1, Key.Left, Key.Right, new Key[] { Key.ShiftLeft, Key.ControlLeft }, (i) => left(i * incr * 2 * 3));

            //keyl.AddBinaryAction(0.005, -0.005, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[0], (f) => offsety += f);
            //keyl.AddBinaryAction(0.005, -0.005, OpenTK.Input.Key.Left, OpenTK.Input.Key.Right, new OpenTK.Input.Key[0], (f) => offsetx -= f);
            //keyl.AddBinaryAction(0.005, -0.005, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ShiftLeft }, (f) => scale += f);

            //keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ControlLeft }, (f) => offsety += f);
            //keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Left, OpenTK.Input.Key.Right, new OpenTK.Input.Key[] { Key.ControlLeft }, (f) => offsetx -= f);
            //keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ShiftLeft, Key.ControlLeft }, (f) => scale += f);

            //keyl.AddBinaryAction(0.005, -0.005, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.AltLeft }, (f) => roty += f);
            //keyl.AddBinaryAction(0.005, -0.005, OpenTK.Input.Key.Left, OpenTK.Input.Key.Right, new OpenTK.Input.Key[] { Key.AltLeft }, (f) => rotx -= f);
            //keyl.AddBinaryAction(0.005, -0.005, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ShiftLeft, Key.AltLeft }, (f) => rotz += f);

            //keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ControlLeft, Key.AltLeft }, (f) => roty += f);
            //keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Left, OpenTK.Input.Key.Right, new OpenTK.Input.Key[] { Key.ControlLeft, Key.AltLeft }, (f) => roty -= f);
            //keyl.AddBinaryAction(0.02, -0.02, OpenTK.Input.Key.Up, OpenTK.Input.Key.Down, new OpenTK.Input.Key[] { Key.ShiftLeft, Key.ControlLeft, Key.AltLeft }, (f) => rotz += f);
            keyl.AddAction(() => proceed = true, Key.Space);
            keyl.AddAction(() => quit = proceed = true, Key.Q);

            Console.WriteLine("Outputting pictures into {0}", dir);
            while (true)
            {
                string camFile = string.Format("{0}/{1}{2:000}.bmp", dir, CAM_FILE, passes);
                string projFile = string.Format("{0}/{1}{2:000}.bmp", dir, PROJ_FILE, passes);
                string projCornerFile = string.Format("{0}/{1}{2:000}.xml", dir, PROJ_CORNER_FILE, passes);
                projector.DrawBackground(Color.Black);

                Console.WriteLine("Adjust projection then press space");
                while (!proceed)
                {
                    projector.DrawCheckerboard(new System.Drawing.Size(8, 5), rotx, roty, rotz, scalex, scaley, offsetx, offsety);
                    display.DrawBitmap(cam());
                    projector.window.ProcessEvents();
                }
                if (quit)
                    break;
                proceed = false;
                var points = projector.DrawCheckerboard(new System.Drawing.Size(8, 5), rotx, roty, rotz, scalex, scaley, offsetx, offsety);
                cam().Save(projFile);
                Utils.SerializeObject(points, projCornerFile);
                passes++;
            }
            projector.Close();
            display.Close();
        }

        public static void Run(string[] args)
        {
            string folderName = args.FirstOrDefault();
            if (folderName == null)
            {
                Console.Write("Please enter a valid folder name for output pictures: ");
                folderName = Console.ReadLine();
            }
            Func<Bitmap> cam;
            var kinect = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).First();
            var xcam = new ExCamera();
            if (true)
            {
                kinect.Start();
                var camera = new Camera(kinect, ColorImageFormat.RgbResolution1280x960Fps12);
                cam = () => camera.TakePicture(3);
            }
            else
            {
                cam = () => xcam.TakePicture();
            }
            Projector projector = new Projector();
            var main = OpenTK.DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var display = new BitmapWindow(main.Bounds.Left + 50, 50, 1280, 960);
            display.Load();
            display.ResizeGraphics();
            var dir = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (Directory.GetFiles(dir).Count() > 0)
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

            if (false)
            {
                var dkeyl = new KeyboardListener(display.Keyboard);
                dkeyl.AddBinaryAction(2, -2, Key.Up, Key.Down, null, (f) =>
                {
                    var ele = kinect.ElevationAngle;
                    ele += f;
                    if (ele < kinect.MaxElevationAngle && ele > kinect.MinElevationAngle)
                        kinect.ElevationAngle = ele;
                    var test = kinect.ElevationAngle;
                    test.ToString();
                });
                dkeyl.AddAction(() => proceed = true, Key.Space);

                Console.WriteLine("Adjust kinect elevation");
                while (!proceed)
                {
                    display.DrawBitmap(cam());
                    display.ProcessEvents();
                }
                proceed = false;
            }
            else
            {
                kinect.ElevationAngle = 19;
            }

            Console.WriteLine("Outputting pictures into {0}", dir);
            while (true)
            {
                string camFile = string.Format("{0}/{1}{2:000}.bmp", dir, CAM_FILE, passes);
                string projFile = string.Format("{0}/{1}{2:000}.bmp", dir, PROJ_FILE, passes);
                string projCornerFile = string.Format("{0}/{1}{2:000}.xml", dir, PROJ_CORNER_FILE, passes);
                projector.DrawBackground(Color.LightGray);

                Console.WriteLine("Press space to do cam pass with corners, q to exit");
                while (!proceed)
                {
                    display.DrawBitmap(cam());
                    projector.window.ProcessEvents();
                    display.ProcessEvents();
                }
                proceed = false;
                if (quit)
                    break;
                cam().Save(camFile);
                Console.WriteLine("Adjust projection then press space");
                while (!proceed)
                {
                    projector.DrawCheckerboard(new System.Drawing.Size(8, 5), 0, 0, 0, scale, offsetx, offsety);
                    display.DrawBitmap(cam());
                    projector.window.ProcessEvents();
                    display.ProcessEvents();
                }
                proceed = false;
                var points = projector.DrawCheckerboard(new System.Drawing.Size(8, 5), 0, 0, 0, scale, offsetx, offsety);
                cam().Save(projFile);
                Utils.SerializeObject(points, projCornerFile);
                passes++;
            }
            projector.Close();
            display.Close();
        }
    }
}
