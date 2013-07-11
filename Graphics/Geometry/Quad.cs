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
        public Quad()
        {
            Vertices = new Vector3[]
            {
                new Vector3(1f, -1.0f,  0.5f),
                new Vector3( -1.0f, -1.0f,  0.5f),
                new Vector3( -1.0f,  1.0f,  0.5f),
                new Vector3(1f,  1.0f,  0.5f),
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
