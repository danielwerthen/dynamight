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
        private IEnumerable<Vector3> MakeVertices(Point p, float size)
        {
            yield return new Vector3((p.X + 0f) * size, -(p.Y + 0f) * size, 0.0f);
            yield return new Vector3((p.X + 0f) * size, -(p.Y + 1f) * size, 0.0f);
            yield return new Vector3((p.X + 1f) * size, -(p.Y + 1f) * size, 0.0f);
            yield return new Vector3((p.X + 1f) * size, -(p.Y + 0f) * size, 0.0f);
        }

        private IEnumerable<int> MakeIndices(Point p, int width)
        {
            yield return (p.X + p.Y * width)*4 + 0;
            yield return (p.X + p.Y * width)*4 + 1;
            yield return (p.X + p.Y * width)*4 + 2;
            yield return (p.X + p.Y * width)*4 + 2;
            yield return (p.X + p.Y * width)*4 + 3;
            yield return (p.X + p.Y * width)*4 + 0;
        }

        IEnumerable<Vector3> MakeNormals(Point p)
        {
            yield return new Vector3(-1.0f, -1.0f, 1.0f);
            yield return new Vector3( 1.0f, -1.0f,  1.0f);
            yield return new Vector3( 1.0f,  1.0f,  1.0f);
            yield return new Vector3(-1.0f,  1.0f,  1.0f);
        }

        IEnumerable<int> MakeColors(Point p)
        {
            var h = p.X % 2 == 0;
            var v = p.Y % 2 == 0;
            int c = 0;
            if (v)
                c = h ? ColorToRgba32(Color.FromArgb(50, 50, 50)) : ColorToRgba32(Color.White);
            else
                c = h ? ColorToRgba32(Color.White) : ColorToRgba32(Color.FromArgb(50, 50, 50));
            for (var i = 0; i < 4; i++)
                yield return c;
        }

        public Checkerboard(Size pattern, float size)
        {
            var corners = new Point[pattern.Width * pattern.Height];
            int c = 0;
            for (var y = 0; y < pattern.Height; y++)
                for (var x = 0; x < pattern.Width; x++)
                    corners[c++] = new Point(x, y);

            Vertices = corners.SelectMany(p => MakeVertices(p, size)).ToArray();
            Indices = corners.SelectMany(p => MakeIndices(p, pattern.Width)).ToArray();
            Normals = corners.SelectMany(p => MakeNormals(p)).ToArray();
            Colors = corners.SelectMany(p => MakeColors(p)).ToArray();
            Colors.ToArray();
            //Vertices = new Vector3[]
            //{
            //    new Vector3(0.0f, -0.0f, -0.0f),
            //    new Vector3(0.0f, -0.1f, -0.0f),
            //    new Vector3(0.1f, -0.1f, -0.0f),
            //    new Vector3(0.1f, -0.0f, -0.0f),
            //    new Vector3(0.0f, -0.1f, -0.0f),
            //    new Vector3(0.0f, -0.2f, -0.0f),
            //    new Vector3(0.1f, -0.2f, -0.0f),
            //    new Vector3(0.1f, -0.1f, -0.0f),
            //    new Vector3(0.1f, -0.1f, -0.0f),
            //    new Vector3(0.1f, -0.2f, -0.0f),
            //    new Vector3(0.2f, -0.2f, -0.0f),
            //    new Vector3(0.2f, -0.1f, -0.0f),
            //    new Vector3(0.1f, -0.0f, -0.0f),
            //    new Vector3(0.1f, -0.1f, -0.0f),
            //    new Vector3(0.2f, -0.1f, -0.0f),
            //    new Vector3(0.2f, -0.0f, -0.0f),
            //};

            //Indices = new int[]
            //{
            //    // front face
            //    0, 1, 2, 2, 3, 0,
            //    4, 5, 6, 6, 7, 4,
            //    8, 9, 10, 10, 11, 8,
            //    12, 13, 14, 14, 15, 12,
            //};

            //Normals = new Vector3[]
            //{
            //    new Vector3(-1.0f, -1.0f,  1.0f),
            //    new Vector3( 1.0f, -1.0f,  1.0f),
            //    new Vector3( 1.0f,  1.0f,  1.0f),
            //    new Vector3(-1.0f,  1.0f,  1.0f),
            //    new Vector3(-1.0f, -1.0f,  1.0f),
            //    new Vector3( 1.0f, -1.0f,  1.0f),
            //    new Vector3( 1.0f,  1.0f,  1.0f),
            //    new Vector3(-1.0f,  1.0f,  1.0f),
            //    new Vector3(-1.0f, -1.0f,  1.0f),
            //    new Vector3( 1.0f, -1.0f,  1.0f),
            //    new Vector3( 1.0f,  1.0f,  1.0f),
            //    new Vector3(-1.0f,  1.0f,  1.0f),
            //    new Vector3(-1.0f, -1.0f,  1.0f),
            //    new Vector3( 1.0f, -1.0f,  1.0f),
            //    new Vector3( 1.0f,  1.0f,  1.0f),
            //    new Vector3(-1.0f,  1.0f,  1.0f),
            //};

            //Colors = new int[]
            //{
            //    ColorToRgba32(Color.FromArgb(50,50,50)),
            //    ColorToRgba32(Color.FromArgb(50,50,50)),
            //    ColorToRgba32(Color.FromArgb(50,50,50)),
            //    ColorToRgba32(Color.FromArgb(50,50,50)),
            //    ColorToRgba32(Color.White),
            //    ColorToRgba32(Color.White),
            //    ColorToRgba32(Color.White),
            //    ColorToRgba32(Color.White),
            //    ColorToRgba32(Color.FromArgb(100,100,100)),
            //    ColorToRgba32(Color.FromArgb(100,100,100)),
            //    ColorToRgba32(Color.FromArgb(100,100,100)),
            //    ColorToRgba32(Color.FromArgb(100,100,100)),
            //    ColorToRgba32(Color.White),
            //    ColorToRgba32(Color.White),
            //    ColorToRgba32(Color.White),
            //    ColorToRgba32(Color.White),
            //};
        }
    }
}
