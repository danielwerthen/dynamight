using Graphics.Projection;
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
    public class Rendering
    {
        PointCloudProgram output;
        public Rendering(PointCloudProgram output)
        {
            this.output = output;
        }

        private static double Distance2(Joint j, Vector3 v)
        {
            var p = j.Position;
            return Math.Sqrt((v.X - p.X) * (v.X - p.X)
                + (v.Y - p.Y) * (v.Y - p.Y)
                + (v.Z - p.Z) * (v.Z - p.Z));
        }

        private static Vector3 Normal(Joint j, Vector3 v)
        {
            var p = j.Position;
            return new Vector3(v.X - p.X, v.Y - p.Y, v.Z - p.Z);
        }

        private static Joint Closest(IEnumerable<Joint> joints, Vector3 v)
        {
            return joints.Select(j => new { d = Distance2(j, v), Joint = j }).OrderBy(item => item.d).Select(item => item.Joint).First();
        }

        private static Color Colorize(Joint j)
        {
            switch (j.JointType)
            {
                case JointType.HandLeft:
                case JointType.HandRight:
                    return Color.FromArgb(255, 0, 0);
                case JointType.WristLeft:
                case JointType.WristRight:
                    return Color.FromArgb(255, 50, 50);
                case JointType.ElbowLeft:
                case JointType.ElbowRight:
                    return Color.FromArgb(255, 100, 100);
                case JointType.ShoulderLeft:
                case JointType.ShoulderRight:
                    return Color.FromArgb(255, 150, 150);
                case JointType.HipLeft:
                case JointType.HipRight:
                    return Color.FromArgb(255, 150, 150);
                case JointType.KneeLeft:
                case JointType.KneeRight:
                    return Color.FromArgb(255, 100, 100);
                case JointType.AnkleLeft:
                case JointType.AnkleRight:
                    return Color.FromArgb(255, 50, 50);
                case JointType.FootLeft:
                case JointType.FootRight:
                    return Color.FromArgb(255, 0, 0);
                case JointType.Head:
                case JointType.HipCenter:
                case JointType.ShoulderCenter:
                case JointType.Spine:
                    return Color.FromArgb(255, 200, 200);
            }
            return Color.White;
        }

        public void Render(CompositePlayer[] players, KinectSensor sensor, DepthImageFormat format)
        {
            var colorizer = new BoneColorizer();
            var data = players.Where(p => p.PlayerId > 0)
                .Select(player =>
                {
                    var verts = player.DepthPoints.Select(dp => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, dp))
                        .Select(sp => new Vector3(sp.X, sp.Y, sp.Z)).ToArray();
                    Vector3[] normals = player.DepthPoints.Select(_ => new Vector3(0, 0, -1)).ToArray();
                    Color[] colors = player.DepthPoints.Select(_ => Color.Gray).ToArray();

                    if (player.Skeleton != null && player.Skeleton.TrackingState == Microsoft.Kinect.SkeletonTrackingState.Tracked)
                    {
                        var bones = Bone.Interpret(player.Skeleton);
                        var interp = verts.Select(v =>
                            {
                                Bone[] close;
                                double[] scaling;
                                bones.Interpolate(v, out close, out scaling);
                                return new { bones = close, scaling = scaling, v = v };
                            }).ToArray();
                        colors = interp.Select(item => colorizer.Colorize(item.bones, item.scaling)).ToArray();
                        normals = interp.Select(item => Bone.Normal(item.v, item.bones, item.scaling)).ToArray();
                        //normals = verts.Select(v => Normal(Closest(player.Skeleton.Joints, v), v)).ToArray();
                        //colors = verts.Select(v => Closest(player.Skeleton.Joints, v)).Select(j => Colorize(j)).ToArray();
                    }
                    return new { Vertices = verts, Normals = normals, Colors = colors };
                }).ToArray();
            if (data.Length > 0)
            {
                output.SetPositions(data.SelectMany(d => d.Vertices).ToArray(),
                    data.SelectMany(d => d.Normals).ToArray(),
                    data.SelectMany(d => d.Colors).ToArray());
            }
            else
            {
                output.SetPositions(new float[0][]);
            }
        }
    }
}
