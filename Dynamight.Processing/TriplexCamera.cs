﻿using Dynamight.ImageProcessing.CameraCalibration.Utils;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.Processing
{

    public class TriplexCamera
    {
        IDepthCamera depth;
        ISkeletonCamera skeleton;

        public TriplexCamera(IDepthCamera depth, ISkeletonCamera skeleton)
        {
            this.depth = depth;
            this.skeleton = skeleton;
        }

        public CompositePlayer[] Trigger(int wait)
        {
            var sp = Task.Run(() => skeleton.GetSkeletons(wait));
            var dp = Task.Run(() => 
                {
                    var result = depth.Get(wait, (pixel, point, size) =>
                    {
                        if (!pixel.IsKnownDepth)
                            return null;
                        var dip = new DepthImagePoint()
                        {
                            X = point.X,
                            Y = point.Y,
                            Depth = pixel.Depth
                        };
                        return (DepthImageIndexedPoint?)new DepthImageIndexedPoint()
                        {
                            Index = pixel.PlayerIndex,
                            Point = dip
                        };
                    }).ToArray().Where(p => p.HasValue).Select(p => p.Value).ToArray();
                    return result;
                });
            dp.Wait();
            var points = dp.Result;
            Skeleton[] skeletons = new Skeleton[0];
            if (points.Any(p => p.Index >= 1 && p.Index <= 6))
            {
                sp.Wait();
                skeletons = sp.Result;
            }
            return points.GroupBy(p => p.Index).Select(group => new CompositePlayer()
            {
                DepthPoints = group.Select(g => g.Point).ToArray(),
                PlayerId = group.Key,
                Skeleton = (group.Key >= 1 && group.Key <= 6) ? skeletons[group.Key - 1] : null
            }).ToArray();
            
        }
    }
}
