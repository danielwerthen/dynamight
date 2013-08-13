using Graphics.Input;
using Graphics.Projection.Lights;
using Graphics.Utils;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class LightStudioProgram : World2ScreenProgram
    {
        int VBOHandle;
        struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;
            public static int SizeInBytes = Vector3.SizeInBytes + Vector2.SizeInBytes;
        }

        const string VSLighting = @"
varying vec3 N;
varying vec3 v;
void main(void)
{
    gl_FrontColor = gl_Color;
    gl_TexCoord[0] = gl_MultiTexCoord0; 
    vec4 V = gl_ModelViewProjectionMatrix * gl_Vertex;
    v = vec3(gl_ModelViewMatrix * gl_Vertex);       
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

        public LightStudioProgram(float pointSize)
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
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            lightProgram.Load();
            
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.Light1);
            GL.Enable(EnableCap.Light2);
            GL.Enable(EnableCap.Light3);
            GL.Enable(EnableCap.Light4);
            GL.Enable(EnableCap.Light5);
            GL.Enable(EnableCap.Light6);
            GL.Enable(EnableCap.Light7);

            // Setup VBO state
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            
            GL.GenBuffers(1, out VBOHandle);

            // Since there's only 1 VBO in the app, might aswell setup here.
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            GL.VertexPointer(3, VertexPointerType.Float, Vertex.SizeInBytes, 0);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, Vertex.SizeInBytes, Vector3.SizeInBytes);
            //GL.ColorPointer(4, ColorPointerType.UnsignedByte, VertexC4ubV3f.SizeInBytes, (IntPtr)0);
            //GL.VertexPointer(3, VertexPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte)));
            //GL.NormalPointer(NormalPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte) + Vector3.SizeInBytes));

            vertices = new Vertex[0];
            lights.UseInput(parent.Keyboard);
            //lights[0].Diffuse = new OpenTK.Graphics.Color4(253, 157, 100, 255);
            lights[0].Diffuse = new OpenTK.Graphics.Color4(253, 176, 130, 255);
            //lights[0].Diffuse = new OpenTK.Graphics.Color4(55,55,55, 255);
            lights[0].InUse = true;
            lights[0].Position = new Vector4(0, 0.4f, 10.3f, 1);
            lights[0].LinearAttenuation = 0f;
            lights[0].ConstantAttenuation = 1f;
            lights[0].SpotDirection = new Vector4(0, 0, 1, 0);



        }

        public override void Unload()
        {
            GL.Disable(EnableCap.PointSprite);
            GL.DeleteBuffers(1, ref VBOHandle);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            lightProgram.Unload();
            base.Unload();
        }

        GLSLLightStudioProgram lightProgram = new GLSLLightStudioProgram();
        Vertex[] vertices = new Vertex[0];
        MoveableLights lights = new MoveableLights(8);
        public MoveableLights Lights
        {
            get { return lights; }
        }

        public void SetLights(Vector3[] positions, params int[] idx)
        {
            positions.Zip(idx, (v, i) =>
            {
                lights[i].Position = new Vector4(v);
                lights[i].InUse = true;
                return 0;
            }).ToArray();
        }

        public void SetPositions(Vector3[] vertices)
        {

            float[][] qs = new float[][] { 
                new float[] { -1, -1 },
                new float[] { 1, -1 },
                new float[] { 1, 1 },
                new float[] { -1, -1 },
                new float[] { -1, 1 },
                new float[] { 1, 1 } };
            Vector2[] tc = new Vector2[] {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,0),
                new Vector2(0,1),
                new Vector2(1,1),
            };
            this.vertices = vertices.SelectMany(v => 
                qs.Select(q => new Vector3(v.X + q[0] * pointSize, v.Y + q[1] * pointSize, v.Z))
                .Zip(tc, (p, t) => new Vertex() { Position = p, TexCoord = t })
                ).ToArray();
        }

        public override void Render()
        {
            base.Render();
            lightProgram.Activate();
            SetupDistortion(lightProgram.ProgramLocation);

            var activeLights = lights.Activate();
            lightProgram.Setup(activeLights);

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vertex.SizeInBytes), IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vertex.SizeInBytes), vertices, BufferUsageHint.StreamDraw);

            GL.DrawArrays(BeginMode.Triangles, 0, vertices.Length);
        }
    }
}
