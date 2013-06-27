using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics
{
	public class PhaseModProgram : Program
	{
		public PhaseModProgram()
		{
		}
		const string VERTEXSHADER =
@"void main(void)
{
  gl_Position = ftransform(); // gl_ModelViewProjectionMatrix * gl_Vertex;
}";
		const string FRAGMENTSHADER =
@"
uniform vec4 COLOR;
uniform int WIDTH;
uniform int HEIGHT;
uniform int VERTICAL;
uniform int STEP;
uniform float PHASE;

void main(void)
{
  float PI = 3.14159265358979323846264;
	vec2 coord = vec2(gl_FragCoord.x / float(WIDTH),1. - gl_FragCoord.y / float(HEIGHT));
  float dt = 0.0;
  if (VERTICAL > 0)
	{
    dt = coord.x;
	}
	else
	{
		dt = coord.y;
	}
  float i = cos((dt - 0.5 / float(STEP)) * PI * float(STEP) + PHASE);
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

		int step;
		int vertical;
		float phase;
		Color color;

		public void SetParams(int step, float phase, bool vertical, Color color)
		{
			this.step = step;
			this.phase = phase;
			this.vertical = vertical ? 1 : 0;
			this.color = color;
		}

		public override void Render()
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);

			GL.UseProgram(program);

			GL.Uniform1(GL.GetUniformLocation(program, "WIDTH"), parent.Width);
			GL.Uniform1(GL.GetUniformLocation(program, "HEIGHT"), parent.Height);
			GL.Uniform1(GL.GetUniformLocation(program, "STEP"), step);
			GL.Uniform1(GL.GetUniformLocation(program, "VERTICAL"), vertical);
			GL.Uniform1(GL.GetUniformLocation(program, "PHASE"), phase);
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
