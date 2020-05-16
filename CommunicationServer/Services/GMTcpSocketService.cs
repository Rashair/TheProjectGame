﻿using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using CommunicationServer.Models;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.Clients;
using Shared.Messages;

namespace CommunicationServer.Services
{
    public class GMTcpSocketService : TcpSocketService<GMMessage, PlayerMessage>
    {
        private readonly BufferBlock<Message> queue;
        private readonly ServiceShareContainer container;
        private readonly ServerConfigurations conf;
        private readonly IApplicationLifetime lifetime;
        private readonly Shared.ServiceSynchronization sync;
        protected readonly ILogger log;

        public GMTcpSocketService(BufferBlock<Message> queue, ServiceShareContainer container,
            ServerConfigurations conf, IApplicationLifetime lifetime, ILogger log, Shared.ServiceSynchronization sync)
            : base(log.ForContext<GMTcpSocketService>())
        {
            this.queue = queue;
            this.container = container;
            this.conf = conf;
            this.lifetime = lifetime;
            this.log = log;
            this.sync = sync;
        }

        public override async Task OnMessageAsync(TcpSocketClient<GMMessage, PlayerMessage> client, GMMessage message,
            CancellationToken cancellationToken)
        {
            await queue.SendAsync(message, cancellationToken);
        }

        public override void OnConnect(TcpSocketClient<GMMessage, PlayerMessage> client)
        {
        }

        public override async Task OnDisconnectAsync(TcpSocketClient<GMMessage, PlayerMessage> client,
            CancellationToken cancellationToken)
        {
            lifetime.StopApplication();
            await Task.CompletedTask;
        }

        public override async Task OnExceptionAsync(TcpSocketClient<GMMessage, PlayerMessage> client, Exception e,
            CancellationToken cancellationToken)
        {
            logger.Warning($"IsOpen: {client.IsOpen}");
            await Task.CompletedTask;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.Information("Start waiting for gm");

            // Block another services untill GM connects, start sync section
            TcpSocketClient<GMMessage, PlayerMessage> gmClient = await ConnectGM(conf.ListenerIP, conf.GMPort, 
                stoppingToken);
            if (gmClient == null)
            {
                return;
            }

            // Singleton initialization
            container.GMClient = gmClient;
            container.CanConnect = true;
            logger.Information("GM connected");

            // GM connected, ServiceShareContainer initiated, release another services and start async section.
            sync.SemaphoreSlim.Release(2);
            await Task.Yield();
            await ClientHandler(gmClient, stoppingToken);
        }

        private async Task<TcpSocketClient<GMMessage, PlayerMessage>> ConnectGM(string ip, int port,
            CancellationToken cancellationToken)
        {
            TcpListener gmListener = StartListener(ip, port);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (gmListener.Pending())
                {
                    try
                    {
                        Task<TcpClient> acceptTask = gmListener.AcceptTcpClientAsync();
                        acceptTask.Wait(cancellationToken);
                        IClient client = new TcpClientWrapper(acceptTask.Result);

                        return new TcpSocketClient<GMMessage, PlayerMessage>(client, log);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.Error("Operation was canceled during GM connection.");
                        break;
                    }
                }
                await Task.Delay(Wait, cancellationToken);
            }

            return null;
        }
    }
}
