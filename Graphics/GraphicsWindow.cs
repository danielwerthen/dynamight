using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
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
		public GraphicsWindow(int x, int y, int width, int height, DisplayDevice display, string title = "Window")
			: base(x, y, width, height, title, GameWindowFlags.Default, GraphicsMode.Default, display)
		{
			this.Visible = true;
			this.display = display;
			glContext = new GraphicsContext(GraphicsMode.Default, WindowInfo, 2, 0, GraphicsContextFlags.Default);
			glContext.MakeCurrent(WindowInfo);
			(glContext as IGraphicsContextInternal).LoadAll();
		}

		protected void MakeCurrent()
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

		public int CreateShader(ShaderType type, string shader)
		{
			MakeCurrent();
			int ptr = GL.CreateShader(type);
			GL.ShaderSource(ptr, shader);
			GL.CompileShader(ptr);

			string LogInfo;
			GL.GetShaderInfoLog(ptr, out LogInfo);
			if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
				throw new Exception(LogInfo);
			return ptr;
		}

		public int CreateProgram(int vs, int fs)
		{
			MakeCurrent();
			int ptr = GL.CreateProgram();
			GL.AttachShader(ptr, vs);
			GL.AttachShader(ptr, fs);
			GL.LinkProgram(ptr);
			
			return ptr;
		}


		public void UpdateTexture(Bitmap bitmap, int texture)
		{
			MakeCurrent();
			TextureTarget Target = TextureTarget.Texture2D;

			GL.BindTexture(Target, texture);

			BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(Target, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			GL.Finish();
			bitmap.UnlockBits(data);
			if (GL.GetError() != ErrorCode.NoError)
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
			GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 1.0);
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
					IWindowInfo ii = ((OpenTK.NativeWindow)this).WindowInfo;
					object inf = ((OpenTK.NativeWindow)this).WindowInfo;
					PropertyInfo parentprop = (inf.GetType()).GetProperty("Parent");
					IWindowInfo parent = ((IWindowInfo)parentprop.GetValue(ii, null));
					PropertyInfo pi = (inf.GetType()).GetProperty("WindowHandle");
					IntPtr hnd = ((IntPtr)pi.GetValue(parent, null));
					SetWindowPos(hnd, IntPtr.Zero, display.Bounds.Left,
					 display.Bounds.Top, display.Bounds.Width, display.Bounds.Height - 1, //Strange but necessary
					 SetWindowPosFlags.SWP_NOMOVE);
					SetWindowPos(hnd, (IntPtr)SpecialWindowHandles.HWND_TOPMOST, display.Bounds.Left,
					 display.Bounds.Top, display.Bounds.Width, display.Bounds.Height,
					 SetWindowPosFlags.SWP_SHOWWINDOW);
					this.WindowState = WindowState.Fullscreen;
				}
			}
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
