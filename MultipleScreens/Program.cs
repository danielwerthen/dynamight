using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultipleScreens
{
    class Program
    {
        static void Main(string[] args)
        {
            DisplayDevice device1 = DisplayDevice.AvailableDisplays.First();
            DisplayDevice device2 = DisplayDevice.AvailableDisplays.Skip(1).First();
            Action<NativeWindow> activate = (window) =>
                {
                    window.Visible = true;
                };
            NativeWindow window1 = new NativeWindow(500, 500, "Window1", GameWindowFlags.Default, GraphicsMode.Default, device1);
            NativeWindow window2 = new NativeWindow(500, 500, "Window2", GameWindowFlags.Default, GraphicsMode.Default, device2);
            activate(window1);
            activate(window2);

            var context1 = new GraphicsContext(GraphicsMode.Default, window1.WindowInfo, 2, 0, GraphicsContextFlags.Default);
            context1.MakeCurrent(window1.WindowInfo);
            context1.LoadAll();
            GL.Disable(EnableCap.Dither);
            GL.ClearColor(System.Drawing.Color.Red);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            context1.SwapBuffers();
            var context2 = new GraphicsContext(GraphicsMode.Default, window2.WindowInfo, 2, 0, GraphicsContextFlags.Default);
            context2.MakeCurrent(window2.WindowInfo);
            context2.LoadAll();
            GL.Disable(EnableCap.Dither);
            GL.ClearColor(System.Drawing.Color.Blue);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            context2.SwapBuffers();

            GL.Disable(EnableCap.Dither);
            GL.ClearColor(System.Drawing.Color.Yellow);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            context1.SwapBuffers();
            Console.ReadLine();
        }
    }
}
