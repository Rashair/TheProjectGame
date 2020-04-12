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

            List<Task> readTasks = new List<Task>();

            while (!cancellationToken.IsCancellationRequested && AcceptConnection())
            {
                if (listener.Pending())
                {
                    var client = await listener.AcceptTcpClientAsync();
                    OnConnected(client);
                    logger.Information($"Client: {client.Client.RemoteEndPoint} connected.");

                    var stream = client.GetStream();
                    var buffer = new byte[BufferSize];
                    int readCount = await ReadMessage(stream, buffer, new byte[2], cancellationToken);
                    await OnMessageAsync(client, buffer, readCount);
                    HandleMessages(client, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        private async Task HandleMessages(TcpClient client, CancellationToken cancellationToken)
        {
            var stream = client.GetStream();
            var buffer = new byte[BufferSize];
            var lengthBuffer = new byte[2];
            logger.Information($"Started handling messages for {client.Client.RemoteEndPoint}");
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                if (stream.DataAvailable)
                {
                    try
                    {
                        int countRead = await ReadMessage(stream, buffer, lengthBuffer, cancellationToken);
                        await OnMessageAsync(client, buffer, countRead);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error reading message: {e}");
                        break;
                    }
                }
                else
                {
                    await Task.Delay(500);
                }
            }
            logger.Information($"Finished handling messages for {client.Client.RemoteEndPoint}");
        }

        private async Task<int> ReadMessage(NetworkStream stream, byte[] buffer, byte[] lengthBuffer,
            CancellationToken cancellationToken)
        {
            await stream.ReadAsync(lengthBuffer, 0, 2, cancellationToken);
            int toRead = lengthBuffer.ToInt16();
            int countRead = await stream.ReadAsync(buffer, 0, toRead, cancellationToken);
            if (countRead != toRead)
            {
                logger.Warning("Wrong message length");
            }
            return countRead;
        }
    }
}
