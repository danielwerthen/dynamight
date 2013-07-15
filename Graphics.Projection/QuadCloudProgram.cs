using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class QuadCloudProgram : World2ScreenProgram
    {
        int VBOHandle;
        struct VertexC4ubV3f
        {
            public Vector2 TexCoord;
            public Vector3 Position;

            public static int SizeInBytes = 20;

        }

        float quadSize = 0;
        VertexC4ubV3f[] VBO;
        public QuadCloudProgram(float quadSize)
        {
            this.quadSize = quadSize;
        }

        public override void Load(ProgramWindow parent)
        {
            base.Load(parent);

            // Setup parameters for Points
            GL.PointSize(quadSize);
            GL.Enable(EnableCap.PointSmooth);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);

            // Setup VBO state
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.EnableClientState(ArrayCap.VertexArray);
            
            GL.GenBuffers(1, out VBOHandle);

            // Since there's only 1 VBO in the app, might aswell setup here.
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, VertexC4ubV3f.SizeInBytes, 0);
            GL.VertexPointer(3, VertexPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(Vector2.SizeInBytes));

            VBO = new VertexC4ubV3f[0];
        }

        public override void Unload()
        {
            GL.Disable(EnableCap.PointSprite);
            GL.DeleteBuffers(1, ref VBOHandle);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            base.Unload();
        }

        private IEnumerable<VertexC4ubV3f> MakeQuad(Vector3 p)
        {
            //yield return new VertexC4ubV3f() { Position = new Vector3(0.1f, -0.2f, p.Z), TexCoord = new Vector2(0, 0) };
            //yield return new VertexC4ubV3f() { Position = new Vector3(0.2f, -0.2f, p.Z), TexCoord = new Vector2(1, 0) };
            //yield return new VertexC4ubV3f() { Position = new Vector3(0.2f, -0.1f, p.Z), TexCoord = new Vector2(1, 1) };
            //yield return new VertexC4ubV3f() { Position = new Vector3(0.1f, -0.1f, p.Z), TexCoord = new Vector2(0, 1) };
            //yield return new VertexC4ubV3f() { Position = new Vector3(0, -0.2f, p.Z), TexCoord = new Vector2(0, 0) };
            //yield return new VertexC4ubV3f() { Position = new Vector3(0.2f, -0.2f, p.Z), TexCoord = new Vector2(1, 0) };
            //yield return new VertexC4ubV3f() { Position = new Vector3(0.2f,0, p.Z), TexCoord = new Vector2(1, 1) };
            //yield return new VertexC4ubV3f() { Position = new Vector3(0, 0, p.Z), TexCoord = new Vector2(0, 1) };
            yield return new VertexC4ubV3f() { Position = new Vector3(p.X + quadSize, p.Y + quadSize, p.Z), TexCoord = new Vector2(0, 0) };
            yield return new VertexC4ubV3f() { Position = new Vector3(p.X - quadSize, p.Y + quadSize, p.Z), TexCoord = new Vector2(1, 0) };
            yield return new VertexC4ubV3f() { Position = new Vector3(p.X - quadSize, p.Y - quadSize, p.Z), TexCoord = new Vector2(1, 1) };
            yield return new VertexC4ubV3f() { Position = new Vector3(p.X + quadSize, p.Y - quadSize, p.Z), TexCoord = new Vector2(0, 1) };
        }

        public void SetPositions(Vector3[] vertices)
        {
            VBO = vertices.SelectMany(v => MakeQuad(v)).ToArray();
        }

        public override void Render()
        {
            base.Render();

            // Tell OpenGL to discard old VBO when done drawing it and reserve memory _now_ for a new buffer.
            // without this, GL would wait until draw operations on old VBO are complete before writing to it
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexC4ubV3f.SizeInBytes * VBO.Length), IntPtr.Zero, BufferUsageHint.StreamDraw);
            // Fill newly allocated buffer
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexC4ubV3f.SizeInBytes * VBO.Length), VBO, BufferUsageHint.StreamDraw);
            // Only draw particles that are alive
            GL.DrawArrays(BeginMode.Quads, 0, VBO.Length);


        }
    }
}
