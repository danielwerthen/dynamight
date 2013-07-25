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
    public enum Commands
    {
        End = 0,
        Depth80 = 1,
        Depth320 = 2,
        Depth640 = 4,
        Skeleton = 8,
        Color1280 = 16,
        Color640 = 32
    }

    public class Program
    {
        public static byte[] ToBytes(Commands command)
        {
            int intValue = (int)command;
            byte[] intBytes = BitConverter.GetBytes(intValue);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            return intBytes;
        }

        public static Commands ToCommand(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return (Commands)BitConverter.ToInt32(bytes, 0);
        }

        static void RunCommandListener(UdpClient client, int port, Action<Commands, IPEndPoint> onCommand, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
                var bytes = client.Receive(ref endPoint);
                if (bytes.Length == 4)
                    onCommand(ToCommand(bytes), endPoint);
            }
            client.Close();
        }

        static void Enable(KinectSensor sensor, Commands command)
        {
            var dc = GetDepthCommand(command);
            if (dc.HasValue)
            {
                if (dc.Value == Commands.Depth80)
                    sensor.DepthStream.Enable(DepthImageFormat.Resolution80x60Fps30);
                else if (dc.Value == Commands.Depth320)
                    sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                else if (dc.Value == Commands.Depth640)
                    sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            }

            if ((Commands.Color640 & command) == Commands.Color640)
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            else if ((Commands.Color1280 & command) == Commands.Color1280)
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);

            if (SkeletonCommand(command))
            {
                var parameters = new TransformSmoothParameters
                {
                    Smoothing = 0.5f,
                    Correction = 0.1f,
                    Prediction = 1.1f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.05f
                };
                sensor.SkeletonStream.Enable(parameters);
            }
        }

        static bool IsDepthCommand(Commands command)
        {
            if ((Commands.Depth80 & command) == Commands.Depth80
                | (Commands.Depth320 & command) == Commands.Depth320
                | (Commands.Depth640 & command) == Commands.Depth640)
                return true;
            return false;
        }

        static Commands? GetDepthCommand(Commands command)
        {
            if ((Commands.Depth80 & command) == Commands.Depth80)
                return Commands.Depth80;
            else if ((Commands.Depth320 & command) == Commands.Depth320)
                return Commands.Depth320;
            else if ((Commands.Depth640 & command) == Commands.Depth640)
                return Commands.Depth640;
            return null;
        }

        static bool ColorCommand(Commands command)
        {
            if ((Commands.Color640 & command) == Commands.Color640
                | (Commands.Color1280 & command) == Commands.Color1280)
                return true;
            return false;
        }

        static bool SkeletonCommand(Commands command)
        {
            if ((Commands.Skeleton & command) == Commands.Skeleton)
                return true;
            return false;
        }

        private static DepthImagePixel[] ByteToDepthImagePixel(byte[] data, int length)
        {
            DepthImagePixel[] output = new DepthImagePixel[length];
            unsafe
            {
                fixed (byte* p = data)
                {
                    IntPtr ptr = (IntPtr)p;
                    DepthImagePixel* pi = (DepthImagePixel*)ptr.ToPointer();
                    for (int i = 0; i < length; i++)
                        output[i] = *(pi + i);
                }
            }
            return output;
        }

        static void SendDepth(UdpClient client, DepthImageStream stream, Commands command, IPEndPoint destination)
        {
            using (var frame = stream.OpenNextFrame(50))
            {
                if (frame == null)
                    return;

                var dc = GetDepthCommand(command);
                if (!dc.HasValue)
                    return;

                client.Send(ToBytes(dc.Value), sizeof(int), destination);
                byte[] pixelData = new byte[frame.PixelDataLength * frame.BytesPerPixel];
                unsafe
                {
                    fixed (byte* p = pixelData)
                    {
                        IntPtr ptr = (IntPtr)p;
                        frame.CopyPixelDataTo((IntPtr)ptr, frame.PixelDataLength);
                    }
                }
                client.Send(pixelData, pixelData.Length, destination);

            }
        }

        static void SendColor(UdpClient client, ColorImageStream stream, Commands command, IPEndPoint destination)
        {
            using (var frame = stream.OpenNextFrame(50))
            {
                if (frame == null)
                    return;

                //var cc = GetColorCommand(command);
            }
        }

        static void CommandRunner(UdpClient client, IPEndPoint destination, Commands commands, CancellationToken token)
        {
            KinectSensor sensor = KinectSensor.KinectSensors.First(row => row.Status == KinectStatus.Connected);
            sensor.Start();
            
            Enable(sensor, commands);
            while (!token.IsCancellationRequested)
            {
                if (IsDepthCommand(commands))
                    SendDepth(client, sensor.DepthStream, commands, destination);
            }
        }

        static void Main(string[] args)
        {
            int commandPort = args.Length > 0 ? int.Parse(args.First()) : 10500;
            CancellationTokenSource ender = new CancellationTokenSource();
            Task commandRunner = null;
            CancellationTokenSource commandEnder = new CancellationTokenSource();
            UdpClient client = new UdpClient(commandPort);
            Action<Commands, IPEndPoint> onCommand = (c, ip) =>
            {
                commandEnder.Cancel();
                if (commandRunner != null)
                    commandRunner.Wait();
                commandEnder = new CancellationTokenSource();
                commandRunner = Task.Run(() => CommandRunner(client, ip, c, commandEnder.Token));
            };
            Task.Run(() => RunCommandListener(client, commandPort, onCommand, ender.Token));
            Thread.Sleep(500);
            Console.ReadLine();
            ender.Cancel();
            UdpClient clientend = new UdpClient();
            clientend.Send(ToBytes(Commands.End), sizeof(int), new IPEndPoint(IPAddress.Loopback, commandPort));
        }
    }
}
