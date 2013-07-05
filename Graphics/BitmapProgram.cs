using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
	public class BitmapProgram : Program
	{
		Bitmap bitmap;
		public BitmapProgram(Bitmap bitmap = null)
		{
			this.bitmap = bitmap;
		}
		const string VERTEXSHADER =
@"
void main(void)
{
  gl_Position = ftransform(); // gl_ModelViewProjectionMatrix * gl_Vertex;
  //gl_TexCoord[0] = gl_MultiTexCoord0; 
}";
		const string FRAGMENTSHADER =
@"
uniform sampler2D COLORTABLE;
uniform int WIDTH;
uniform int HEIGHT;

void main(void)
{
  gl_FragColor = texture2D( COLORTABLE, vec2(gl_FragCoord.x / float(WIDTH),1. - gl_FragCoord.y / float(HEIGHT)));
  //gl_FragColor = texture2D(COLORTABLE, gl_TexCoord[0].st);
}
";
		int texture, program;
		TextureUnit unit = TextureUnit.Texture0;
		ProgramWindow parent;
		public override void Load(ProgramWindow parent)
		{
			this.parent = parent;
			GL.Disable(EnableCap.Dither);
			GL.Enable(EnableCap.Texture2D);
			GL.ClearColor(System.Drawing.Color.Black);
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
