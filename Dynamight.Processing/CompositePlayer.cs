using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.Processing
{
    public struct CompositePlayer
    {
        public DepthImagePoint[] DepthPoints;
        public Skeleton Skeleton;
        public int PlayerId;
    }
}
