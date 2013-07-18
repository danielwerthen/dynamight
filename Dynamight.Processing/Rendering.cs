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

        public void Render(CompositePlayer[] players, KinectSensor sensor, DepthImageFormat format)
        {
            var test = players.Where(p => p.PlayerId > 0)
                .SelectMany(player =>
                {
                    var verts = player.DepthPoints.Select(dp => sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(format, dp))
                        .Select(sp => new Vector3(sp.X, sp.Y, sp.Z)).ToArray();
                    Vector3[] normals = null;
                    Color[] colors = null;
                    if (player.Skeleton == null || player.Skeleton.TrackingState != Microsoft.Kinect.SkeletonTrackingState.Tracked)
                        retur
                    player.DepthPoints
                });
        }
    }
}
