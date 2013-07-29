using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamight.RemoteSlave
{
    public class RemoteTCPSlave
    {
        public const int BUFFER_LENGTH = 8 * 1024;
        public const int COMMAND_LENGTH = sizeof(Commands);
        private TcpListener listener;
        private List<Task> clientListeners = new List<Task>();
        private ConcurrentDictionary<Commands, List<NetworkStream>> writers = new ConcurrentDictionary<Commands, List<NetworkStream>>();
        private CancellationTokenSource _cancel;
        public RemoteTCPSlave(int port)
        {
            listener = new TcpListener(IPAddress.Loopback,port);
            _cancel = new CancellationTokenSource();
            listener.Start();
        }

        public NetworkStream[] this[Commands command]
        {
            get
            {
                if (_cancel.IsCancellationRequested)
                    return new NetworkStream[0];
                else if (!writers.ContainsKey(command))
                    return new NetworkStream[0];
                else
                    return writers[command].ToArray();
            }
        }

        private void HandleCommand(NetworkStream stream, Commands command, CancellationToken token)
        {
            foreach (var scommand in RemoteSlave.Split(command))
            {
                if (!writers.ContainsKey(scommand))
                    writers[scommand] = new List<NetworkStream>();
                writers[scommand].Add(stream);
            }
        }

        private void CloseWriters(TcpClient client, NetworkStream stream)
        {
            foreach (var list in writers.Values)
                list.Remove(stream);
        }

        private Task ListenToClient(TcpClient client, CancellationToken token)
        {
            return Task.Run(() =>
            {
                var stream = client.GetStream();
                byte[] commandBuffer = new byte[COMMAND_LENGTH];
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var reading = stream.ReadAsync(commandBuffer, 0, COMMAND_LENGTH);
                        reading.Wait(token);
                        if (!reading.IsCompleted || reading.Result != COMMAND_LENGTH)
                            continue;
                        var command = RemoteSlave.ToCommand(commandBuffer);
                        if (command == Commands.End)
                        {
                            CloseWriters(client, stream);
                            break;
                        }
                        else
                            HandleCommand(stream, command, token);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        CloseWriters(client, stream);
                        break;
                    }
                }
                client.Close();
            });
        }

        private void HandleClient(TcpClient client, CancellationToken token)
        {
            clientListeners.Add(ListenToClient(client, token));
        }

        public async Task ProcessIncomingClients(CancellationToken token)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                HandleClient(client, token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Close()
        {
            listener.Stop();
            foreach (var t in clientListeners)
                t.Wait();
        }
    }
}
