using Graphics.Geometry;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Projection.Lights
{
    public class LightRenderable : Renderable
    {
        LightSourceParameters light;
        public LightRenderable(LightSourceParameters light, Func<Vector2, Vector2> texTran)
        {
            this.light = light;
            this.Shape = new LightShape(new Vector3(0, 0, 0), 0.2f, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1), texTran);
            this.Animatable = new LightAnima(light);
        }

        public override bool Visible
        {
            get { return light.InUse; }
        }
    }

    public class LightAnima : Translator
    {
        LightSourceParameters light;
        public LightAnima(LightSourceParameters light)
        {
            this.light = light;
        }

        public override Matrix4 GetModelView(double time)
        {
            this.SetPosition(light.Position.Xyz);
            return base.GetModelView(time);
        }
    }

    public class LightShape : Shape
    {
        public LightShape(Vector3 C, float size, Vector3 E1, Vector3 E2, Vector3 E3, Func<Vector2, Vector2> texTran)
        {
            Vertices = new Vector3[]
            {
                C,
                new Vector3(
                    C.X + size * 0.5f * E1.X + size * 0.5f * E2.X + size * 0.5f * E3.X,
                    C.Y + size * 0.5f * E1.Y + size * 0.5f * E2.Y + size * 0.5f * E3.Y,
                    C.Z + size * 0.5f * E1.Z + size * 0.5f * E2.Z + size * 0.5f * E3.Z),
                new Vector3(                             
                    C.X - size * 0.5f * E1.X + size * 0.5f * E2.X + size * 0.5f * E3.X,
                    C.Y - size * 0.5f * E1.Y + size * 0.5f * E2.Y + size * 0.5f * E3.Y,
                    C.Z - size * 0.5f * E1.Z + size * 0.5f * E2.Z + size * 0.5f * E3.Z),
                new Vector3(                             
                    C.X - size * 0.5f * E1.X - size * 0.5f * E2.X + size * 0.5f * E3.X,
                    C.Y - size * 0.5f * E1.Y - size * 0.5f * E2.Y + size * 0.5f * E3.Y,
                    C.Z - size * 0.5f * E1.Z - size * 0.5f * E2.Z + size * 0.5f * E3.Z),
                new Vector3(                             
                    C.X + size * 0.5f * E1.X - size * 0.5f * E2.X + size * 0.5f * E3.X,
                    C.Y + size * 0.5f * E1.Y - size * 0.5f * E2.Y + size * 0.5f * E3.Y,
                    C.Z + size * 0.5f * E1.Z - size * 0.5f * E2.Z + size * 0.5f * E3.Z),
            };

            Indices = new int[]
            {
                0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1, 1, 2, 3, 3, 4, 1
            };

            Vertices = Indices.Select(i => Vertices[i]).ToArray();
            int c = 0;
            Indices = Indices.Select(_ => c++).ToArray();

            var ids = Dynamight.ImageProcessing.CameraCalibration.Range.OfInts(Indices.Length / 3);
            Func<int, Vector3> findN = (i) =>
            {
                var p0 = Vertices[i * 3 + 0];
                var p1 = Vertices[i * 3 + 1];
                var p2 = Vertices[i * 3 + 2];
                var e1 = p1 - p0;
                var e2 = p2 - p0;
                var n = Vector3.Cross(e1, e2);
                n.Normalize();
                return n;
            };


            Normals = Indices.Select(i => findN((int)Math.Floor((double)i / (double)3))).ToArray();

            Texcoords = Vertices.Select(v => texTran(new Vector2(0, 0))).ToArray();

            Colors = Vertices.Select(_ => ColorToRgba32(Color.White)).ToArray();
        }
    }
}
