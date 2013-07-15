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
    public class TransformativeProgram : Program
    {

        private float rad(float x, float y)
        {
            return (float)(Math.Pow(x, 2.0) + Math.Pow(y, 2.0));
        }

        private float dist(float r2, Matrix4 K)
        {
            return (float)((1.0 + K.M11 * r2 + K.M12 * Math.Pow(r2, 2.0) + K.M21 * Math.Pow(r2, 3))
                / (1.0 + K.M22 * r2 + K.M23 * Math.Pow(r2, 2.0) + K.M24 * Math.Pow(r2, 3)));
        }

        Vector4 tran(Vector4 V, Matrix4 K, Vector2 F, Vector2 C)
        {
            float xp = V.X / V.Z;
            float yp = V.Y / V.Z;
            float r2 = rad(xp, yp);
            float xpp = (float)(xp * dist(r2, K) + 2 * K.M13 * xp * yp + K.M14 * (r2 + 2 * Math.Pow(xp, 2)));
            float ypp = (float)(yp * dist(r2, K) + K.M13 * (r2 + 2 * Math.Pow(yp, 2)) + 2 * K.M14 * xp * yp);
            float u = F.X * xpp + C.X;
            float v = F.Y * ypp + C.Y;
            return new Vector4(u, v, V.Z, V.W);
        }



		const string VERTEXSHADER =
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
    return (1.0 + K[0][0] * r2 + K[1][0] * pow(r2, 2.0) + K[0][1] * pow(r2, 4.0))
        /  (1.0 + K[1][1] * r2 + K[2][1] * pow(r2, 2.0) + K[3][1] * pow(r2, 4.0));
}

void main(void)
{
  gl_FrontColor = gl_Color;
  gl_TexCoord[0] = gl_MultiTexCoord0; 
  vec4 V = gl_ModelViewProjectionMatrix * gl_Vertex;
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

  gl_Position = vec4(2.0 * (u / float(WIDTH)) - 1.0, 2.0 * ((float(HEIGHT) - v) / float(HEIGHT)) - 1.0, 0.5, 1.0);
}";

		const string FRAGMENTSHADER =
@"
uniform sampler2D COLORTABLE;
void main(void)
{
  //gl_FragColor = texture2D(COLORTABLE, gl_TexCoord[0].st);
  gl_FragColor = gl_Color;
}
";

        int vertex_buffer_object, color_buffer_object, element_buffer_object;

        Shape shape = new Checkerboard(new Size(7,4), 0.05f);
        Vector2 F, C;
        Matrix4 K;

        public void SetProjection(CalibrationResult calib)
        {
            parent.MakeCurrent();

            var Width = parent.Width;
            var Height = parent.Height;
            GL.Viewport(0,0, Width, Height);
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
                0,0,0,0,
                0,0,0,0);
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
			var vs = parent.CreateShader(ShaderType.VertexShader, VERTEXSHADER);
			var fs = parent.CreateShader(ShaderType.FragmentShader, FRAGMENTSHADER);
			program = parent.CreateProgram(vs, fs);
			GL.DeleteShader(vs);
			GL.DeleteShader(fs);
			this.bitmap = this.bitmap ?? new System.Drawing.Bitmap(parent.Width, parent.Height);
			texture = parent.LoadTexture(this.bitmap, unit);


            CreateVBO();
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

			if (program != 0)
				GL.DeleteProgram(program);
			if (texture != 0)
                GL.DeleteTexture(texture);
            if (vertex_buffer_object != 0)
                GL.DeleteBuffers(1, ref vertex_buffer_object);
            if (element_buffer_object != 0)
                GL.DeleteBuffers(1, ref element_buffer_object);
            if (color_buffer_object != 0)
                GL.DeleteBuffers(1, ref color_buffer_object);
		}

        public override void Render()
        {

            GL.Clear(ClearBufferMask.ColorBufferBit |
                     ClearBufferMask.DepthBufferBit);

            GL.UseProgram(program);
            GL.Uniform1(GL.GetUniformLocation(program, "WIDTH"), parent.Width);
            GL.Uniform1(GL.GetUniformLocation(program, "HEIGHT"), parent.Height);
            GL.Uniform2(GL.GetUniformLocation(program, "F"), F);
            GL.Uniform2(GL.GetUniformLocation(program, "C"), C);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "K"), true, ref K);

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertex_buffer_object);
            GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
            GL.BindBuffer(BufferTarget.ArrayBuffer, color_buffer_object);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, element_buffer_object);

            GL.DrawElements(BeginMode.Triangles, shape.Indices.Length,
                DrawElementsType.UnsignedInt, IntPtr.Zero);

            //GL.DrawArrays(GL.Enums.BeginMode.POINTS, 0, shape.Vertices.Length);

            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
        }

        #region private void CreateVBO()

        void CreateVBO()
        {
            int size;

            GL.GenBuffers(1, out vertex_buffer_object);
            GL.GenBuffers(1, out color_buffer_object);
            GL.GenBuffers(1, out element_buffer_object);

            // Upload the vertex buffer.
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertex_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(shape.Vertices.Length * 3 * sizeof(float)), shape.Vertices,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != shape.Vertices.Length * 3 * sizeof(Single))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (vertices). Tried to upload {0} bytes, uploaded {1}.",
                    shape.Vertices.Length * 3 * sizeof(Single), size));

            // Upload the color buffer.
            GL.BindBuffer(BufferTarget.ArrayBuffer, color_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(shape.Colors.Length * sizeof(int)), shape.Colors,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != shape.Colors.Length * sizeof(int))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (colors). Tried to upload {0} bytes, uploaded {1}.",
                    shape.Colors.Length * sizeof(int), size));

            // Upload the index buffer (elements inside the vertex buffer, not color indices as per the IndexPointer function!)
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, element_buffer_object);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(shape.Indices.Length * sizeof(Int32)), shape.Indices,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != shape.Indices.Length * sizeof(int))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (offsets). Tried to upload {0} bytes, uploaded {1}.",
                    shape.Indices.Length * sizeof(int), size));
        }

        #endregion
    }

}
