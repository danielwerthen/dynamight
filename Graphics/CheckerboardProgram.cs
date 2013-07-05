using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
	public class CheckerboardProgram : Program
	{
		public CheckerboardProgram()
		{
		}
		const string VERTEXSHADER =
@"void main(void)
{
  gl_Position = ftransform(); // gl_ModelViewProjectionMatrix * gl_Vertex;
}";
		const string FRAGMENTSHADER =
@"
#extension GL_EXT_gpu_shader4 : enable
uniform int WIDTH;
uniform int HEIGHT;
uniform vec4 COLOR;

void main(void)
{
  float PI = 3.14159265358979323846264;
	vec2 coord = vec2((gl_FragCoord.x) / float(WIDTH),1. - (gl_FragCoord.y) / float(HEIGHT));
    float dx = coord.x * 2.0 * 4.0;
    float dy = coord.y * 2.0 * 2.5;
    float ix = float(int(floor(coord.x * 2.0 * 4.0)) % 2);
    float iy = float(int(floor(coord.y * 2.0 * 2.5)) % 2);
    float i = 0.0;
    if (iy > 0.0)
        i = ix;
    else
        i = 1.0 - ix;

  gl_FragColor = vec4(i * COLOR.x, i * COLOR.y, i * COLOR.z, COLOR.w);
}
";
		int texture, program;
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
		}

		public override void Unload()
		{
			if (program != 0)
				GL.DeleteProgram(program);
		}

        Color color = Color.White;

		public override void Render()
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);

			GL.UseProgram(program);

			GL.Uniform1(GL.GetUniformLocation(program, "WIDTH"), parent.Width);
            GL.Uniform1(GL.GetUniformLocation(program, "HEIGHT"), parent.Height);
			GL.Uniform4(GL.GetUniformLocation(program, "COLOR"), new OpenTK.Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, 1.0f));

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
		}
	}
}
