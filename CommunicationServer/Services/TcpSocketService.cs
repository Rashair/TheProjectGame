﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.Clients;

namespace CommunicationServer.Services
{
    public abstract class TcpSocketService<R, S> : BackgroundService
    {
        protected const int Wait = 50;

        protected readonly ILogger logger;

        public abstract Task OnMessageAsync(TcpSocketClient<R, S> client, R message,
            CancellationToken cancellationToken);

        public abstract void OnConnect(TcpSocketClient<R, S> client);

        public abstract Task OnDisconnectAsync(TcpSocketClient<R, S> client, CancellationToken cancellationToken);

        public abstract Task OnExceptionAsync(TcpSocketClient<R, S> client, Exception e,
            CancellationToken cancellationToken);

        public TcpSocketService(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task ClientHandler(TcpSocketClient<R, S> client, CancellationToken cancellationToken)
        {
            OnConnect(client);
            await ClientLoopAsync(client, cancellationToken);
            await OnDisconnectAsync(client, cancellationToken);
        }

        public async Task ClientLoopAsync(TcpSocketClient<R, S> client, CancellationToken cancellationToken)
        {
            var socket = (TcpClient)client.GetSocket();
            logger.Information($"Started handling messages for {socket.Client.RemoteEndPoint}");
            try
            {
                (bool result, R message) = await client.ReceiveAsync(cancellationToken);
                while (!cancellationToken.IsCancellationRequested && result)
                {
                    await OnMessageAsync(client, message, cancellationToken);
                    (result, message) = await client.ReceiveAsync(cancellationToken);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Error reading message: {e}");
                await OnExceptionAsync(client, e, cancellationToken);
            }

            logger.Information($"Finished handling messages for {socket.Client.RemoteEndPoint}");
        }

        public TcpListener StartListener(string ip, int port)
        {
            IPAddress address = IPAddress.Parse(ip);
            try
            {
                TcpListener listener = new TcpListener(address, port);
                listener.Start();
                return listener;
            }
            catch (SocketException e)
            {
                logger.Error($"Error starting listener on: {ip}:{port}, Exception:\n {e}");
                throw;
            }
        }
    }
}