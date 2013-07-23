using Microsoft.Kinect;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.Processing
{
    public enum BoneType
    {
        HandLeft,
        ForearmLeft,
        UpperarmLeft,
        HandRight,
        ForearmRight,
        UpperarmRight,
        ShoulderLeft,
        ShoulderRight,
        UpperLegLeft,
        UpperLegRight,
        LowerLegLeft,
        LowerLegRight,
        FootLeft,
        FootRight,
        HipLeft,
        HipRight,
        Pelvis,
        Spine,
        Head
    }

    public class BoneCollection : IEnumerable<Bone>
    {
        IEnumerable<Bone> bones;
        Dictionary<BoneType, Bone> lookup;
        Dictionary<JointType, Bone[]> jointLookup;
        IEnumerable<Joint> joints;
        public BoneCollection(IEnumerable<Bone> bones)
        {
            joints = bones.Select(b => new { j = b.From.JointType, b = b.From })
                .Concat(bones.Select(b => new { j = b.To.JointType, b = b.To }))
                .GroupBy(pair => pair.j)
                .Select(group => group.Select(g => g.b).FirstOrDefault());
            lookup = bones.ToDictionary(b => b.Type);
            jointLookup = bones.Select(b => new { j = b.From.JointType, b = b })
                .Concat(bones.Select(b => new { j = b.To.JointType, b = b }))
                .GroupBy(pair => pair.j)
                .ToDictionary(group => group.Key, group => group.Select(g => g.b).ToArray());
        }

        public Bone this[BoneType type]
        {
            get { return lookup[type]; }
        }

        public Bone[] this[JointType type]
        {
            get { return jointLookup[type]; }
        }

        public IEnumerator<Bone> GetEnumerator()
        {
            return bones.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return bones.GetEnumerator();
        }

        public void Interpolate(Vector3 v, out Bone[] bones, out double[] scaling)
        {
            var closest = joints.Select(j => new { d = j.Distance(v), type = j.JointType }).OrderBy(j => j.d).Select(j => j.type).First();
            
            var bs = jointLookup[closest];
            var norms = bs.Select(b => new { other = b.To.JointType == closest ? b.From : b.To, b = b })
              .Select(p => new { val = p.other.Distance(v) / p.b.Length, b = p.b }).ToArray();
            if (norms.Length == 1)
            {
                bones = norms.Select(n => n.b).ToArray();
                scaling = new double[] { 1 };
                return;
            }
            var sum = norms.Select(n => n.val).Sum();
            bones = norms.Select(n => n.b).ToArray();
            scaling = norms.Select(n => (1 - (n.val / sum)) / (double)norms.Length).ToArray();
        }
    }

    public class BoneColorizer
    {
        public Color Colorize(Bone[] bones, double[] scales)
        {
            var f = bones.Zip(scales, (b, s) =>
                {
                    var c = this[b.Type];
                    return new double[] { c.R * s, c.G * s, c.B * s };
                }).Aggregate((a, b) => new double[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] });
            return Color.FromArgb((int)f[0], (int)f[1], (int)f[2]);
        }

        public Color this[BoneType type]
        {
            get 
            {
                switch (type)
                {
                    case BoneType.HandLeft:
                    case BoneType.HandRight:
                        return Color.FromArgb(255, 0, 0);
                    case BoneType.ForearmLeft:
                    case BoneType.ForearmRight:
                        return Color.FromArgb(255, 50, 50);
                    case BoneType.UpperarmLeft:
                    case BoneType.UpperarmRight:
                        return Color.FromArgb(255, 100, 100);
                    case BoneType.ShoulderLeft:
                    case BoneType.ShoulderRight:
                        return Color.FromArgb(255, 150, 150);
                    case BoneType.Head:
                        return Color.FromArgb(255, 250, 250);
                    case BoneType.Spine:
                    case BoneType.Pelvis:
                    case BoneType.HipLeft:
                    case BoneType.HipRight:
                        return Color.FromArgb(255, 150, 150);
                    case BoneType.UpperLegLeft:
                    case BoneType.UpperLegRight:
                        return Color.FromArgb(255, 100, 100);
                    case BoneType.LowerLegLeft:
                    case BoneType.LowerLegRight:
                        return Color.FromArgb(255, 50, 50);
                    case BoneType.FootLeft:
                    case BoneType.FootRight:
                        return Color.FromArgb(255, 0, 0);
                    default:
                        return Color.White;
                }
            }

        }
    }

    public static class JointOp
    {

        public static double Distance(this Joint j, Vector3 v)
        {
            var p = j.Position;
            return Math.Sqrt((v.X - p.X) * (v.X - p.X)
                + (v.Y - p.Y) * (v.Y - p.Y)
                + (v.Z - p.Z) * (v.Z - p.Z));
        }

        public static double Distance(this Joint j1, Joint j2)
        {
            var p = j1.Position;
            var v = j2.Position;
            return Math.Sqrt((v.X - p.X) * (v.X - p.X)
                + (v.Y - p.Y) * (v.Y - p.Y)
                + (v.Z - p.Z) * (v.Z - p.Z));
        }
    }

    public class Bone
    {
        public BoneType Type;
        public Bone(Joint j1, Joint j2, BoneType type)
        {
            From = j1;
            To = j2;
            Type = type;
            Length = j1.Distance(j2);
            if (Length <= 1)
                Length = 1;
            Dir = new Vector3d(  (To.Position.X - From.Position.X) / Length,
                                (To.Position.Y - From.Position.Y) / Length,
                                (To.Position.Z - From.Position.Z) / Length);
        }
        public Joint From;
        public Joint To;
        public double Length;
        public Vector3d Dir;

        //public static double[] Interpolate(IEnumerable<Bone> bones, Vector3 v)
        //{
        //    var dists = bones.Select(b => new { j1 = Distance(b.From, v), j2 = Distance(b.To, v), b = b }).ToArray();
        //    var interps = dists.Select(d =>
        //        {
        //            if (d.j1 < d.b.Length && d.j2 < d.b.Length)
        //                return d;
        //            return null;
        //        });
        //}

        public static double[] Normal(Vector3 p, double scale, Vector3 A, Vector3d Dir)
        {
            var s = (p.X - A.X) * Dir.X +
                (p.Y - A.Y) * Dir.Y +
                (p.Z - A.Z) * Dir.Z;

            var x = new Vector3d(A.X + s * Dir.X,
                A.Y + s * Dir.Y,
                A.Z + s * Dir.Z);
            var n = new double[] { 
                x.X - p.X, 
                x.Y - p.Y, 
                x.Z - p.Z };
            var nl = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
            return new double[] { -n[0] * nl * scale, -n[1] * nl * scale, -n[2] * nl * scale };
        }

        public double[] Normal(Vector3 p, double scale)
        {
            var a = new Vector3(this.From.Position.X, this.From.Position.Y, this.From.Position.Z);
            return Normal(p, scale, a, this.Dir);
        }

        public static Vector3 Normal(Vector3 v, Bone[] bones, double[] scales)
        {
            var ns = bones.Zip(scales, (b, s) => b.Normal(v, s)).ToArray();
            return new Vector3((float)(ns.Sum(n => n[0]) / (float)ns.Length),
                (float)(ns.Sum(n => n[1]) / (float)ns.Length),
                (float)(ns.Sum(n => n[2]) / (float)ns.Length));
        }

        public static BoneCollection Interpret(Skeleton skeleton)
        {
            return new BoneCollection(_Interpret(skeleton));
        }

        private static IEnumerable<Bone> _Interpret(Skeleton skeleton)
        {
            var js = skeleton.Joints;
            yield return new Bone(js[JointType.HandLeft],   js[JointType.WristLeft], BoneType.HandLeft);
            yield return new Bone(js[JointType.HandRight],  js[JointType.WristRight], BoneType.HandRight);
            yield return new Bone(js[JointType.WristLeft],   js[JointType.ElbowLeft], BoneType.ForearmLeft);
            yield return new Bone(js[JointType.WristRight],  js[JointType.ElbowRight], BoneType.ForearmRight);
            yield return new Bone(js[JointType.ElbowLeft],   js[JointType.ShoulderLeft], BoneType.UpperarmLeft);
            yield return new Bone(js[JointType.ElbowRight],  js[JointType.ShoulderRight], BoneType.UpperarmRight);
            yield return new Bone(js[JointType.ShoulderLeft],   js[JointType.ShoulderCenter], BoneType.ShoulderLeft);
            yield return new Bone(js[JointType.ShoulderRight],  js[JointType.ShoulderCenter], BoneType.ShoulderRight);
            yield return new Bone(js[JointType.HipLeft],   js[JointType.KneeLeft], BoneType.UpperLegLeft);
            yield return new Bone(js[JointType.HipRight],  js[JointType.KneeRight], BoneType.UpperLegRight);
            yield return new Bone(js[JointType.HipLeft],   js[JointType.HipCenter], BoneType.HipLeft);
            yield return new Bone(js[JointType.HipRight],  js[JointType.HipCenter], BoneType.HipRight);
            yield return new Bone(js[JointType.KneeLeft],   js[JointType.AnkleLeft], BoneType.LowerLegLeft);
            yield return new Bone(js[JointType.KneeRight],  js[JointType.AnkleRight], BoneType.LowerLegRight);
            yield return new Bone(js[JointType.AnkleLeft],   js[JointType.FootLeft], BoneType.FootLeft);
            yield return new Bone(js[JointType.AnkleRight],  js[JointType.FootRight], BoneType.FootRight);
            yield return new Bone(js[JointType.HipCenter], js[JointType.Spine], BoneType.Pelvis);
            yield return new Bone(js[JointType.Spine], js[JointType.ShoulderCenter], BoneType.Spine);
            yield return new Bone(js[JointType.ShoulderCenter], js[JointType.Head], BoneType.Head);
        }
    }


}
