using Graphics.Geometry;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class Renderable
    {
        public Renderable()
        {
        }

        public Shape Shape { get; set; }
        public Animatable Animatable { get; set; }
        
        private bool _visible = true;
        public virtual bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

    }

    public struct DynamicVertex
    {
        public byte R, G, B, A;
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;

        public static int SizeInBytes = 36;

        public DynamicVertex(Vector3 v)
        {
            R = 150;
            B = 150;
            G = 150;
            A = 255;
            Normal = new Vector3(0, 0, -1);
            TexCoord = new Vector2(0.25f, 0.25f);
            Position = v;
        }
    }

    public class DynamicRenderable
    {
        public DynamicVertex[] Vertices { get; set; }
        int VBOHandle;

        public DynamicRenderable()
        {
            Vertices = new DynamicVertex[0];
        }

        public void Load()
        {
            GL.GenBuffers(1, out VBOHandle);
        }

        public void Unload()
        {
            if (VBOHandle != 0)
                GL.DeleteBuffers(1, ref VBOHandle);
        }

        private void SetupBuffer()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, DynamicVertex.SizeInBytes, (IntPtr)0);
            GL.VertexPointer(3, VertexPointerType.Float, DynamicVertex.SizeInBytes, (IntPtr)(4 * sizeof(byte)));
            GL.NormalPointer(NormalPointerType.Float, DynamicVertex.SizeInBytes, (IntPtr)(4 * sizeof(byte) + Vector3.SizeInBytes));
            GL.TexCoordPointer(2, TexCoordPointerType.Float, DynamicVertex.SizeInBytes, (IntPtr)(4 * sizeof(byte) + Vector3.SizeInBytes + Vector3.SizeInBytes));
            // Tell OpenGL to discard old VBO when done drawing it and reserve memory _now_ for a new buffer.
            // without this, GL would wait until draw operations on old VBO are complete before writing to it
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(DynamicVertex.SizeInBytes * Vertices.Length), IntPtr.Zero, BufferUsageHint.StreamDraw);
            // Fill newly allocated buffer
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(DynamicVertex.SizeInBytes * Vertices.Length), Vertices, BufferUsageHint.StreamDraw);
        }

        public void Render()
        {
            SetupBuffer();
            GL.DrawArrays(BeginMode.Points, 0, Vertices.Length);
        }
    }
}
