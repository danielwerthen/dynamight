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
  float u = F.x * xp + C.x;
  float v = F.y * yp + C.y;
  gl_Position = vec4((u) / ( 2.0 * float(WIDTH)), V.y, V.z, V.w);
  gl_Position = vec4(u / (float(WIDTH)), v / (float(HEIGHT)), z, 1.0);
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

        Shape shape = new Checkerboard();

        public float[] Multi(OpenTK.Matrix4d mat, float[] v)
        {
            return new float[] {
                (float)(v[0] * mat.M11 + v[1] * mat.M21 + v[2] * mat.M31 + v[3] * mat.M41),
                (float)(v[0] * mat.M12 + v[1] * mat.M22 + v[2] * mat.M32 + v[3] * mat.M42),
                (float)(v[0] * mat.M13 + v[1] * mat.M23 + v[2] * mat.M33 + v[3] * mat.M43),
                (float)(v[0] * mat.M14 + v[1] * mat.M24 + v[2] * mat.M34 + v[3] * mat.M44),
            };
        }

        Vector2 F, C;
        Matrix4 K;
        public void SetProjection(CalibrationResult calib)
        {
            parent.MakeCurrent();

            var Width = parent.Width;
            var Height = parent.Height;
            GL.Viewport(0,0, Width, Height);

            var points = new float[][] {
                new float[] { 0,0,0,1 },
                new float[] { 1,0,0,1 },
                new float[] { 0,1,0,1 },
                new float[] { 0,0,1,1 },
            };
            var rt = calib.Extrinsic.ExtrinsicMatrix.Data;
            var extrin = new OpenTK.Matrix4d(
                rt[0, 0], rt[1, 0], rt[2, 0], 0,
                rt[0, 1], rt[1, 1], rt[2, 1], 0,
                rt[0, 2], rt[1, 2], rt[2, 2], 0,
                rt[0, 3], rt[1, 3], rt[2, 3], 1);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref extrin);

            var tp = points.Select(r => Multi(extrin, r)).ToArray();
            
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
            return;


            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            Matrix4 test = new Matrix4(
                2, 0, 0, 0, 
                0, -2, 0, 0, 
                0, 0, -1, 0,
                -1f, 1f, -1.1f, 1);
            Matrix4 lookat = Matrix4.LookAt(0, 0, 5, 0, 0, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Modelview);
            //GL.LoadMatrix(ref test);

            float aspect_ratio = Width / (float)Height;
            Matrix4 perpective = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspect_ratio, 1, 64);
            GL.MatrixMode(MatrixMode.Projection);
            var perp = new Matrix4(
                1f, 0, 0, 0,
                0, 1.3333f, 0, 0,
                0, 0, -1f, -1f,
                0, 0, -2, 0);

            GL.LoadMatrix(ref perp);
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

        //public override void Render()
        //{
        //    GL.Clear(ClearBufferMask.ColorBufferBit |
        //             ClearBufferMask.DepthBufferBit);

        //    GL.BindTexture(TextureTarget.Texture2D, texture);
        //    GL.UseProgram(program);

        //    GL.Uniform1(GL.GetUniformLocation(program, "COLORTABLE"), unit - TextureUnit.Texture0);
        //    GL.Uniform1(GL.GetUniformLocation(program, "WIDTH"), parent.Width);
        //    GL.Uniform1(GL.GetUniformLocation(program, "HEIGHT"), parent.Height);



        //    GL.Begin(BeginMode.Quads);

        //    GL.TexCoord2(0.0f, 1.0f);
        //    GL.Vertex3(0.0f, 0.0f, 5.0f);
        //    GL.TexCoord2(1.0f, 1.0f);
        //    GL.Vertex3(1.5f, 0.0f, 5.0f);
        //    GL.TexCoord2(1.0f, 0.0f);
        //    GL.Vertex3(1.5f, 1.5f, 5.0f);
        //    GL.TexCoord2(0.0f, 0.0f);
        //    GL.Vertex3(0.0f, 1.5f, 5.0f);

        //    GL.End();
        //}
    }

}
