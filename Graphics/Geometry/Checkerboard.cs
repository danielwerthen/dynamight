using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Geometry
{
    public class Checkerboard : Shape
    {
        public Checkerboard()
        {
            Vertices = new Vector3[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.25f, 0.0f),
                new Vector3(0.25f, 0.25f, 0.0f),
                new Vector3(0.25f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.25f, 0.0f),
                new Vector3(0.0f, 0.5f, 0.0f),
                new Vector3(0.25f, 0.5f, 0.0f),
                new Vector3(0.25f, 0.25f, 0.0f),
                new Vector3(0.25f, 0.25f, 0.0f),
                new Vector3(0.25f, 0.5f, 0.0f),
                new Vector3(0.5f, 0.5f, 0.0f),
                new Vector3(0.5f, 0.25f, 0.0f),
                new Vector3(0.25f, 0.0f, 0.0f),
                new Vector3(0.25f, 0.25f, 0.0f),
                new Vector3(0.5f, 0.25f, 0.0f),
                new Vector3(0.5f, 0.0f, 0.0f),
            };

            Indices = new int[]
            {
                // front face
                0, 1, 2, 2, 3, 0,
                4, 5, 6, 6, 7, 4,
                8, 9, 10, 10, 11, 8,
                12, 13, 14, 14, 15, 12,
            };

            Normals = new Vector3[]
            {
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
            };

            Colors = new int[]
            {
                ColorToRgba32(Color.FromArgb(50,50,50)),
                ColorToRgba32(Color.FromArgb(50,50,50)),
                ColorToRgba32(Color.FromArgb(50,50,50)),
                ColorToRgba32(Color.FromArgb(50,50,50)),
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.FromArgb(100,100,100)),
                ColorToRgba32(Color.FromArgb(100,100,100)),
                ColorToRgba32(Color.FromArgb(100,100,100)),
                ColorToRgba32(Color.FromArgb(100,100,100)),
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.White),
                ColorToRgba32(Color.White),
            };
        }
    }
}
