﻿using Dynamight.ImageProcessing.CameraCalibration;
using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Graphics;
using Graphics.Projection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class TransformApp
    {
        public static void Run(string[] args)
        {
            var camfile = args.FirstOrDefault() ?? Calibration.KinectDefaultFileName;
            var projfile = args.Skip(1).FirstOrDefault() ?? Calibration.ProjectorDefaultFileName;
            if (!File.Exists(camfile) || !File.Exists(projfile))
            {
                Console.WriteLine("Either calib file could not be found.");
                return;
            }
            var cc = Utils.DeSerializeObject<CalibrationResult>(camfile);
            var pc = Utils.DeSerializeObject<CalibrationResult>(projfile);

            //var points = new float[][] {
            //    new float[] { 0,0,0,1 },
            //    new float[] { 0.2f,0,0,1 },
            //    new float[] { 0.2f,0.2f,0,1 },
            //    new float[] { 0,0.2f,0,1 },
            //};

            //var proj = new Projector();
            //proj.DrawPoints(pc.Transform(points), 5.0f);
            //proj.Close();

            var window = ProgramWindow.OpenOnSecondary();
            
            var program = new TransformativeProgram();
            window.SetProgram(program);
            program.Draw().Fill(Color.Goldenrod).Finish();
            program.SetProjection(pc);
            while (true)
                window.RenderFrame();
        }
    }
}
