using Dynamight.RemoteSlave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.App
{
    public class RemoteApp
    {
        public static void Run(string[] args)
        {
            RemoteKinect kinect = new RemoteKinect("localhost", 10500);
            kinect.ReceivedDepthImage += (o, e) =>
            {
                e.Pixels.ToString();
            };
            kinect.Start(Commands.Depth80);
            Console.ReadLine();
        }
    }
}
