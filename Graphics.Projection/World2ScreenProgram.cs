using Dynamight.ImageProcessing.CameraCalibration;
using Graphics.Geometry;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
	public abstract class World2ScreenProgram : Program
	{

        public static float Rad(float x, float y)
        {
            return (float)(Math.Pow(x, 2.0) + Math.Pow(y, 2.0));
        }

        public static float Dist(float r2, Matrix4 K)
        {
            
            return (float)((1.0 + K.M11 * r2 + K.M12 * Math.Pow(r2, 2.0) + K.M21 * Math.Pow(r2, 3.0))
                / (1.0 + K.M22 * r2 + K.M23 * Math.Pow(r2, 2.0) + K.M24 * Math.Pow(r2, 3.0)));
        }

        public static Vector4 Distort(Vector4 V, Matrix4 K, Vector2 F, Vector2 C, int WIDTH, int HEIGHT)
        {
          float xp = V.X;
          float yp = V.Y;
          float z = V.Z;

          if (V.Z == 0.0f)
          {
            xp = xp / 0.0001f;
            yp = yp / 0.0001f;
            z = 0.0f;
          } else
          {
            xp = xp / V.Z;
            yp = yp / V.Z;
            z = V.Z;
          }
          float r2 = Rad(xp, yp);
          float xpp = (float)(xp * Dist(r2, K) + 2.0 * K.M13 * xp * yp + K.M14 * (r2 + 2.0 * Math.Pow(xp, 2.0)));
          float ypp = (float)(yp * Dist(r2, K) + K.M13 * (r2 + 2.0 * Math.Pow(yp, 2.0)) + 2.0 * K.M14 * xp * yp);
          float u = F.X * xpp + C.X;
          float v = F.Y * ypp + C.Y;

          var r = new Vector4d(2.0 * (u / (float)(WIDTH)) - 1.0, 2.0 * (((float)(HEIGHT) - v) / (float)(HEIGHT)) - 1.0, 0.5, 1.0);
          return new Vector4((float)r.X, (float)r.Y, (float)r.Z, (float)r.Y);
        }

		protected const string VSDistort =
@"
uniform int WIDTH;
uniform int HEIGHT;
uniform vec2 F;
uniform vec2 C;
uniform mat4 K;

float rad(float x, float y)
{
    return pow(x,2.0) + pow(y,2.0);
}

float dist(float r2)
{
    return (1.0 + K[0][0] * r2 + K[1][0] * pow(r2, 2.0) + K[0][1] * pow(r2, 3.0))
        /  (1.0 + K[1][1] * r2 + K[2][1] * pow(r2, 2.0) + K[3][1] * pow(r2, 3.0));
}

vec4 distort(vec4 V)
{
  float xp = V.x;
  float yp = V.y;
  float z = V.z;

  if (V.z == 0.)
  {
    xp = xp / 0.0001;
    yp = yp / 0.0001;
    z = 0.;
  } else
  {
    xp = xp / V.z;
    yp = yp / V.z;
    z = 0.5;
  }
  float r2 = rad(xp, yp);
  float xpp = xp * dist(r2) + 2.0 * K[2][0] * xp * yp + K[3][0] * (r2 + 2.0 * pow(xp, 2.0));
  float ypp = yp * dist(r2) + K[2][0] * (r2 + 2.0 * pow(yp, 2.0)) + 2.0 * K[3][0] * xp * yp;
  float u = F.x * xpp + C.x;
  float v = F.y * ypp + C.y;

  return vec4(2.0 * (u / float(WIDTH)) - 1.0, 2.0 * ((float(HEIGHT) - v) / float(HEIGHT)) - 1.0, 0.5, 1.0);
}
";

		protected const string VSMain =
@"
void main(void)
{
  gl_FrontColor = gl_Color;
  gl_TexCoord[0] = gl_MultiTexCoord0; 
  vec4 V = gl_ModelViewProjectionMatrix * gl_Vertex;
  gl_Position = distort(V);
}
";

		protected const string FSMain =
@"
uniform sampler2D COLORTABLE;

void main(void)
{
  //gl_FragColor = texture2D(COLORTABLE, gl_TexCoord[0].st);
  //gl_FragColor = gl_FragColor.a * gl_FragColor;
  gl_FragColor = gl_Color;
}
";

		protected virtual IEnumerable<string> VertexShaderParts()
		{
			yield return VSDistort;
			yield return VSMain;
		}

		protected virtual IEnumerable<string> FragmentShaderParts()
		{
			yield return FSMain;
		}


		Shape shape = new Checkerboard(new Size(7, 4), 0.05f);
		Vector2 F, C;
		Matrix4 K;
        protected CalibrationResult calib;

        protected Vector4 Transform(float[] v)
        {
            var rt = calib.Extrinsic.ExtrinsicMatrix.Data;
            return new Vector4(
                (float)(v[0] * rt[0, 0] + v[1] * rt[0, 1] + v[2] * rt[0, 2] + v[3] * rt[0, 3]),
                (float)(v[0] * rt[1, 0] + v[1] * rt[1, 1] + v[2] * rt[1, 2] + v[3] * rt[1, 3]),
                (float)(v[0] * rt[2, 0] + v[1] * rt[2, 1] + v[2] * rt[2, 2] + v[3] * rt[2, 3]),
                (float)(v[3])
            );
        }

        protected Vector4 Distort(Vector4 v)
        {
            return Distort(v, K, F, C, parent.Width, parent.Height);
        }

		public void SetProjection(CalibrationResult calib)
		{
            this.calib = calib;
			parent.MakeCurrent();

			var Width = parent.Width;
			var Height = parent.Height;
			GL.Viewport(0, 0, Width, Height);
			var rt = calib.Extrinsic.ExtrinsicMatrix.Data;
			var extrin = new OpenTK.Matrix4d(
					rt[0, 0], rt[1, 0], rt[2, 0], 0,
					rt[0, 1], rt[1, 1], rt[2, 1], 0,
					rt[0, 2], rt[1, 2], rt[2, 2], 0,
					rt[0, 3], rt[1, 3], rt[2, 3], 1);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref extrin);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			var intrin = calib.Intrinsic.IntrinsicMatrix.Data;
			var dist = calib.Intrinsic.DistortionCoeffs.Data;
			F = new Vector2((float)intrin[0, 0], (float)intrin[1, 1]);
			C = new Vector2((float)intrin[0, 2], (float)intrin[1, 2]);
			K = new Matrix4(
					(float)dist[0, 0], (float)dist[1, 0], (float)dist[2, 0], (float)dist[3, 0],
					(float)dist[4, 0], (float)dist[5, 0], (float)dist[6, 0], (float)dist[7, 0],
					0, 0, 0, 0,
					0, 0, 0, 0);
		}

		protected virtual string GetVertexShader()
		{
			return string.Join("\n", VertexShaderParts());
		}

		protected virtual string GetFragmentShader()
		{
			return string.Join("\n", FragmentShaderParts());
		}

		protected virtual string GetGeometryShader()
		{
			return null;
		}

		int texture, program;
		TextureUnit unit = TextureUnit.Texture0;
		ProgramWindow parent;
		Bitmap bitmap;
		public override void Load(ProgramWindow parent)
		{
			this.parent = parent;
			GL.Disable(EnableCap.Dither);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.DepthTest);
			GL.ClearColor(System.Drawing.Color.Black);
			var vs = parent.CreateShader(ShaderType.VertexShader, GetVertexShader());
			var fs = parent.CreateShader(ShaderType.FragmentShader, GetFragmentShader());
			var gss = GetGeometryShader();
			int? gs = null;
			if (gss != null)
				gs = parent.CreateShader(ShaderType.GeometryShader, gss);
			program = parent.CreateProgram(vs, fs, gs);
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
			parent.MakeCurrent();
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			GL.MatrixMode(MatrixMode.Projection);
			GL.Ortho(0.0, 1.0, 0.0, 1.0, -100.0, 100.0);
			GL.Disable(EnableCap.DepthTest);

			if (program != 0)
				GL.DeleteProgram(program);
			if (texture != 0)
				GL.DeleteTexture(texture);

		}

		public override void Render()
		{

			GL.Clear(ClearBufferMask.ColorBufferBit |
							 ClearBufferMask.DepthBufferBit);

			GL.UseProgram(program);
			GL.Uniform1(GL.GetUniformLocation(program, "COLORTABLE"), unit - TextureUnit.Texture0);
			GL.Uniform1(GL.GetUniformLocation(program, "WIDTH"), parent.Width);
			GL.Uniform1(GL.GetUniformLocation(program, "HEIGHT"), parent.Height);
			GL.Uniform2(GL.GetUniformLocation(program, "F"), F);
			GL.Uniform2(GL.GetUniformLocation(program, "C"), C);
			GL.UniformMatrix4(GL.GetUniformLocation(program, "K"), true, ref K);



		}
	}

}
