using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = args;
            while (true)
            {
                switch (input.Select(row => row.ToLowerInvariant()).FirstOrDefault() ?? "")
                {
                    case "calibrate":
                    case "c":
                        Calibration.Calibrate(input.Skip(1).ToArray());
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
                Console.WriteLine("Please enter one of the following commands:");
                Console.WriteLine("Calibrate (c) [cameraCalibrationFile] [projectorCalibrationFile]");
                input = Console.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}
