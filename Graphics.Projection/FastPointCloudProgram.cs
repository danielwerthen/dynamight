using Graphics.Utils;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class FastPointCloudProgram : World2ScreenProgram
    {
        int VBOHandle;

        const string VSLighting = @"
varying vec3 N;
varying vec3 v;
void main(void)
{
    gl_FrontColor = gl_Color;
    gl_TexCoord[0] = gl_MultiTexCoord0; 
    vec4 V = gl_ModelViewProjectionMatrix * gl_Vertex;
    //v = vec3(gl_ModelViewProjectionMatrix * gl_Vertex);       
    //N = normalize(gl_NormalMatrix * gl_Normal);
    gl_Position = distort(V);

}
";

        const string FSLighting = @"

varying vec3 N;
varying vec3 v;
uniform sampler2D COLORTABLE;

void main(void)
{
   gl_FragColor = gl_Color;
}
";

        protected override IEnumerable<string> VertexShaderParts()
        {
            yield return VSDistort;
            yield return VSLighting;
        }

        protected override IEnumerable<string> FragmentShaderParts()
        {
            yield return FSLighting;
        }

        float pointSize = 0;

        public FastPointCloudProgram(float pointSize)
        {
            this.pointSize = pointSize;
        }

        public override void Load(ProgramWindow parent)
        {
            base.Load(parent);

            // Setup parameters for Points
            GL.PointSize(pointSize);
            GL.Enable(EnableCap.PointSmooth);
            GL.Disable(EnableCap.DepthTest);
            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);

            // Setup VBO state
            GL.EnableClientState(ArrayCap.VertexArray);
            
            GL.GenBuffers(1, out VBOHandle);

            // Since there's only 1 VBO in the app, might aswell setup here.
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, 0);
            //GL.ColorPointer(4, ColorPointerType.UnsignedByte, VertexC4ubV3f.SizeInBytes, (IntPtr)0);
            //GL.VertexPointer(3, VertexPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte)));
            //GL.NormalPointer(NormalPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte) + Vector3.SizeInBytes));

            vertices = new Vector3[0];
        }

        public override void Unload()
        {
            GL.Disable(EnableCap.PointSprite);
            GL.DeleteBuffers(1, ref VBOHandle);
            GL.DisableClientState(ArrayCap.VertexArray);
            base.Unload();
        }

        Vector3[] vertices = new Vector3[0];
        public void SetPositions(Vector3[] vertices)
        {
            this.vertices = vertices;
        }

        public override void Render()
        {
            base.Render();

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vector3.SizeInBytes), IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vector3.SizeInBytes), vertices, BufferUsageHint.StreamDraw);

            GL.DrawArrays(BeginMode.Points, 0, vertices.Length);
        }
    }
}
