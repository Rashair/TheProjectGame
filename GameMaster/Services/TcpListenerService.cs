using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Managers;
using GameMaster.Models;
using Serilog;
using Shared.Messages;

namespace GameMaster.Services
{
    public abstract class TcpListenerService : WaitForInitService
    {
        private const int BufferSize = 1024 * 4;
        private readonly GameConfiguration conf;
        protected readonly ISocketManager<TcpClient, GMMessage> manager;

        public TcpListenerService(ILogger logger, GM gameMaster, GameConfiguration conf,
           ISocketManager<TcpClient, GMMessage> manager)
            : base(gameMaster, logger)
        {
            this.conf = conf;
            this.manager = manager;
        }

        protected abstract Task OnMessageAsync(TcpClient socket, byte[] buffer, int count);

        protected virtual void OnConnected(TcpClient socket)
        {
            bool result = manager.AddSocket(socket);
        }

        protected virtual async Task OnDisconnectedAsync(TcpClient socket, CancellationToken cancellationToken)
        {
            await manager.RemoveSocketAsync(manager.GetId(socket), cancellationToken);
        }

        protected virtual bool AcceptConnection()
        {
            return !gameMaster.WasGameStarted;
        }

        protected override async Task RunService(CancellationToken cancellationToken)
        {
            var ip = IPAddress.Parse(conf.CsIP);
            var listener = new TcpListener(ip, conf.CsPort);
            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested && AcceptConnection())
                {
                    if (listener.Pending())
                    {
                        var client = await listener.AcceptTcpClientAsync();
                        OnConnected(client);
                        logger.Information($"Client: {client.Client.RemoteEndPoint} connected.");

                        var stream = client.GetStream();
                        var buffer = new byte[BufferSize];

                        // Read JoinTheGameMessage, blocking!
                        int count = stream.Read(buffer, 0, BufferSize);
                        await OnMessageAsync(client, buffer, count);

                        var receiveTask = Task.Run(async () =>
                        {
                            while (!cancellationToken.IsCancellationRequested && client.Connected)
                            {
                                if (stream.DataAvailable)
                                {
                                    {
                                        int countRead = await stream.ReadAsync(buffer, 0, BufferSize);
                                        await OnMessageAsync(client, buffer, countRead);
                                    }
                                }
                            }
                            stream.Close();
                        }, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(500);
                    }
                }
            }, cancellationToken);
        }
    }
}
