﻿using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
    public class Scanline : RenderWindow
    {

        private Scanline(int width, int height, DisplayDevice device) 
            : base(width, height, device)
        {

        }

        int VertexShaderObject, FragmentShaderObject, ProgramObject;
        int PointVertexShaderObject, PointFragmentShaderObject, PointProgramObject;
        int Step;
        int Length = 10;
        bool Rows = false;

        protected void LoadPoints()
        {


            using (StreamReader sr = new StreamReader("Data/Shaders/Points_VS.glsl"))
            {
                PointVertexShaderObject = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(PointVertexShaderObject, sr.ReadToEnd());
                GL.CompileShader(PointVertexShaderObject);
            }

            string LogInfo;
            GL.GetShaderInfoLog(PointVertexShaderObject, out LogInfo);
            if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
                throw new Exception(LogInfo);

            using (StreamReader sr = new StreamReader("Data/Shaders/Points_FS.glsl"))
            {
                PointFragmentShaderObject = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(PointFragmentShaderObject, sr.ReadToEnd());
                GL.CompileShader(PointFragmentShaderObject);
            }

            GL.GetShaderInfoLog(PointFragmentShaderObject, out LogInfo);
            if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
                throw new Exception(LogInfo);

            PointProgramObject = GL.CreateProgram();
            GL.AttachShader(PointProgramObject, PointVertexShaderObject);
            GL.AttachShader(PointProgramObject, PointFragmentShaderObject);
            GL.LinkProgram(PointProgramObject);

            GL.UseProgram(PointProgramObject);

            GL.DeleteShader(PointVertexShaderObject);
            GL.DeleteShader(PointFragmentShaderObject);
        }

        protected override void Load()
        {
            //this.VSync = VSyncMode.On;

            GL.Disable(EnableCap.Dither);
            GL.ClearColor(System.Drawing.Color.Black);

            using (StreamReader sr = new StreamReader("Data/Shaders/Scanline_VS.glsl"))
            {
                VertexShaderObject = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(VertexShaderObject, sr.ReadToEnd());
                GL.CompileShader(VertexShaderObject);
            }

            string LogInfo;
            GL.GetShaderInfoLog(VertexShaderObject, out LogInfo);
            if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
                throw new Exception(LogInfo);

            using (StreamReader sr = new StreamReader("Data/Shaders/Scanline_FS.glsl"))
            {
                FragmentShaderObject = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(FragmentShaderObject, sr.ReadToEnd());
                GL.CompileShader(FragmentShaderObject);
            }

            GL.GetShaderInfoLog(FragmentShaderObject, out LogInfo);
            if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
                throw new Exception(LogInfo);

            ProgramObject = GL.CreateProgram();
            GL.AttachShader(ProgramObject, VertexShaderObject);
            GL.AttachShader(ProgramObject, FragmentShaderObject);
            GL.LinkProgram(ProgramObject);

            GL.UseProgram(ProgramObject);

            GL.DeleteShader(VertexShaderObject);
            GL.DeleteShader(FragmentShaderObject);

            LoadPoints();
        }

        protected override void Unload()
        {
            if (ProgramObject != 0)
                GL.DeleteProgram(ProgramObject);
            if (PointProgramObject != 0)
                GL.DeleteProgram(PointProgramObject);
        }

        public void RenderWhite()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(PointProgramObject);

            GL.Begin(BeginMode.Quads);
            {
                GL.Vertex2(-1.0f, -1.0f);
                GL.Vertex2(1.0f, -1.0f);
                GL.Vertex2(1.0f, 1.0f);
                GL.Vertex2(-1.0f, 1.0f);
            }
            GL.End();
            SwapBuffers();
        }

        public void RenderPoints(System.Drawing.PointF[] points, float size)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.UseProgram(PointProgramObject);

            GL.Begin(BeginMode.Quads);
            {
                Func<float, float> t = (x) => x * 2f - 1f;
                Func<float, float> tx = (x) => t(x / (float)Width);
                Func<float, float> ty = (y) => t(y / (float)Height);
                foreach (var p in points.Select(row => new System.Drawing.PointF(tx(row.X), ty(row.Y))))
                {
                    float xscale = size / Width;
                    float yscale = size / Height;
                    GL.Vertex2(p.X - xscale, p.Y - yscale);
                    GL.Vertex2(p.X + xscale, p.Y - yscale);
                    GL.Vertex2(p.X + xscale, p.Y + yscale);
                    GL.Vertex2(p.X - xscale, p.Y + yscale);
                }
            }
            GL.End();
            SwapBuffers();
        }

        public override void RenderFrame()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(ProgramObject);

            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "WIDTH"), Width);
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "HEIGHT"), Height);
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "STEP"), Step);
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "LENGTH"), Length);
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "ROWS"), Rows ? 1 : 0);

            GL.Begin(BeginMode.Quads);
            {
                GL.Vertex2(-1.0f, -1.0f);
                GL.Vertex2(1.0f, -1.0f);
                GL.Vertex2(1.0f, 1.0f);
                GL.Vertex2(-1.0f, 1.0f);
            }
            GL.End();
            SwapBuffers();
        }

        public static Scanline Make()
        {
            var display = DisplayDevice.AvailableDisplays.Where(row => !row.IsPrimary).Skip(0).First();
            var primary = DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            //var sc = new Scanline(display.Width, display.Height, display);
            var sc = new Scanline(display.Width, display.Height, primary);
            sc.Open(display);
            //sc.Visible = true;
            //sc.WindowBorder = WindowBorder.Hidden;
            //var id = GetForegroundWindow();
            //SetWindowPos(id, (IntPtr)SpecialWindowHandles.HWND_TOPMOST, display.Bounds.Left - 800,
            // display.Bounds.Top, display.Bounds.Width + 16, display.Bounds.Height + 38,
            // SetWindowPosFlags.SWP_SHOWWINDOW);
            //sc.WindowState = WindowState.Fullscreen;
            
            //var id = GetForegroundWindow();
            //MoveWindow(id, display.Bounds.Left, 0, display.Bounds.Width, display.Bounds.Height, true);
            return sc;
        }

        public void Update(out int? x, out int? y)
        {
            Step++;
            if (Step * Length > (!Rows ? Width : Height))
            {
                Step = 0;
                Rows = !Rows;
            }
            y = Rows ? (int?)(Step * Length) : null;
            x = !Rows ? (int?)(Step * Length) : null;
        }

        public void Set(int Step, int Length, bool Rows)
        {
            this.Step = Step;
            this.Length = Length;
            this.Rows = Rows;
        }

        public static void StartScanline()
        {
            using (var scan = Make())
            {
                for (var i = 0; i < 10000; i++)
                {
                    scan.RenderFrame();
                    int? x, y;
                    scan.Update(out x, out y);
                }
                scan.Unload();
            }
        }

    }
}