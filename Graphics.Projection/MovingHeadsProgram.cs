using Graphics.Geometry;
using Graphics.Textures;
using Graphics.Utils;
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
    public class MovingHeadsProgram : World2ScreenProgram
    {

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
uniform sampler2D COLORTABLE;

void main(void)
{
  gl_FragColor = texture2D(COLORTABLE, gl_TexCoord[0].st);
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

        public MovingHeadsProgram()
        {

        }

        MultipleTextures textures;
        public override void Load(ProgramWindow parent)
        {
            base.Load(parent);

            // Setup parameters for Points
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.CullFace(CullFaceMode.Back);
            textures = new MultipleTextures(this);
            Bitmap[] maps = new Bitmap[4];
            {
                var bitmap = new Bitmap(300, 300);

                QuickDraw.Start(bitmap).All((xp, yp) =>
                {
                    var x = 0.5 - xp;
                    var y = 0.5 - yp;
                    var i = Math.Sqrt(x * x + y * y);
                    if (i > 1)
                        i = 1;
                    i = Math.Pow(1 - i, 1);
                    i += 0.4;
                    if (i > 1)
                        i = 1;
                    else
                        i = Math.Pow(i, 29);
                    byte ii = (byte)(i * 255);
                    return Color.FromArgb(ii, 245, 150, 135);
                }).Finish();
                maps[0] = bitmap;
            }
            {
                var bitmap = new Bitmap(300, 300);

                QuickDraw.Start(bitmap).All((xp, yp) =>
                {
                    var x = 0.5 - xp;
                    var y = 0.5 - yp;
                    var i = Math.Sqrt(x * x + y * y);
                    i = (Math.Sin(i * Math.PI * 8) + 1) / 2.0;
                    byte ii = (byte)(i * 255);
                    return Color.FromArgb(ii, 245, 150, 135);
                }).Finish();
                maps[1] = bitmap;
                maps[2] = bitmap;
                maps[3] = bitmap;
            }


            textures.Load(maps);
            renderer = new Renderer();
        }
        Translator translator;
        Renderer renderer;
        Renderable[] objects;

        public Action<Vector3, bool>[] CreateRenderables(int count)
        {
            var ids = Dynamight.ImageProcessing.CameraCalibration.Range.OfInts(count);
            renderer.Load(objects = ids.Select(_ => new Renderable()
            {
                Shape = new Quad(new Vector3(0, 0.0f, 0f), 0.25f, defaultE1, defaultE2, (v) => textures.Transform(v, 0)),
                Animatable = translator = new Translator() // new RadialSpin(new Vector3(0.2f,0,0))
            }).ToArray());
            renderer.Start();
            return ids.Select(i =>
            {
                return (Action<Vector3, bool>)((v, visible) =>
                {
                    var r = objects[i];
                    (r.Animatable as Translator).SetPosition(v);
                    r.Visible = visible;
                });
            }).ToArray();
        }

        public override void Unload()
        {
            renderer.Unload();
            base.Unload();
        }

        Vector3 position = new Vector3(-0.1f, 0.0f, -1f);
        public void SetPosition(Vector3 v)
        {
            translator.SetPosition(v);
        }



        public override void Render()
        {
            base.Render();

            renderer.Render();

            //GL.Begin(BeginMode.Quads);

            //DrawQuad(new Vector3(0,0,-1f), 0.5, 2);

            //GL.End();

        }

        private readonly static Vector3 defaultE1 = new Vector3(1, 0, 0);
        private readonly static Vector3 defaultE2 = new Vector3(0, 1, 0);

        private void DrawQuad(Vector3 C, double size, int texture, Vector3? e1 = null, Vector3? e2 = null)
        {
            var E1 = e1 ?? defaultE1;
            var E2 = e2 ?? defaultE2;
            GL.TexCoord2(textures.Transform(new Vector2(0.0f, 1.0f), texture));
            GL.Vertex3(C.X + size * 0.5 * E1.X + size * 0.5 * E2.X,
                C.Y + size * 0.5 * E1.Y + size * 0.5 * E2.Y,
                C.Z + size * 0.5 * E1.Z + size * 0.5 * E2.Z);
            GL.TexCoord2(textures.Transform(new Vector2(1.0f, 1.0f), texture));
            GL.Vertex3(C.X - size * 0.5 * E1.X + size * 0.5 * E2.X,
                C.Y - size * 0.5 * E1.Y + size * 0.5 * E2.Y,
                C.Z - size * 0.5 * E1.Z + size * 0.5 * E2.Z);
            GL.TexCoord2(textures.Transform(new Vector2(1.0f, 0.0f), texture));
            GL.Vertex3( C.X - size * 0.5 * E1.X - size * 0.5 * E2.X,
                        C.Y - size * 0.5 * E1.Y - size * 0.5 * E2.Y,
                        C.Z - size * 0.5 * E1.Z - size * 0.5 * E2.Z);
            GL.TexCoord2(textures.Transform(new Vector2(0.0f, 0.0f), texture));
            GL.Vertex3( C.X + size * 0.5 * E1.X - size * 0.5 * E2.X,
                        C.Y + size * 0.5 * E1.Y - size * 0.5 * E2.Y,
                        C.Z + size * 0.5 * E1.Z - size * 0.5 * E2.Z);
        }
    }
}
