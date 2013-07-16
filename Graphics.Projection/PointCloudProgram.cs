using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class PointCloudProgram : World2ScreenProgram
    {
        int VBOHandle;
        struct VertexC4ubV3f
        {
            public byte R, B, G, A;
            public Vector3 Position;
            public Vector3 Normal;

            public static int SizeInBytes = 28;

        }


        float pointSize = 0;
        VertexC4ubV3f[] VBO;
        public PointCloudProgram(float pointSize)
        {
            this.pointSize = pointSize;
        }

        public override void Load(ProgramWindow parent)
        {
            base.Load(parent);

            // Setup parameters for Points
            GL.PointSize(pointSize);
            GL.Enable(EnableCap.PointSmooth);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);

            // Setup VBO state
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.VertexArray); 
            
            GL.GenBuffers(1, out VBOHandle);

            // Since there's only 1 VBO in the app, might aswell setup here.
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, VertexC4ubV3f.SizeInBytes, (IntPtr)0);
            GL.VertexPointer(3, VertexPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte)));
            GL.NormalPointer(NormalPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte) + Vector3.SizeInBytes));

            VBO = new VertexC4ubV3f[0];
        }

        public override void Unload()
        {
            GL.Disable(EnableCap.PointSprite);
            GL.DeleteBuffers(1, ref VBOHandle);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.VertexArray);
            base.Unload();
        }

        public void SetPositions(Vector3[] vertices)
        {
            VBO = new VertexC4ubV3f[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                VBO[i].R = 100;
                VBO[i].G = 100;
                VBO[i].B = 100;
                VBO[i].A = 255;
                VBO[i].Position = vertices[i];
                VBO[i].Normal = new Vector3(0, 0, 1);
            }
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
            GL.DrawArrays(BeginMode.Points, 0, VBO.Length);
        }
    }
}
