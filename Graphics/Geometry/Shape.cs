using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Geometry
{
    public class Shape
    {
        #region utils
        public static int ColorToRgba32(Color c)
        {
            return (int)((c.A << 24) | (c.B << 16) | (c.G << 8) | c.R);
        }
        #endregion

        private Vector3[] vertices, normals;
        private Vector2[] texcoords;
        private int[] indices;
        private int[] colors;

        public Vector3[] Vertices
        {
            get { return vertices; }
            protected set
            {
                vertices = value;
            }
        }

        public Vector3[] Normals
        {
            get { return normals; }
            protected set
            {
                normals = value;
            }
        }

        public Vector2[] Texcoords
        {
            get { return texcoords; }
            protected set
            {
                texcoords = value;
            }
        }

        public int[] Indices
        {
            get { return indices; }
            protected set
            {
                indices = value;
            }
        }

        public int[] Colors
        {
            get { return colors; }
            protected set
            {
                colors = value;
            }
        }

        public static Shape Merge(Shape[] shapes)
        {
            Shape shape = new Shape();
            shape.Vertices = shapes.SelectMany(s => s.Vertices).ToArray();
            shape.Colors = shapes.SelectMany(s => s.Colors).ToArray();
            shape.Normals = shapes.SelectMany(s => s.Normals).ToArray();
            int c = 0, offset = 0;
            shape.Indices = shapes.SelectMany(s =>
            {
                offset += shapes[c++].Vertices.Length;
                return s.Indices.Select(i => i + offset);
            }).ToArray();
            shape.Texcoords = shapes.SelectMany(s => s.Texcoords).ToArray();
            return shape;
        }
    }
}
