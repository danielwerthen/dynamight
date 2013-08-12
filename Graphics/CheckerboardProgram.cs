using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
	public class CheckerboardProgram : BitmapProgram
	{
		Bitmap bitmap;
		public CheckerboardProgram(Bitmap bitmap = null)
		{
			this.bitmap = bitmap;
		}
		const string VERTEXSHADER =
@"
void main(void)
{
  gl_Position = ftransform(); // gl_ModelViewProjectionMatrix * gl_Vertex;
  gl_TexCoord[0] = gl_MultiTexCoord0; 
}";
		const string FRAGMENTSHADER =
@"
uniform sampler2D COLORTABLE;
uniform int WIDTH;
uniform int HEIGHT;

void main(void)
{
  gl_FragColor = texture2D(COLORTABLE, gl_TexCoord[0].st);
}
";

		double rotx, roty, rotz,
			scalex, scaley, offsetx, offsety;
        public void SetTransforms(double rotx, double roty, double rotz, double scale, double offsetx, double offsety)
        {
            SetTransforms(rotx, roty, rotz, scale, scale, offsetx, offsety);
        }
		public void SetTransforms(double rotx, double roty, double rotz, double scalex, double scaley, double offsetx, double offsety)
		{
			parent.MakeCurrent();
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			var rotX = OpenTK.Matrix4d.CreateRotationX(rotx);
			var rotY = OpenTK.Matrix4d.CreateRotationY(roty);
			var rotZ = OpenTK.Matrix4d.CreateRotationZ(rotz);
			var sm = OpenTK.Matrix4d.Scale(scalex, scaley, 1);
			var offset = OpenTK.Matrix4d.CreateTranslation(offsetx, offsety, 0);
			var tran = OpenTK.Matrix4d.CreateTranslation(-0.5, -0.5, 0);
			var trani = OpenTK.Matrix4d.CreateTranslation(0.5, 0.5, 0);
			this.rotx = rotx;
			this.roty = roty;
			this.rotz = rotz;
            this.scalex = scalex;
            this.scaley = scaley;
			this.offsetx = offsetx;
			this.offsety = offsety;
			var mat = tran * rotX * rotY * rotZ * sm * offset * trani;
			GL.LoadMatrix(ref mat);
		}

		Size size;
		public void SetSize(int cols, int rows)
		{
            if (cols != size.Width || rows != size.Height)
            {
                size = new Size(cols, rows);
                Draw().All((x, y) =>
                {
                    double ix = Math.Floor(x * cols) % 2;
                    double iy = Math.Floor(y * rows) % 2;
                    double i = iy > 0 ? ix : 1 - ix;
                    i = 1 - i;
                    return Color.FromArgb((int)(i * 255), (int)(i * 255), (int)(i * 255));
                }, true).Finish();
            }
		}

		private IEnumerable<PointF> GetLocalCorners()
		{
			var dx = (1.0 / ((double)size.Width));
			var dy = (1.0 / ((double)size.Height));
			for (int iy = 1; iy < size.Height; iy++)
			{
				for (int ix = 1; ix < size.Width; ix++)
				{
					yield return new PointF((float)(dx * ix), (float)((dy * iy)));
				}
			}
		}

		public IEnumerable<PointF> GetCorners()
		{
			var rotX = OpenTK.Matrix4d.CreateRotationX(rotx);
			var rotY = OpenTK.Matrix4d.CreateRotationY(-roty);
			var rotZ = OpenTK.Matrix4d.CreateRotationZ(-rotz);
			var sm = OpenTK.Matrix4d.Scale(scalex, scaley, 1);
			var offset = OpenTK.Matrix4d.CreateTranslation(offsetx, offsety, 0);
			var tran = OpenTK.Matrix4d.CreateTranslation(-0.5, -0.5, 0);
			var trani = OpenTK.Matrix4d.CreateTranslation(0.5, 0.5, 0);
			var mat = tran * rotX * rotY * rotZ * sm * offset * trani;
			return GetLocalCorners()
				.Select(lp =>
					new PointF((float)(mat.Column0.X * lp.X + mat.Column0.Y * lp.Y + mat.Column0.W) * (float)bitmap.Width,
						(float)(mat.Column1.X * lp.X + mat.Column1.Y * lp.Y + mat.Column1.W) * (float)bitmap.Height));
		}

		int texture, program;
		TextureUnit unit = TextureUnit.Texture0;
		ProgramWindow parent;
		public override void Load(ProgramWindow parent)
		{
			this.parent = parent;
			GL.Disable(EnableCap.Dither);
			GL.Enable(EnableCap.Texture2D);
			GL.ClearColor(System.Drawing.Color.White);
			var vs = parent.CreateShader(ShaderType.VertexShader, VERTEXSHADER);
			var fs = parent.CreateShader(ShaderType.FragmentShader, FRAGMENTSHADER);
			program = parent.CreateProgram(vs, fs);
			GL.DeleteShader(vs);
			GL.DeleteShader(fs);
			this.bitmap = this.bitmap ?? new System.Drawing.Bitmap(parent.Width, parent.Height);
			texture = parent.LoadTexture(this.bitmap, unit);
		}

		public QuickDraw Draw()
		{
			return QuickDraw.Start(this.bitmap, () => LoadBitmap(this.bitmap));
		}

		public void LoadBitmap(Bitmap bitmap)
		{
			if (parent == null)
				throw new Exception("Can not load bitmap since the program hasn't been activated yet.");
			parent.UpdateTexture(bitmap, texture);
		}

		public override void Unload()
		{
            this.SetTransforms(0, 0, 0, 1, 0, 0);
			if (program != 0)
				GL.DeleteProgram(program);
			if (texture != 0)
				GL.DeleteTexture(texture);
		}

		public override void Render()
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);

			GL.BindTexture(TextureTarget.Texture2D, texture);
			GL.UseProgram(program);

			GL.Uniform1(GL.GetUniformLocation(program, "COLORTABLE"), unit - TextureUnit.Texture0);
			GL.Uniform1(GL.GetUniformLocation(program, "WIDTH"), parent.Width);
			GL.Uniform1(GL.GetUniformLocation(program, "HEIGHT"), parent.Height);

			GL.Begin(BeginMode.Quads);

			GL.TexCoord2(0.0f, 1.0f);
			GL.Vertex2(0.0f, 0.0f);
			GL.TexCoord2(1.0f, 1.0f);
			GL.Vertex2(1.0f, 0.0f);
			GL.TexCoord2(1.0f, 0.0f);
			GL.Vertex2(1.0f, 1.0f);
			GL.TexCoord2(0.0f, 0.0f);
			GL.Vertex2(0.0f, 1.0f);

			GL.End();
		}
	}
}