using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
	public class ProgramWindow : GraphicsWindow
	{
		public ProgramWindow(int x, int y, int width, int height, DisplayDevice device = null)
			: base(x, y, width, height, device == null ? DisplayDevice.AvailableDisplays.First() : device)
		{
		}

		public ProgramWindow(int width, int height, DisplayDevice device = null)
			: base(width, height, device == null ? DisplayDevice.AvailableDisplays.First() : device)
		{
		}

		public override void Load()
		{
			base.Load();
		}

		public override void Unload()
		{
			MakeCurrent();
			if (active != null)
				active.Unload();
		}

		Program active;

		public Program SetProgram(Program program)
		{
            if (program == active)
                return program;
			MakeCurrent();
			if (active != null)
				active.Unload();
			active = program;
			if (active != null)
				active.Load(this);
            return program;
		}

		public override void RenderFrame()
		{
			MakeCurrent();
			if (active != null)
				active.Render();
			SwapBuffers();
		}

        public static ProgramWindow OpenOnSecondary()
        {
            var display = DisplayDevice.AvailableDisplays.First(row => !row.IsPrimary);
            var window = new ProgramWindow((int)(display.Bounds.Left/ 1.5), display.Bounds.Top, display.Width, display.Height, display);
            window.Fullscreen = true;
            window.Load();
            window.ResizeGraphics();
            return window;
        }
	}

	public abstract class Program
	{
		public abstract void Load(ProgramWindow window);

		public abstract void Unload();

		public abstract void Render();
	}
}
