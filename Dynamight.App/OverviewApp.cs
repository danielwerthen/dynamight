using Graphics;
using Graphics.Projection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class OverviewApp
    {
        public static void Run(string[] args)
        {
            var overview = new ProgramWindow(750, 50, 640, 480);
            overview.Load();
            overview.ResizeGraphics();
            OverviewProgram program = new OverviewProgram();
            overview.SetProgram(program);
            while (true)
            {
                overview.ProcessEvents();
                overview.RenderFrame();
            }
        }
    }
}
