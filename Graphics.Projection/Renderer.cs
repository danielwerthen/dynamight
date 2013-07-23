using Graphics.Geometry;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class Renderer
    {
        public Renderer()
        {
            watch = new Stopwatch();
        }

        struct Buffers
        {
            public int vertex_buffer_object;
            public int color_buffer_object;
            public int element_buffer_object;
            public int normal_buffer_object;
            public int tex_buffer_object;
        }

        Renderable[] renderables;
        Buffers[] buffers;

        public void Load(Renderable[] renderables)
        {
            this.renderables = renderables;
            this.buffers = renderables.Select(r => Load(r)).ToArray();
        }

        Stopwatch watch;
        public void Start()
        {
            watch.Start();
        }

        public void Pause()
        {
            watch.Stop();
        }

        public void Reset()
        {
            watch.Reset();
        }

        private double getTime()
        {
            return watch.ElapsedMilliseconds / 1000.0;
        }

        public void Render()
        {

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.NormalArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);

            for (int i = 0; i < renderables.Length; i++)
            {
                var renderable = renderables[i];
                if (!renderable.Visible)
                    continue;
                var buf = buffers[i];
                GL.MatrixMode(MatrixMode.Modelview);
                var mat = renderable.Animatable.GetModelView(getTime());
                GL.LoadMatrix(ref mat);

                GL.BindBuffer(BufferTarget.ArrayBuffer, buf.vertex_buffer_object);
                GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);

                GL.BindBuffer(BufferTarget.ArrayBuffer, buf.color_buffer_object);
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);

                GL.BindBuffer(BufferTarget.ArrayBuffer, buf.normal_buffer_object);
                GL.NormalPointer(NormalPointerType.Float, 0, IntPtr.Zero);

                GL.BindBuffer(BufferTarget.ArrayBuffer, buf.tex_buffer_object);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, IntPtr.Zero);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, buf.element_buffer_object);
                GL.DrawElements(BeginMode.Triangles, renderable.Shape.Indices.Length,
                    DrawElementsType.UnsignedInt, IntPtr.Zero);
            }

            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.NormalArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
        }

        public void Unload()
        {
            foreach (var buf in buffers)
                Unload(buf);
        }

        void Unload(Buffers buf)
        {
            if (buf.vertex_buffer_object != 0)
                GL.DeleteBuffers(1, ref buf.vertex_buffer_object);
            if (buf.color_buffer_object != 0)
                GL.DeleteBuffers(1, ref buf.color_buffer_object);
            if (buf.element_buffer_object != 0)
                GL.DeleteBuffers(1, ref buf.element_buffer_object);
            if (buf.normal_buffer_object != 0)
                GL.DeleteBuffers(1, ref buf.normal_buffer_object);
            if (buf.tex_buffer_object != 0)
                GL.DeleteBuffers(1, ref buf.tex_buffer_object);
        }

        Buffers Load(Renderable renderable)
        {
            int size;

            Buffers buf = new Buffers();
            var shape = renderable.Shape;

            GL.GenBuffers(1, out buf.vertex_buffer_object);
            GL.GenBuffers(1, out buf.color_buffer_object);
            GL.GenBuffers(1, out buf.element_buffer_object);
            GL.GenBuffers(1, out buf.normal_buffer_object);
            GL.GenBuffers(1, out buf.tex_buffer_object);

            // Upload the vertex buffer.
            GL.BindBuffer(BufferTarget.ArrayBuffer, buf.vertex_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(shape.Vertices.Length * 3 * sizeof(float)), shape.Vertices,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != shape.Vertices.Length * 3 * sizeof(Single))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (vertices). Tried to upload {0} bytes, uploaded {1}.",
                    shape.Vertices.Length * 3 * sizeof(Single), size));

            // Upload the color buffer.
            GL.BindBuffer(BufferTarget.ArrayBuffer, buf.color_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(shape.Colors.Length * sizeof(int)), shape.Colors,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != shape.Colors.Length * sizeof(int))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (colors). Tried to upload {0} bytes, uploaded {1}.",
                    shape.Colors.Length * sizeof(int), size));

            // Upload the index buffer (elements inside the vertex buffer, not color indices as per the IndexPointer function!)
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, buf.element_buffer_object);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(shape.Indices.Length * sizeof(Int32)), shape.Indices,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != shape.Indices.Length * sizeof(int))
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (offsets). Tried to upload {0} bytes, uploaded {1}.",
                    shape.Indices.Length * sizeof(int), size));

            GL.BindBuffer(BufferTarget.ArrayBuffer, buf.normal_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(shape.Normals.Length * 3 * sizeof(float)), shape.Normals,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != shape.Normals.Length * sizeof(float) * 3)
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (offsets). Tried to upload {0} bytes, uploaded {1}.",
                    shape.Normals.Length * sizeof(float) * 3, size));


            GL.BindBuffer(BufferTarget.ArrayBuffer, buf.tex_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(shape.Texcoords.Length * 2 * sizeof(float)), shape.Texcoords,
                BufferUsageHint.StaticDraw);
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (size != shape.Texcoords.Length * sizeof(float) * 2)
                throw new ApplicationException(String.Format(
                    "Problem uploading vertex buffer to VBO (offsets). Tried to upload {0} bytes, uploaded {1}.",
                    shape.Texcoords.Length * sizeof(float) * 2, size));

            return buf;
        }
    }
}
