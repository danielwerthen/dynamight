using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public struct ProgramCommand
    {
        public string[] Names;
        public string[] Parameters;
        public Action<string[]> Run;
    }

    class Program
    {
        static bool exit = false;
        static ProgramCommand[] commands = new ProgramCommand[] {
            new ProgramCommand() {
                Names = new string[] { "calibrate", "c" }, 
                Parameters = new string[] { "camcalibfile", "projcalibfile" },
                Run = Calibration.Calibrate
            },
            new ProgramCommand() {
                Names = new string[] { "calibrateExtrin", "ce" }, 
                Parameters = new string[] { "camcalibfile" },
                Run = ExtrinsicCalibration.Run
            },
            new ProgramCommand() {
                Names = new string[] { "pictureGrabber", "pg" }, 
                Parameters = new string[] { "folder" },
                Run = PictureGrabber.Run
            },
            new ProgramCommand() {
                Names = new string[] { "pictureCalibrator", "pc" }, 
                Parameters = new string[] { "precalib" },
                Run = PictureCalibrator.Run
            },
            new ProgramCommand() {
                Names = new string[] { "calibrationResultPresenter", "crp" }, 
                Parameters = new string[] { "camcalibfile", "projcalibfile" },
                Run = CalibrationResultPresenter.Run
            },
            new ProgramCommand() {
                Names = new string[] { "lightningFast", "lf" }, 
                Parameters = new string[] { "camcalibfile", "projcalibfile" },
                Run = LightningFastApp.Run
            },
            new ProgramCommand() {
                Names = new string[] { "lightStudio", "ls" }, 
                Parameters = new string[] { "camcalibfile", "projcalibfile" },
                Run = LightningStudio.Run
            },
            new ProgramCommand() {
                Names = new string[] { "picTakeHelper", "pth" }, 
                Parameters = new string[] { },
                Run = PicTakeHelperApp.Run
            },
            new ProgramCommand() {
                Names = new string[] { "remotekinect", "rk" }, 
                Parameters = new string[] { },
                Run = RemoteApp.Run
            },
            new ProgramCommand() {
                Names = new string[] { "overview", "o" }, 
                Parameters = new string[] { "camcalibfile" },
                Run = OverviewApp.Run
            },
            new ProgramCommand() {
                Names = new string[] { "Lightning", "l" }, 
                Parameters = new string[] { "camcalibfile", "projcalibfile" },
                Run = HandLightning.Run
            },
            new ProgramCommand() {
                Names = new string[] { "Skeleton", "s" }, 
                Parameters = new string[] { "camcalibfile", "projcalibfile" },
                Run = SkeletonApp.Run
            },
            new ProgramCommand() {
                Names = new string[] { "Transform", "t" }, 
                Parameters = new string[] { "camcalibfile", "projcalibfile" },
                Run = TransformApp.Run
            },
            new ProgramCommand() {
                Names = new string[] { "MovingHeads", "mh" }, 
                Parameters = new string[] { "camcalibfile", "projcalibfile" },
                Run = MovingHeadsApp.Run
            },
            new ProgramCommand() {
                Names = new string[] { "quit", "q" }, 
                Parameters = new string[] {  },
                Run = (args) => exit = true
            },

        };
        static void Main(string[] args)
        {
            var input = args;
            while (true)
            {
                var ic = input.Select(row => row.ToLowerInvariant()).FirstOrDefault() ?? "";
                var matches = commands.Where(c => c.Names.Contains(ic));
                if (matches.Count() == 0)
                    Console.WriteLine("Unknown command");
                else
                {
                    matches.First().Run(input.Skip(1).ToArray());
                    if (exit)
                        return;
                }
                Console.WriteLine("Please enter one of the following commands:");
                foreach (var c in commands)
                    Console.WriteLine("{0} ({1}) {2}",
                        char.ToUpper(c.Names.First().First()) + c.Names.First().Substring(1),
                        string.Join("/", c.Names.Skip(1)),
                        string.Join(" ", c.Parameters.Select(str => string.Format("[{0}]", str))));
                input = Console.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}
