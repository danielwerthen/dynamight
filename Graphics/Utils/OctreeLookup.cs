using OctreeSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphics.Utils
{
    public class OctreeLookup : IDisposable
    {
        Octree tree;
        public OctreeLookup(IEnumerable<float[]> points)
        {
            tree = new Octree(
                points.Max(p => p[0]),
                points.Min(p => p[0]),
                points.Max(p => p[1]),
                points.Min(p => p[1]),
                points.Max(p => p[2]),
                points.Min(p => p[2]),
                points.Count());
            foreach (var p in points)
                tree.AddNode(p[0], p[1], p[2], p);
        }

        public IEnumerable<float[]> Neighbours(float[] p, float rad)
        {
            return tree.GetNodes(p[0], p[1], p[2], rad).ToArray().Select(o => (float[])o).ToArray();
        }

        public void Dispose()
        {
            tree.Clear();
            tree = null;
        }
    }
}
