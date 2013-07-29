using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
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

    public class RemoteSlave
    {
        public const int SKELETON_BUFFER_LENGTH = 8535;
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

            if (IsSkeletonCommand(command))
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

        public static IEnumerable<Commands> Split(Commands commands)
        {
            var all = (Commands[])Enum.GetValues(typeof(Commands));
            return all.Where(c => (c & commands) == c);
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

        static Commands? GetColorCommand(Commands command)
        {
            if ((Commands.Color1280 & command) == Commands.Color1280)
                return Commands.Color1280;
            else if ((Commands.Color640 & command) == Commands.Color640)
                return Commands.Color640;
            return null;
        }

        static bool IsColorCommand(Commands command)
        {
            if ((Commands.Color640 & command) == Commands.Color640
                | (Commands.Color1280 & command) == Commands.Color1280)
                return true;
            return false;
        }

        static bool IsSkeletonCommand(Commands command)
        {
            if ((Commands.Skeleton & command) == Commands.Skeleton)
                return true;
            return false;
        }

        static Commands? GetSkeletonCommand(Commands command)
        {
            if ((Commands.Skeleton & command) == Commands.Skeleton)
                return Commands.Skeleton;
            return null;
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

        static void SendDepth(KinectSensor sensor, NetworkStream[] streams, Commands command)
        {
            using (var frame = sensor.DepthStream.OpenNextFrame(50))
            {
                if (frame == null)
                    return;
                var dc = GetDepthCommand(command);
                if (!dc.HasValue)
                    return;
                byte[] pixelData = new byte[frame.PixelDataLength * frame.BytesPerPixel];
                unsafe
                {
                    fixed (byte* p = pixelData)
                    {
                        IntPtr ptr = (IntPtr)p;
                        frame.CopyPixelDataTo((IntPtr)ptr, frame.PixelDataLength);
                    }
                }
                foreach (var stream in streams)
                {
                    stream.Write(ToBytes(dc.Value), 0, sizeof(Commands));
                    stream.Write(pixelData, 0, pixelData.Length);
                }
            }
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
                Send(client, pixelData, destination);
            }
        }

        static void SendColor(KinectSensor sensor, NetworkStream[] streams, Commands command)
        {
            using (var frame = sensor.ColorStream.OpenNextFrame(50))
            {
                if (frame == null)
                    return;

                var cc = GetColorCommand(command);
                if (!cc.HasValue)
                    return;

                byte[] pixelData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(pixelData);
                foreach (var stream in streams)
                {
                    try
                    {
                        stream.Write(ToBytes(cc.Value), 0, sizeof(int));
                        stream.Write(pixelData, 0, pixelData.Length);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        static void SendColor(UdpClient client, ColorImageStream stream, Commands command, IPEndPoint destination)
        {
            using (var frame = stream.OpenNextFrame(50))
            {
                if (frame == null)
                    return;

                var cc = GetColorCommand(command);
                if (!cc.HasValue)
                    return;

                client.Send(ToBytes(cc.Value), sizeof(int), destination);
                byte[] pixelData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(pixelData);
                Send(client, pixelData, destination);
            }
        }

        static void Send(UdpClient client, byte[] data, IPEndPoint destination, int bufferSize = 8192)
        {
            for (var i = 0; i < data.Length; i += bufferSize)
            {
                client.Send(data.Skip(i).Take(bufferSize).ToArray(), Math.Min(data.Length - i, bufferSize), destination);
            }
        }

        static void SendSkeleton(KinectSensor sensor, NetworkStream[] streams, Commands command)
        {
            using (var frame = sensor.SkeletonStream.OpenNextFrame(50))
            {
                if (frame == null)
                    return;

                var sc = GetSkeletonCommand(command);
                if (!sc.HasValue)
                    return;

                Skeleton[] data = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(data);
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(ms, data);
                    var toSend = ms.ToArray();
                    foreach (var stream in streams)
                    {
                        stream.Write(ToBytes(sc.Value), 0, sizeof(Commands));
                        stream.Write(toSend, 0, toSend.Length);
                        var empty = new byte[Math.Max(SKELETON_BUFFER_LENGTH - toSend.Length, 0)];
                        if (empty.Length > 0)
                            stream.Write(empty, 0, empty.Length);
                    }
                }
            }
        }

        static void SendSkeleton(UdpClient client, SkeletonStream stream, Commands command, IPEndPoint destination)
        {
            using (var frame = stream.OpenNextFrame(50))
            {
                if (frame == null)
                    return;

                var sc = GetSkeletonCommand(command);
                if (!sc.HasValue)
                    return;

                client.Send(ToBytes(sc.Value), sizeof(int), destination);
                Skeleton[] data = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(data);
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(ms, data);
                    var toSend = ms.ToArray();
                    Send(client, toSend, destination);
                    var empty = new byte[Math.Max(SKELETON_BUFFER_LENGTH - toSend.Length, 0)];
                    if (empty.Length > 0)
                        Send(client, empty, destination);
                }
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
                if (IsColorCommand(commands))
                    SendColor(client, sensor.ColorStream, commands, destination);
                if (IsSkeletonCommand(commands))
                    SendSkeleton(client, sensor.SkeletonStream, commands, destination);
            }
        }

        static void HandleDepthStreams(RemoteTCPSlave tcp, KinectSensor sensor)
        {

            NetworkStream[] streams = tcp[Commands.Depth80];
            Commands command = Commands.Depth80;
            if (streams.Length == 0)
            {
                streams = tcp[Commands.Depth320];
                if (streams.Length == 0)
                {
                    streams = tcp[Commands.Depth640];
                    if (streams.Length == 0)
                        return;
                    else
                        command = Commands.Depth640;
                }
                else
                    command = Commands.Depth320;
            }
            Enable(sensor, command);
            SendDepth(sensor, streams, command);
        }

        static void HandleColorStreams(RemoteTCPSlave tcp, KinectSensor sensor)
        {
            NetworkStream[] streams = tcp[Commands.Color640];
            Commands command = Commands.Color640;
            if (streams.Length == 0)
            {
                streams = tcp[Commands.Color1280];
                if (streams.Length == 0)
                {
                    return;
                }
                else
                    command = Commands.Color1280;
            }
            Enable(sensor, command);
            SendColor(sensor, streams, command);
        }

        static void HandleSkeletonStreams(RemoteTCPSlave tcp, KinectSensor sensor)
        {
            var streams = tcp[Commands.Skeleton];
            if (streams.Length == 0)
                return;
            Enable(sensor, Commands.Skeleton);
            SendSkeleton(sensor, streams, Commands.Skeleton);
        }

        static void Main(string[] args)
        {
            int commandPort = args.Length > 0 ? int.Parse(args.First()) : 10500;
            RemoteTCPSlave tcp = new RemoteTCPSlave(commandPort);
            CancellationTokenSource ender = new CancellationTokenSource();
            Console.WriteLine("Ctrl-C to exit!");
            Console.CancelKeyPress += (o, e) =>
            {
                ender.Cancel();
            };
            KinectSensor sensor = KinectSensor.KinectSensors.First(s => s.Status == KinectStatus.Connected);
            sensor.Start();
            Task proc = tcp.ProcessIncomingClients(ender.Token);
            while (!ender.IsCancellationRequested)
            {
                if (proc.IsCompleted)
                    proc = tcp.ProcessIncomingClients(ender.Token);
                HandleDepthStreams(tcp, sensor);
                HandleColorStreams(tcp, sensor);
                HandleSkeletonStreams(tcp, sensor);
                Thread.Yield();
            }
            tcp.Close();
        }

        static void Main2(string[] args)
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
