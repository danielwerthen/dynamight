using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.RemoteSlave
{
    public class DepthImageEventArgs : EventArgs
    {
        public DepthImagePixel[] Pixels;
        public DepthImageFormat Format;
    }

    public class RemoteKinect
    {
        UdpClient slave;
        IPAddress ip;
        int port;
        int innerPort = 10499;
        public RemoteKinect(string hostname, int port)
        {
            slave = new UdpClient(innerPort);
            slave.Connect(hostname, port);
            this.ip = (slave.Client.RemoteEndPoint as IPEndPoint).Address;
            this.port = (slave.Client.RemoteEndPoint as IPEndPoint).Port;
        }

        public event EventHandler<DepthImageEventArgs> ReceivedDepthImage;

        public RemoteKinect(IPEndPoint ip)
        {
            slave = new UdpClient();
            slave.Connect(ip);
            this.ip = (slave.Client.RemoteEndPoint as IPEndPoint).Address;
            this.port = (slave.Client.RemoteEndPoint as IPEndPoint).Port;
        }

        private int BufferLength(Commands command)
        {
            switch (command)
            {
                case Commands.Depth80:
                    return 80 * 60 * sizeof(Int16);
                case Commands.Depth320:
                    return 320 * 240 * sizeof(Int16);
                case Commands.Depth640:
                    return 640 * 480 * sizeof(Int16);
                default:
                    return 0;
            }
        }

        private byte[] ReceiveFullBuffer(Commands command, byte[] leftovers, out byte[] rest)
        {
            var length = BufferLength(command);
            if (length <= leftovers.Length)
            {
                rest = leftovers.Skip(length).ToArray();
                return leftovers.Take(length).ToArray();
            }
            else
            {
                while (length > leftovers.Length)
                {
                    var point = new IPEndPoint(ip, port);
                    var data = slave.Receive(ref point);
                    leftovers = leftovers.Concat(data).ToArray();
                }
                if (length <= leftovers.Length)
                {
                    rest = leftovers.Skip(length).ToArray();
                    return leftovers.Take(length).ToArray();
                }
                else
                    throw new Exception("Congrats, this should not happen.");

            }
        }

        private DepthImagePixel[] ByteToDepthImagePixel(byte[] data, int length)
        {
            DepthImagePixel[] output = new DepthImagePixel[length];
            unsafe
            {
                fixed (byte* p = data)
                {
                    IntPtr ptr = (IntPtr)p;
                    short* pi = (short*)ptr.ToPointer();
                    for (int i = 0; i < length; i++)
                        output[i] = new DepthImagePixel()
                        {
                            PlayerIndex = (short)((int)*(pi + i) & DepthImageFrame.PlayerIndexBitmask),
                            Depth = (short)((int)*(pi + i) >> DepthImageFrame.PlayerIndexBitmaskWidth)
                        };
                }
            }
            return output;
        }

        private void HandlePackage(Commands command, byte[] data)
        {
            var length = BufferLength(command);
            if (length != data.Length)
                throw new Exception("Congrats, this should not happen.");
            if (command == Commands.Depth80)
            {
                var format = DepthImageFormat.Resolution80x60Fps30;
                DepthImagePixel[] pixels = ByteToDepthImagePixel(data, 80 * 60);
                if (ReceivedDepthImage != null)
                    ReceivedDepthImage(this, new DepthImageEventArgs { Format = format, Pixels = pixels });
            }
            else if (command == Commands.Depth320)
            {
                var format = DepthImageFormat.Resolution320x240Fps30;
                DepthImagePixel[] pixels = ByteToDepthImagePixel(data, 320 * 240);
                if (ReceivedDepthImage != null)
                    ReceivedDepthImage(this, new DepthImageEventArgs { Format = format, Pixels = pixels });
            }
            else if (command == Commands.Depth640)
            {
                var format = DepthImageFormat.Resolution640x480Fps30;
                DepthImagePixel[] pixels = ByteToDepthImagePixel(data, 640 * 480);
                if (ReceivedDepthImage != null)
                    ReceivedDepthImage(this, new DepthImageEventArgs { Format = format, Pixels = pixels });
            }
            else
            {
                throw new NotImplementedException("Please wait");
            }
        }

        private void StartReceiving(CancellationToken token)
        {
            int csize = sizeof(Commands);
            receiver = Task.Run(() =>
            {
                byte[] buffer = new byte[0];
                while (!token.IsCancellationRequested)
                {
                    var test = slave.Client.LocalEndPoint as IPEndPoint;
                    var point = new IPEndPoint(IPAddress.Any, test.Port);
                    byte[] commandHeader;
                    if (buffer.Length >= csize)
                    {
                        commandHeader = buffer.Take(csize).ToArray();
                        buffer = buffer.Skip(csize).ToArray();
                    }
                    else
                    {
                        buffer = buffer.Concat(slave.Receive(ref point)).ToArray();
                        continue;
                    }
                    var command = Program.ToCommand(commandHeader.Take(csize).ToArray());
                    var data = ReceiveFullBuffer(command, commandHeader.Skip(csize).ToArray(), out buffer);
                    HandlePackage(command, data);
                }
            });
        }

        Task receiver = null;
        CancellationTokenSource stopper = new CancellationTokenSource();
        public void Start(Commands command)
        {
            stopper.Cancel();
            if (receiver != null)
                receiver.Wait();
            slave.Send(Program.ToBytes(command), sizeof(int));

            //var test = slave.Client.LocalEndPoint as IPEndPoint;
            //var point = new IPEndPoint(IPAddress.Any, innerPort);
            
            //var test2 = slave.Receive(ref point);
            stopper = new CancellationTokenSource();
            StartReceiving(stopper.Token);
        }

        public void Stop()
        {
            stopper.Cancel();
            if (receiver != null)
                receiver.Wait();
            receiver = null;
            slave.Send(Program.ToBytes(Commands.End), sizeof(int));
        }
    }
}
