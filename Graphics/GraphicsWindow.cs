﻿using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Platform;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
	public class GraphicsWindow : NativeWindow
	{
		private DisplayDevice display;
		private GraphicsContext glContext;
        public KeyboardDevice Keyboard;
        public MouseDevice Mouse;
		public GraphicsWindow(int x, int y, int width, int height, DisplayDevice display, string title = "Window")
			: base(x, y, width, height, title, GameWindowFlags.Default, GraphicsMode.Default, display)
		{
			this.Visible = true;
			this.display = display;
			glContext = new GraphicsContext(GraphicsMode.Default, WindowInfo);
			glContext.MakeCurrent(WindowInfo);
			(glContext as IGraphicsContextInternal).LoadAll();
			LoadWindowHandle();
            Keyboard = InputDriver.Keyboard[0];
            Mouse = InputDriver.Mouse[0];
		}

		public GraphicsWindow(int width, int height, DisplayDevice display, string title = "Window")
			: base(width, height, title, GameWindowFlags.Default, GraphicsMode.Default, display)
		{
			this.Visible = true;
			this.display = display;
			glContext = new GraphicsContext(GraphicsMode.Default, WindowInfo, 2, 0, GraphicsContextFlags.Default);
			glContext.MakeCurrent(WindowInfo);
			(glContext as IGraphicsContextInternal).LoadAll();
			LoadWindowHandle();
		}

		private void LoadWindowHandle()
		{
			IWindowInfo ii = ((OpenTK.NativeWindow)this).WindowInfo;
			object inf = ((OpenTK.NativeWindow)this).WindowInfo;
			PropertyInfo parentprop = (inf.GetType()).GetProperty("Parent");
			IWindowInfo parent = ((IWindowInfo)parentprop.GetValue(ii, null));
			PropertyInfo pi = (inf.GetType()).GetProperty("WindowHandle");
			WindowHandle = ((IntPtr)pi.GetValue(parent, null));
		}

		private IntPtr WindowHandle;

		public void MakeCurrent()
		{
			glContext.MakeCurrent(WindowInfo);
		}

		public virtual void Load()
		{
		}

		public int LoadTexture(Bitmap bitmap, TextureUnit unit)
		{
			MakeCurrent();
			int texture;
			GL.ActiveTexture(unit);
			GL.GenTextures(1, out texture);
			GL.BindTexture(TextureTarget.Texture2D, texture);

			BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
					OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

			bitmap.UnlockBits(data);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			return texture;
		}

		public void LoadFbo(int width, int height, out int ColorTexture, out int FboHandle)
		{
			MakeCurrent();
			GL.GenTextures(1, out ColorTexture);
			GL.BindTexture(TextureTarget.Texture2D, ColorTexture);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

			var error = GL.GetError();
			if (error != ErrorCode.NoError)
				throw new Exception(error.ToString());

			GL.BindTexture(TextureTarget.Texture2D, 0);

			GL.Ext.GenFramebuffers(1, out FboHandle);
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, FboHandle);
			GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, ColorTexture, 0);

			GL.Ext.CheckFramebufferStatus(FramebufferTarget.FramebufferExt);
		}

		public int CreateShader(ShaderType type, string shader)
		{
			MakeCurrent();
            return CreateShader2(type, shader);
		}

        public static int CreateShader2(ShaderType type, string shader)
        {
            int ptr = GL.CreateShader(type);
            GL.ShaderSource(ptr, shader);
            GL.CompileShader(ptr);

            string LogInfo;
            GL.GetShaderInfoLog(ptr, out LogInfo);
            if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
                throw new Exception(LogInfo);
            return ptr;
        }

		public int CreateProgram(int vs, int fs, int? gs = null)
		{
			MakeCurrent();
            return CreateProgram2(vs, fs, gs);
		}

        public static int CreateProgram2(int vs, int fs, int? gs = null)
        {
            int ptr = GL.CreateProgram();
            GL.AttachShader(ptr, vs);
            GL.AttachShader(ptr, fs);
            if (gs.HasValue)
                GL.AttachShader(ptr, gs.Value);
            GL.LinkProgram(ptr);

            return ptr;
        }

		public void UpdateTexture(Bitmap bitmap, int texture)
		{
			MakeCurrent();
			TextureTarget Target = TextureTarget.Texture2D;

			GL.BindTexture(Target, texture);

			BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            OpenTK.Graphics.OpenGL.PixelFormat format;
            if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                format = OpenTK.Graphics.OpenGL.PixelFormat.Luminance;
            else if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                format = OpenTK.Graphics.OpenGL.PixelFormat.Bgr;
            else
                format = OpenTK.Graphics.OpenGL.PixelFormat.Bgra;
			GL.TexImage2D(Target, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, format, PixelType.UnsignedByte, data.Scan0);
			GL.Finish();
			bitmap.UnlockBits(data);
            var error = GL.GetError();
			if (error != ErrorCode.NoError)
				throw new Exception("Error loading texture " + "bitmap");

		}

		public virtual void Unload()
		{

		}

		public virtual void ResizeGraphics()
		{
			MakeCurrent();
			GL.Viewport(0, 0, Width, Height);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			//GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 1.0);
            GL.Ortho(0.0, 1.0, 0.0, 1.0, -100.0, 100.0);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
		}

		public virtual void RenderFrame()
		{
		}

		protected void SwapBuffers()
		{
			MakeCurrent();
			EnsureUndisposed();
			glContext.SwapBuffers();
		}

		public override void Dispose()
		{
			try
			{
				if (glContext != null)
				{
					glContext.Dispose();
					glContext = null;
				}
			}
			finally
			{
				base.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		public bool Fullscreen
		{
			set
			{
				if (value)
				{
					this.WindowBorder = WindowBorder.Hidden;
                    int left = (int)(display.Bounds.Left / 1.5);
					SetWindowPos(WindowHandle, IntPtr.Zero, left,
					 display.Bounds.Top, display.Bounds.Width, display.Bounds.Height - 1, //Strange but necessary
					 SetWindowPosFlags.SWP_NOMOVE);
					SetWindowPos(WindowHandle, (IntPtr)SpecialWindowHandles.HWND_TOPMOST, left,
					 display.Bounds.Top, display.Bounds.Width, display.Bounds.Height,
					 SetWindowPosFlags.SWP_SHOWWINDOW);
					this.WindowState = WindowState.Fullscreen;
				}
			}
		}

		protected override void OnMove(EventArgs e)
		{
			base.OnMove(e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			glContext.Update(base.WindowInfo);
			this.ResizeGraphics();
			this.RenderFrame();
		}

		

		#region DllImports

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

		/// <summary>
		///     Special window handles
		/// </summary>
		private enum SpecialWindowHandles
		{
			// ReSharper disable InconsistentNaming
			/// <summary>
			///     Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
			/// </summary>
			HWND_TOP = 0,
			/// <summary>
			///     Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
			/// </summary>
			HWND_BOTTOM = 1,
			/// <summary>
			///     Places the window at the top of the Z order.
			/// </summary>
			HWND_TOPMOST = -1,
			/// <summary>
			///     Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
			/// </summary>
			HWND_NOTOPMOST = -2
			// ReSharper restore InconsistentNaming
		}

		[Flags]
		private enum SetWindowPosFlags : uint
		{
			// ReSharper disable InconsistentNaming

			/// <summary>
			///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.
			/// </summary>
			SWP_ASYNCWINDOWPOS = 0x4000,

			/// <summary>
			///     Prevents generation of the WM_SYNCPAINT message.
			/// </summary>
			SWP_DEFERERASE = 0x2000,

			/// <summary>
			///     Draws a frame (defined in the window's class description) around the window.
			/// </summary>
			SWP_DRAWFRAME = 0x0020,

			/// <summary>
			///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
			/// </summary>
			SWP_FRAMECHANGED = 0x0020,

			/// <summary>
			///     Hides the window.
			/// </summary>
			SWP_HIDEWINDOW = 0x0080,

			/// <summary>
			///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
			/// </summary>
			SWP_NOACTIVATE = 0x0010,

			/// <summary>
			///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
			/// </summary>
			SWP_NOCOPYBITS = 0x0100,

			/// <summary>
			///     Retains the current position (ignores X and Y parameters).
			/// </summary>
			SWP_NOMOVE = 0x0002,

			/// <summary>
			///     Does not change the owner window's position in the Z order.
			/// </summary>
			SWP_NOOWNERZORDER = 0x0200,

			/// <summary>
			///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
			/// </summary>
			SWP_NOREDRAW = 0x0008,

			/// <summary>
			///     Same as the SWP_NOOWNERZORDER flag.
			/// </summary>
			SWP_NOREPOSITION = 0x0200,

			/// <summary>
			///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
			/// </summary>
			SWP_NOSENDCHANGING = 0x0400,

			/// <summary>
			///     Retains the current size (ignores the cx and cy parameters).
			/// </summary>
			SWP_NOSIZE = 0x0001,

			/// <summary>
			///     Retains the current Z order (ignores the hWndInsertAfter parameter).
			/// </summary>
			SWP_NOZORDER = 0x0004,

			/// <summary>
			///     Displays the window.
			/// </summary>
			SWP_SHOWWINDOW = 0x0040,

			// ReSharper restore InconsistentNaming
		}
		#endregion
	}
}
