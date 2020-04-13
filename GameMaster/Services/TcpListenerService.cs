using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Managers;
using GameMaster.Models;
using Serilog;
using Shared;
using Shared.Clients;
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

        protected abstract Task OnMessageAsync(TcpClient socket, object message, 
            CancellationToken cancellationToken);

        protected virtual void OnConnected(TcpClient socket)
        {
            bool result = manager.AddSocket(socket);
            if (!result)
            {
                logger.Error($"Failed to add socket: {socket.Client.RemoteEndPoint}");
            }
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
            try
            {
                listener.Start();
            }
            catch (SocketException e)
            {
                logger.Error($"Error starting listener on: {ip}:{conf.CsPort}, Exception:\n {e}");
                return;
            }

            while (!cancellationToken.IsCancellationRequested && AcceptConnection())
            {
                if (listener.Pending())
                {
                    var client = await listener.AcceptTcpClientAsync();
                    OnConnected(client);

                    // TODO - improve handling messages
                    HandleMessages(new TcpSocketClient<PlayerMessage, GMMessage>(client), cancellationToken).
                        ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(50);
                }
            }
        }

        private async Task HandleMessages(ISocketClient<PlayerMessage, GMMessage> client, 
            CancellationToken cancellationToken)
        {
            logger.Information($"Started handling messages for {client.ConnectionUri}");
            var socket = (TcpClient)client.GetSocket();
            while (!cancellationToken.IsCancellationRequested && client.IsOpen)
            {
                try
                {
                    (bool messageReceived, PlayerMessage message) = await client.ReceiveAsync(cancellationToken);
                    if (messageReceived)
                    {
                        await OnMessageAsync(socket, message, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Error reading message: {e}");
                    break;
                }
            }
            logger.Information($"Finished handling messages for {client.ConnectionUri}");
        }
    }
}
