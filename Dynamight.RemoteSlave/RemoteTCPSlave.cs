using System;
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
        private Dictionary<Commands, List<NetworkStream>> writers = new Dictionary<Commands, List<NetworkStream>>();
        private CancellationTokenSource _cancel;
        public RemoteTCPSlave(int port)
        {
            listener = new TcpListener(IPAddress.Loopback,port);
            _cancel = new CancellationTokenSource();

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

        private Task ListenToClient(TcpClient client)
        {
            var token = _cancel.Token;
            return Task.Run(() =>
            {
                var stream = client.GetStream();
                byte[] commandBuffer = new byte[COMMAND_LENGTH];
                while (!token.IsCancellationRequested)
                {
                    var reading = stream.ReadAsync(commandBuffer, 0, COMMAND_LENGTH);
                    reading.Wait(token);
                    if (!reading.IsCompleted || reading.Result != COMMAND_LENGTH)
                        continue;
                    HandleCommand(stream, RemoteSlave.ToCommand(commandBuffer), token);
                }
                client.Close();
            });
        }

        private void HandleClient(TcpClient client)
        {
            clientListeners.Add(ListenToClient(client));
        }

        public async Task ProcessIncomingClients()
        {
            var client = await listener.AcceptTcpClientAsync();
            HandleClient(client);
        }

        public void Start(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Task proc = ProcessIncomingClients();
                proc.Wait(token);
                if (proc.IsCompleted)
                    continue;
            }
            _cancel.Cancel();
        }
    }
}
