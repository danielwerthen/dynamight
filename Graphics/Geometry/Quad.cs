using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Geometry
{
    public class Quad : Shape
    {
        public Quad(Vector3 C, float size, Vector3 E1, Vector3 E2, Func<Vector2, Vector2> texTran)
        {
            Vertices = new Vector3[]
            {
                new Vector3(
                    C.X + size * 0.5f * E1.X + size * 0.5f * E2.X,
                    C.Y + size * 0.5f * E1.Y + size * 0.5f * E2.Y,
                    C.Z + size * 0.5f * E1.Z + size * 0.5f * E2.Z),
                new Vector3(                             
                    C.X - size * 0.5f * E1.X + size * 0.5f * E2.X,
                    C.Y - size * 0.5f * E1.Y + size * 0.5f * E2.Y,
                    C.Z - size * 0.5f * E1.Z + size * 0.5f * E2.Z),
                new Vector3(                             
                    C.X - size * 0.5f * E1.X - size * 0.5f * E2.X,
                    C.Y - size * 0.5f * E1.Y - size * 0.5f * E2.Y,
                    C.Z - size * 0.5f * E1.Z - size * 0.5f * E2.Z),
                new Vector3(                             
                    C.X + size * 0.5f * E1.X - size * 0.5f * E2.X,
                    C.Y + size * 0.5f * E1.Y - size * 0.5f * E2.Y,
                    C.Z + size * 0.5f * E1.Z - size * 0.5f * E2.Z),
            };

            Indices = new int[]
            {
                // front face
                0, 1, 2, 2, 3, 0,
            };

            Normals = new Vector3[]
            {
                new Vector3(0,0,  -1.0f),
            };

            Texcoords = new Vector2[] {
                texTran(new Vector2(1,0)),
                texTran(new Vector2(1,1)),
                texTran(new Vector2(0,1)),
                texTran(new Vector2(0,0)),
            };

            Colors = new int[]
            {
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.Gold),
                ColorToRgba32(Color.Gold),
            };
        }

        public Quad()
        {
            Vertices = new Vector3[]
            {
                new Vector3(1f, 0.0f,  -0.5f),
                new Vector3( 0.0f, 0.0f,  -0.5f),
                new Vector3( 0.0f,  1.0f,  -0.5f),
                new Vector3(1f,  1.0f,  -0.5f),
            };

            Indices = new int[]
            {
                // front face
                0, 1, 2, 2, 3, 0,
            };

            Normals = new Vector3[]
            {
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
            };

            Texcoords = new Vector2[] {
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1),
                new Vector2(0,0),
            };

            Colors = new int[]
            {
                ColorToRgba32(Color.DarkRed),
                ColorToRgba32(Color.DarkRed),
                ColorToRgba32(Color.Gold),
                ColorToRgba32(Color.Gold),
            };
        }
    }
}
