using Graphics.Geometry;
using Graphics.Input;
using Graphics.Projection.Lights;
using Graphics.Textures;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection
{
    public class OverviewProgram : TextureProgram
    {
        /// <summary>
        /// Current camera position.
        /// </summary>
        private Vector3 eye = new Vector3(0.0f, 0.0f, -8.0f);

        private Vector2 camRot = new Vector2(0.0f, -0.25f);

        /// <summary>
        /// Current point to look at.
        /// </summary>
        private Vector3 pointAt = Vector3.Zero;

        /// <summary>
        /// Current "up" vector.
        /// </summary>
        private Vector3 up = Vector3.UnitY;

        /// <summary>
        /// Vertical field-of-view angle in radians.
        /// </summary>
        private float fov = 0.25f;

        /// <summary>
        /// Camera's far point.
        /// </summary>
        private float far = 200.0f;

        /// <summary>
        /// Camera's near point.
        /// </summary>
        private float near = 0.1f;

        private Size windowSize;
        private Renderer staticRenderer;
        private Renderer dynamicRenderer;
        private GLSLPointCloudProgram dynamicProgram = new GLSLPointCloudProgram(false);
        private MultipleTextures textures;

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
    gl_Position = V;

}
";

        const string FSLighting = @"

varying vec3 N;
varying vec3 v;
uniform sampler2D COLORTABLE;

void main(void)
{
  gl_FragColor = texture2D(COLORTABLE, gl_TexCoord[0].st);
  //gl_FragColor = vec4(1,1,1,1);
}
";
        

        /// <summary>
        /// Sets up a projective viewport
        /// </summary>
        private void SetupCamera()
        {
            int width = windowSize.Width;
            int height = windowSize.Height;

            // 1. set ViewPort transform:
            GL.Viewport(0, 0, width, height);

            // 2. set projection matrix
            GL.MatrixMode(MatrixMode.Projection);
            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(fov * (float)Math.PI, (float)width / (float)height, near, far);
            eye.Z = eye.Z.Clamp(-float.MaxValue, 0);
            Matrix4 eyem = Matrix4.CreateTranslation(eye);
            camRot.Y = camRot.Y.Clamp(-0.5f * (float)Math.PI, 0.5f * (float)Math.PI);
            Matrix4 rotX = Matrix4.CreateRotationX(-camRot.Y);
            //Let it rotate around Y however much it want!
            //camRot.X = camRot.X.Clamp(-1f * (float)Math.PI, 1f * (float)Math.PI);
            Matrix4 rotY = Matrix4.CreateRotationY(camRot.X);
            var tran = rotY * rotX * eyem * proj;
            GL.LoadMatrix(ref tran);
        }

        int program;
        Renderable Grid;
        Renderable[] LightsObjects;
        DynamicRenderable[] dynamics;
        public void SetPointCloud(int i, DynamicVertex[] vertices)
        {
            dynamics[i].Vertices = vertices;
        }

        public override void Load(ProgramWindow parent)
        {
            parent.MakeCurrent();
            base.Load(parent);
            GL.Disable(EnableCap.Dither);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.DstAlpha);
            GL.ClearColor(System.Drawing.Color.FromArgb(0,0,0,0));
            var vs = parent.CreateShader(ShaderType.VertexShader, VSLighting);
            var fs = parent.CreateShader(ShaderType.FragmentShader, FSLighting);
            program = parent.CreateProgram(vs, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
            dynamicProgram.Load();

            textures = new MultipleTextures(this);
            Bitmap white = new Bitmap(101, 101);
            QuickDraw.Start(white)
                .Fill(Color.White).Finish();
            Bitmap gridBot = new Bitmap(101, 101);
            QuickDraw.Start(gridBot)
                .All((x, y) =>
                {
                    var lpp = 10;
                    var xm = x % lpp;
                    var ym = y % lpp;
                    if (xm == 0 || ym == 0)
                        return Color.Gray;
                    return Color.FromArgb(150, 50, 50, 50);
                }, false).Finish();
            Bitmap[] maps = new Bitmap[] {
                white, gridBot
            };
            textures.Load(maps);

            lights = (Dynamight.ImageProcessing.CameraCalibration.Range.OfInts(8)).Select(_ => new LightSourceParameters()).ToArray();

            var xs = Dynamight.ImageProcessing.CameraCalibration.Range.OfDoubles(2, -2, 0.1f);
            //dynamicRenderer = new Renderer(new DynamicRenderable[] {
            //    new DynamicRenderable() {
            //        Vertices = xs.SelectMany(x => xs.Select(y => new DynamicVertex(new Vector3((float)x, (float)y, 0)))).ToArray()
            //        //Vertices = new DynamicVertex[] {
            //        //    new DynamicVertex(new Vector3(0,0,0)),
            //        //    new DynamicVertex(new Vector3(1,0,0)),
            //        //    new DynamicVertex(new Vector3(0,1,0)),
            //        //    new DynamicVertex(new Vector3(0,0,1)),
            //        //}
            //    }
            //});
            dynamicRenderer = new Renderer(dynamics = (Dynamight.ImageProcessing.CameraCalibration.Range.OfInts(6)).Select(_ => new DynamicRenderable()).ToArray());
            staticRenderer = new Renderer(null, (new Renderable[] {
                Grid = new Renderable() {
                    Visible = false,
                    Shape = new Quad(new Vector3(0,0,0), 10, new Vector3(1,0,0), new Vector3(0,0,1), (v) => textures.Transform(v, 1)),
                    Animatable = new Translator()
                },
            }).Concat(LightsObjects = lights.Select(l => new LightRenderable(l, (v) => textures.Transform(v, 0))).ToArray()).ToArray());
            windowSize = parent.Size;
            SetupCamera();

            staticRenderer.Load();
            dynamicRenderer.Load();
            staticRenderer.Start();
            dynamicRenderer.Start();

            keyl = new KeyboardListener(parent.Keyboard);

            keyl.AddAction(() => Selection = (Selection == 0 ? null : (int?)0), Key.F1);
            keyl.AddAction(() => Selection = (Selection == 1 ? null : (int?)1), Key.F2);
            keyl.AddAction(() => Selection = (Selection == 2 ? null : (int?)2), Key.F3);
            keyl.AddAction(() => Selection = (Selection == 3 ? null : (int?)3), Key.F4);
            keyl.AddAction(() => Selection = (Selection == 4 ? null : (int?)4), Key.F5);
            keyl.AddAction(() => Selection = (Selection == 5 ? null : (int?)5), Key.F6);
            keyl.AddAction(() => Selection = (Selection == 6 ? null : (int?)6), Key.F7);
            keyl.AddAction(() => Selection = (Selection == 7 ? null : (int?)7), Key.F8);

            keyl.AddAction(Activate, Key.A);
            keyl.AddAction(() => Grid.Visible = !Grid.Visible, Key.G);

            keyl.AddBinaryAction(0.01f, -0.01f, Key.Right, Key.Left, null, (f) => MoveX(f));
            keyl.AddBinaryAction(0.01f, -0.01f, Key.Down, Key.Up, null, (f) => MoveY(f));
            keyl.AddBinaryAction(0.01f, -0.01f, Key.Down, Key.Up, new Key[] { Key.ShiftLeft }, (f) => MoveZ(f));

            keyl.AddBinaryAction(0.05f, -0.05f, Key.Right, Key.Left, new Key[] { Key.ControlLeft }, (f) => MoveX(f));
            keyl.AddBinaryAction(0.05f, -0.05f, Key.Down, Key.Up, new Key[] { Key.ControlLeft }, (f) => MoveY(f));
            keyl.AddBinaryAction(0.05f, -0.05f, Key.Down, Key.Up, new Key[] { Key.ShiftLeft, Key.ControlLeft }, (f) => MoveZ(f));


            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.Light1);
            GL.Enable(EnableCap.Light2);
            GL.Enable(EnableCap.Light3);
            GL.Enable(EnableCap.Light4);
            GL.Enable(EnableCap.Light5);
            GL.Enable(EnableCap.Light6);
            GL.Enable(EnableCap.Light7);

            lights[0].InUse = true;
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
            if (Selection == null)
                camRot.X += f;
            else
            {
                var l = lights[Selection.Value];
                l.Position.X += f;
            }
        }

        private void MoveY(float f)
        {
            if (Selection == null)
                camRot.Y += f;
            else
            {
                var l = lights[Selection.Value];
                l.Position.Y += f;
            }
        }

        private void MoveZ(float f)
        {
            if (Selection == null)
                eye.Z += f;
            else
            {
                var l = lights[Selection.Value];
                l.Position.Z += f;
            }
        }

        KeyboardListener keyl;

        LightSourceParameters[] lights;
        public override void Unload()
        {
            base.Unload();
            parent.MakeCurrent();
            if (program != 0)
                GL.DeleteProgram(program);
            dynamicProgram.Unload();
        }

        public override void Render()
        {
            SetupCamera();
            base.Render();
            parent.MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit |
                             ClearBufferMask.DepthBufferBit);
            GL.UseProgram(program);
            GL.Uniform1(GL.GetUniformLocation(program, "COLORTABLE"), unit - TextureUnit.Texture0);
            staticRenderer.Render();

            dynamicProgram.Activate();
            int c = 0;
            var lids = lights.Where(l => l.InUse).Select(_ => c++).ToArray();
            if (c > 0)
                c.ToString();
            lids.Zip(lights.Where(l => l.InUse), (i, l) => l.Set(i)).ToArray();
            dynamicProgram.Setup(c);
            dynamicRenderer.Render();
        }
    }
}
