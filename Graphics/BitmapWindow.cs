using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
    public class BitmapWindow : RenderWindow
    {

        private BitmapWindow(int width, int height, DisplayDevice device) 
            : base(width, height, device)
        {

        }

        int VertexShaderObject, FragmentShaderObject, ProgramObject, TextureObject;

        const string VERTEXSHADER = 
@"void main(void)
{
  gl_Position = ftransform(); // gl_ModelViewProjectionMatrix * gl_Vertex;
}";
        const string FRAGMENTSHADER =
@"
uniform sampler2D COLORTABLE;
uniform int WIDTH;
uniform int HEIGHT;

void main(void)
{
  gl_FragColor = texture2D( COLORTABLE, vec2(gl_FragCoord.x / float(WIDTH),gl_FragCoord.y / float(HEIGHT)));
}
";

        public override void Load()
        {
            MakeCurrent();
            base.Load();

            GL.Disable(EnableCap.Dither);
            GL.Enable(EnableCap.Texture2D);
            GL.ClearColor(System.Drawing.Color.Black);

            //VertexShaderObject = GL.CreateShader(ShaderType.VertexShader);
            //GL.ShaderSource(VertexShaderObject, VERTEXSHADER);
            //GL.CompileShader(VertexShaderObject);

            //string LogInfo;
            //GL.GetShaderInfoLog(VertexShaderObject, out LogInfo);
            //if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
            //    throw new Exception(LogInfo);

            //FragmentShaderObject = GL.CreateShader(ShaderType.FragmentShader);
            //GL.ShaderSource(FragmentShaderObject, FRAGMENTSHADER);
            //GL.CompileShader(FragmentShaderObject);

            //GL.GetShaderInfoLog(FragmentShaderObject, out LogInfo);
            //if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
            //    throw new Exception(LogInfo);

            //ProgramObject = GL.CreateProgram();
            //GL.AttachShader(ProgramObject, VertexShaderObject);
            //GL.AttachShader(ProgramObject, FragmentShaderObject);
            //GL.LinkProgram(ProgramObject);

            //GL.UseProgram(ProgramObject);

            //GL.DeleteShader(VertexShaderObject);
            //GL.DeleteShader(FragmentShaderObject);

            using (Bitmap bitmap = new Bitmap(Width, Height))
            {
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        double ii = (double)x / (double)Width;
                        ii = ii * 255;
                        byte z = (byte)ii;
                        double ii2 = (double)y / (double)Height;
                        ii2 = ii2 * 255;
                        byte z2 = (byte)ii2;
                        bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(z, z2, 0));
                    }
                TextureObject = LoadTexture(bitmap);
            }
        }

        public override void Unload()
        {
            MakeCurrent();
            if (ProgramObject != 0)
                GL.DeleteProgram(ProgramObject);
            if (TextureObject != 0)
                GL.DeleteTextures(1, ref TextureObject);
        }

        public void LoadBitmap(Bitmap bitmap)
        {
            MakeCurrent();
            UpdateTexture(bitmap, TextureObject);
        }

        public override void RenderFrame()
        {
            MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BindTexture(TextureTarget.Texture2D, TextureObject);

            GL.Begin(BeginMode.Quads);

            GL.TexCoord2(0.0f, 1.0f);
            GL.Vertex2(-1.0f, -1.0f);
            GL.TexCoord2(1.0f, 1.0f);
            GL.Vertex2(1.0f, -1.0f);
            GL.TexCoord2(1.0f, 0.0f);
            GL.Vertex2(1.0f, 1.0f);
            GL.TexCoord2(0.0f, 0.0f);
            GL.Vertex2(-1.0f, 1.0f);

            GL.End();

            SwapBuffers();
        }

        public void RenderFrame2()
        {
            MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(ProgramObject);

            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "COLORTABLE"), TextureObject);
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "WIDTH"), Width);
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "HEIGHT"), Height);

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

        public static BitmapWindow Make()
        {
            var display = DisplayDevice.AvailableDisplays.First(row => row.IsPrimary);
            var secondary = DisplayDevice.AvailableDisplays.First(row => !row.IsPrimary);
            //var sc = new Scanline(display.Width, display.Height, display);
            var window = new BitmapWindow(secondary.Width / 2, secondary.Height / 2, display);
            window.Visible = true;
            window.Load();
            window.ResizeGraphics();
            return window;
        }
    }
}
