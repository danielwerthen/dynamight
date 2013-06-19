using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
    public class RenderWindow : NativeWindow
    {
        private GraphicsContext glContext;
        public RenderWindow(int width, int height, DisplayDevice device) 
            : base(width, height, "RenderOutput", GameWindowFlags.Default, GraphicsMode.Default, device == null ? DisplayDevice.AvailableDisplays.FirstOrDefault(row => row.IsPrimary) : device)
        {
            try
            {
                glContext = new GraphicsContext(GraphicsMode.Default , WindowInfo, 2, 0, GraphicsContextFlags.Default);
                glContext.MakeCurrent(WindowInfo);
                (glContext as IGraphicsContextInternal).LoadAll();


                //glWindow.WindowInfoChanged += delegate(object sender, EventArgs e) { OnWindowInfoChangedInternal(e); };
            }
            catch (Exception e)
            {
                base.Dispose();
                throw;
            }
        }

        public System.Drawing.Size Size
        {
            get { return new System.Drawing.Size(Width, Height); }
        }


        protected virtual void Load()
        {
        }

        protected virtual void Unload()
        {

        }

        protected virtual void ResizeGraphics()
        {
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

        public virtual void Open(DisplayDevice display)
        {
            this.Visible = true;
            this.WindowBorder = WindowBorder.Hidden;
            IWindowInfo ii = ((OpenTK.NativeWindow)this).WindowInfo;
            object inf = ((OpenTK.NativeWindow)this).WindowInfo;
            PropertyInfo pi = (inf.GetType()).GetProperty("WindowHandle");
            IntPtr hnd = ((IntPtr)pi.GetValue(ii, null));
            var id = GetForegroundWindow();
            SetWindowPos(id, (IntPtr)SpecialWindowHandles.HWND_TOPMOST, display.Bounds.Left - 500,
             display.Bounds.Top, display.Bounds.Width, display.Bounds.Height,
             SetWindowPosFlags.SWP_SHOWWINDOW);
            this.WindowState = WindowState.Fullscreen;
            this.Load();
            this.ResizeGraphics();
        }

#region DllImports
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);


        /// <summary>
        ///     Special window handles
        /// </summary>
        public enum SpecialWindowHandles
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
        public enum SetWindowPosFlags : uint
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
