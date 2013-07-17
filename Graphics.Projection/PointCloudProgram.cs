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

        const string VSLighting = @"
varying vec3 N;
varying vec3 v;
void main(void)
{
    gl_FrontColor = gl_Color;
    gl_TexCoord[0] = gl_MultiTexCoord0; 
    vec4 V = gl_ModelViewProjectionMatrix * gl_Vertex;
    v = vec3(gl_ModelViewProjectionMatrix * gl_Vertex);       
    N = normalize(gl_NormalMatrix * gl_Normal);
    gl_Position = distort(V);

}
";

        const string FSLighting = @"

varying vec3 N;
varying vec3 v;

void main(void)
{
   vec3 D = gl_LightSource[0].position.xyz - v;
   float dist = distance(gl_LightSource[0].position.xyz, v);
   float attn = 1.0/( gl_LightSource[0].constantAttenuation + 
                    gl_LightSource[0].linearAttenuation * dist +
                    gl_LightSource[0].quadraticAttenuation * dist * dist );
   vec3 L = normalize(D);   
   vec4 Idiff = attn * gl_FrontLightProduct[0].diffuse * max(dot(N,L), 0.0);  
   Idiff = clamp(Idiff, 0.0, 1.0); 

   gl_FragColor = Idiff;
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
            GL.EnableClientState(ArrayCap.NormalArray);
            
            GL.GenBuffers(1, out VBOHandle);

            // Since there's only 1 VBO in the app, might aswell setup here.
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, VertexC4ubV3f.SizeInBytes, (IntPtr)0);
            GL.VertexPointer(3, VertexPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte)));
            GL.NormalPointer(NormalPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte) + Vector3.SizeInBytes));



            GL.Light(LightName.Light0, LightParameter.Diffuse, OpenTK.Graphics.Color4.White);
            GL.Light(LightName.Light0, LightParameter.ConstantAttenuation, 1);
            GL.Light(LightName.Light0, LightParameter.LinearAttenuation, 4f);
            GL.Light(LightName.Light0, LightParameter.QuadraticAttenuation, 0);
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Material(MaterialFace.Front, MaterialParameter.Ambient, new float[] { 0.3f, 0.3f, 0.3f, 1.0f });
            GL.Material(MaterialFace.Front, MaterialParameter.Diffuse, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
            GL.Material(MaterialFace.Front, MaterialParameter.Specular, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
            GL.Material(MaterialFace.Front, MaterialParameter.Emission, new float[] { 0.0f, 0.0f, 0.0f, 1.0f });

            VBO = new VertexC4ubV3f[0];
        }

        public override void Unload()
        {
            GL.Disable(EnableCap.PointSprite);
            GL.DeleteBuffers(1, ref VBOHandle);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.NormalArray);
            base.Unload();
        }

        private float[] normal(float[] p0, float[] p1, float[] p2)
        {
            var u = new float[] { p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2] };
            var v = new float[] { p2[0] - p0[0], p2[1] - p0[1], p2[2] - p0[2] };
            var n = new float[] { u[1] * v[2] - u[2] * v[1],
                u[2] * v[0] - u[0] * v[2],
                u[0] * v[1] - u[1] * v[0] };
            if (n[2] < 0)
                return n;
            else
                return new float[] { -n[0], -n[1], -n[2] };
        }

        public float[] GetNormal(float[] p, float[][] closest)
        {
            var ns = closest.Where(c => c[0] != p[0] && c[1] != p[1] && c[2] != p[2])
                .OrderBy(c => (c[0] - p[0]) * (c[0] - p[0]) + (c[1] - p[1]) * (c[1] - p[1]) + (c[2] - p[2]) * (c[2] - p[2]))
                .Take(6)
                .ToArray();
            int count = 0;
            var ids = ns.Select(_ => count++).ToArray();
            var normals = ids.Skip(1).Select(i => normal(p, ns[i - 1], ns[i])).ToArray();
            return normals.Aggregate((v1, v2) => 
                new float[] { (v1[0] + v2[0]) / (float)normals.Count(), 
                    (v1[1] + v2[1]) / (float)normals.Count(), 
                    (v1[2] + v2[2]) / (float)normals.Count() });
        }

        public void SetPositions(float[][] vertices)
        {
            //using (var tree = new OctreeLookup(vertices))
            //{
                VBO = new VertexC4ubV3f[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    Vector3 norm = new Vector3(0, 0, -1);
                    //var v = vertices[i];
                    //var closest = tree.Neighbours(v, 0.01f).ToArray();
                    //if (closest.Count() > 3)
                    //{
                    //    var n = GetNormal(v, closest);
                    //    norm = new Vector3(n[0], n[1], v[2]);
                    //}
                    VBO[i].R = 100;
                    VBO[i].G = 100;
                    VBO[i].B = 100;
                    VBO[i].A = 255;
                    VBO[i].Position = new Vector3(vertices[i][0], vertices[i][1], vertices[i][2]);
                    VBO[i].Normal = norm;
                }
            //}
        }

        public void SetPositions(Vector3[] vertices)
        {
            if (VBO.Length != vertices.Length)
            {
                VBO = new VertexC4ubV3f[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    Vector3 norm = new Vector3(0, 0, -1);
                    //var v = vertices[i];
                    //var closest = tree.Neighbours(v, 0.01f).ToArray();
                    //if (closest.Count() > 3)
                    //{
                    //    var n = GetNormal(v, closest);
                    //    norm = new Vector3(n[0], n[1], v[2]);
                    //}
                    VBO[i].R = 150;
                    VBO[i].G = 150;
                    VBO[i].B = 150;
                    VBO[i].A = 255;
                    VBO[i].Position = vertices[i];
                    VBO[i].Normal = norm;
                }
            }
            else
            {
                for (var i = 0; i < vertices.Length; i++)
                {
                    VBO[i].Position = vertices[i];
                }
            }
        }
        Vector4 Light0Pos;

        public Vector4 SetLight0Pos(float[] v)
        {
            return Light0Pos = this.Transform(v);
        }

        public override void Render()
        {
            base.Render();

            GL.Light(LightName.Light0, LightParameter.Position, Light0Pos);

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
