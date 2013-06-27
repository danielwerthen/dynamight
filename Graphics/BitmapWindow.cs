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
	public class BitmapWindow : GraphicsWindow
	{
		public BitmapWindow(int x, int y, int width, int height, DisplayDevice device = null)
			: base(x, y, width, height, device == null ? DisplayDevice.AvailableDisplays.First() : device)
		{
		}
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
uniform vec2 TRANSLATE;
uniform vec2 SCALE;


void main(void)
{
  vec2 coord = vec2(gl_FragCoord.x / float(WIDTH),1. - gl_FragCoord.y / float(HEIGHT));
  coord = vec2((coord.x - TRANSLATE.x) / SCALE.x, (coord.y + TRANSLATE.y) / SCALE.y );
  gl_FragColor = texture2D( COLORTABLE, coord);
}
";
		int texture, program;
		TextureUnit unit = TextureUnit.Texture0;
		public override void Load()
		{
			MakeCurrent();
			GL.Disable(EnableCap.Dither);
			GL.Enable(EnableCap.Texture2D);
			GL.ClearColor(System.Drawing.Color.Black);
			var vs = CreateShader(ShaderType.VertexShader, VERTEXSHADER);
			var fs = CreateShader(ShaderType.FragmentShader, FRAGMENTSHADER);
			program = CreateProgram(vs, fs);
			GL.DeleteShader(vs);
			GL.DeleteShader(fs);
			texture = LoadTexture(new System.Drawing.Bitmap(Width, Height), unit);
		}

		public void LoadBitmap(Bitmap bitmap)
		{
			UpdateTexture(bitmap, texture);
		}

        public void SetBounds(RectangleF rect)
        {
            var tc = new PointF(rect.Location.X, rect.Location.Y);
            bounds = new RectangleF(tc, new SizeF(rect.Size.Width, rect.Size.Height));
        }

        private RectangleF bounds = new RectangleF(0,0,1,1);
		public override void RenderFrame()
		{
			MakeCurrent();
			GL.Clear(ClearBufferMask.ColorBufferBit);

			GL.BindTexture(TextureTarget.Texture2D, texture);
			GL.UseProgram(program);

			GL.Uniform1(GL.GetUniformLocation(program, "COLORTABLE"), unit - TextureUnit.Texture0);
			GL.Uniform1(GL.GetUniformLocation(program, "WIDTH"), Width);
			GL.Uniform1(GL.GetUniformLocation(program, "HEIGHT"), Height);
            GL.Uniform2(GL.GetUniformLocation(program, "TRANSLATE"), new Vector2(bounds.X, bounds.Y));
            GL.Uniform2(GL.GetUniformLocation(program, "SCALE"), new Vector2(bounds.Width, bounds.Height));

			GL.Begin(BeginMode.Quads);

			GL.TexCoord2(0.0f, 1.0f);
            GL.Vertex2(bounds.Left, bounds.Bottom); //GL.Vertex2(0.0f, 0.0f);
			GL.TexCoord2(1.0f, 1.0f);
            GL.Vertex2(bounds.Right, bounds.Bottom); // GL.Vertex2(1.0f, 0.0f);
			GL.TexCoord2(1.0f, 0.0f);
            GL.Vertex2(bounds.Right, bounds.Top); // GL.Vertex2(1.0f, 1.0f);
			GL.TexCoord2(0.0f, 0.0f);
            GL.Vertex2(bounds.Left, bounds.Top); // GL.Vertex2(0.0f, 1.0f);

			GL.End();

			SwapBuffers();
		}

        public void DrawBitmap(Bitmap b)
        {
            LoadBitmap(b);
            RenderFrame();
        }
		public override void Unload()
		{
			MakeCurrent();
			if (program != 0)
				GL.DeleteProgram(program);
			if (texture != 0)
				GL.DeleteTexture(texture);
		}
	}
}
