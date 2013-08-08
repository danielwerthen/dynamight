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
            
            GL.GenBuffers(1, out VBOHandle);

            // Since there's only 1 VBO in the app, might aswell setup here.
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOHandle);
            GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, 0);
            //GL.ColorPointer(4, ColorPointerType.UnsignedByte, VertexC4ubV3f.SizeInBytes, (IntPtr)0);
            //GL.VertexPointer(3, VertexPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte)));
            //GL.NormalPointer(NormalPointerType.Float, VertexC4ubV3f.SizeInBytes, (IntPtr)(4 * sizeof(byte) + Vector3.SizeInBytes));

            vertices = new Vector3[0];
            lights = (Dynamight.ImageProcessing.CameraCalibration.Range.OfInts(8)).Select(_ => new LightSourceParameters()).ToArray();
            lights[0].InUse = true;
            lights[0].Position = new Vector4(0, 0.44f, 0.5f, 1);
            lights[0].LinearAttenuation = 10f;

            var keyl = new KeyboardListener(parent.Keyboard);

            keyl.AddAction(() => Selection = (Selection == 0 ? null : (int?)0), Key.F1);
            keyl.AddAction(() => Selection = (Selection == 1 ? null : (int?)1), Key.F2);
            keyl.AddAction(() => Selection = (Selection == 2 ? null : (int?)2), Key.F3);
            keyl.AddAction(() => Selection = (Selection == 3 ? null : (int?)3), Key.F4);
            keyl.AddAction(() => Selection = (Selection == 4 ? null : (int?)4), Key.F5);
            keyl.AddAction(() => Selection = (Selection == 5 ? null : (int?)5), Key.F6);
            keyl.AddAction(() => Selection = (Selection == 6 ? null : (int?)6), Key.F7);
            keyl.AddAction(() => Selection = (Selection == 7 ? null : (int?)7), Key.F8);

            keyl.AddAction(Activate, Key.A);

            keyl.AddBinaryAction(0.01f, -0.01f, Key.Right, Key.Left, null, (f) => MoveX(f));
            keyl.AddBinaryAction(0.01f, -0.01f, Key.Down, Key.Up, null, (f) => MoveY(f));
            keyl.AddBinaryAction(0.01f, -0.01f, Key.Down, Key.Up, new Key[] { Key.ShiftLeft }, (f) => MoveZ(f));

            keyl.AddBinaryAction(0.05f, -0.05f, Key.Right, Key.Left, new Key[] { Key.ControlLeft }, (f) => MoveX(f));
            keyl.AddBinaryAction(0.05f, -0.05f, Key.Down, Key.Up, new Key[] { Key.ControlLeft }, (f) => MoveY(f));
            keyl.AddBinaryAction(0.05f, -0.05f, Key.Down, Key.Up, new Key[] { Key.ShiftLeft, Key.ControlLeft }, (f) => MoveZ(f));


        }
        int? Selection = null;

        private void Activate()
        {
            if (Selection == null)
                return;
            else
            {
                var l = lights[Selection.Value];
                l.InUse = !l.InUse;
            }
        }

        private void MoveX(float f)
        {
            if (Selection != null)
            {
                var l = lights[Selection.Value];
                l.Position.X += f;
                Console.Clear();
                Console.WriteLine(l.Position.ToString());
            }
        }

        private void MoveY(float f)
        {
            if (Selection != null)
            {
                var l = lights[Selection.Value];
                l.Position.Y += f;
                Console.Clear();
                Console.WriteLine(l.Position.ToString());
            }
        }

        private void MoveZ(float f)
        {
            if (Selection != null)
            {
                var l = lights[Selection.Value];
                l.Position.Z += f;
                Console.Clear();
                Console.WriteLine(l.Position.ToString());
            }
        }

        public override void Unload()
        {
            GL.Disable(EnableCap.PointSprite);
            GL.DeleteBuffers(1, ref VBOHandle);
            GL.DisableClientState(ArrayCap.VertexArray);
            lightProgram.Unload();
            base.Unload();
        }

        GLSLLightStudioProgram lightProgram = new GLSLLightStudioProgram();
        Vector3[] vertices = new Vector3[0];
        LightSourceParameters[] lights;
        public void SetPositions(Vector3[] vertices)
        {
            this.vertices = vertices;
        }

        public override void Render()
        {
            base.Render();
            lightProgram.Activate();
            SetupDistortion(lightProgram.ProgramLocation);

            int c = 0;
            var lids = lights.Where(l => l.InUse).Select(_ => c++).ToArray();
            if (c > 0)
                c.ToString();
            lids.Zip(lights.Where(l => l.InUse), (i, l) => l.Set(i)).ToArray();
            lightProgram.Setup(c);

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vector3.SizeInBytes), IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Vector3.SizeInBytes), vertices, BufferUsageHint.StreamDraw);

            GL.DrawArrays(BeginMode.Points, 0, vertices.Length);
        }
    }
}
