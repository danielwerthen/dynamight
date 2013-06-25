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

		public void SetProgram(Program program)
		{
			MakeCurrent();
			if (active != null)
				active.Unload();
			active = program;
			if (active != null)
				active.Load(this);
		}

		public override void RenderFrame()
		{
			MakeCurrent();
			if (active != null)
				active.Render();
			SwapBuffers();
		}


	}

	public abstract class Program
	{
		public abstract void Load(ProgramWindow window);

		public abstract void Unload();

		public abstract void Render();
	}
}
