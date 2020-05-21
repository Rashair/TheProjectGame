using System;
using System.Collections.Generic;
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
    public class GMTcpSocketService : TcpSocketService<Message, Message>
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

        public override async Task OnMessageAsync(TcpSocketClient<Message, Message> client, Message message,
            CancellationToken cancellationToken)
        {
            await queue.SendAsync(message, cancellationToken);
        }

        public override void OnConnect(TcpSocketClient<Message, Message> client)
        {
        }

        public override async Task OnDisconnectAsync(TcpSocketClient<Message, Message> client,
            CancellationToken cancellationToken)
        {
            await client.CloseAsync(cancellationToken);
            lifetime.StopApplication();
        }

        public override Task OnExceptionAsync(TcpSocketClient<Message, Message> client, Exception e,
            CancellationToken cancellationToken)
        {
            logger.Warning($"IsOpen: {client.IsOpen}");
            return Task.CompletedTask;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.Information("Start waiting for gm");

            // Block another services untill GM connects, start sync section
            TcpSocketClient<Message, Message> gmClient = await ConnectGM(conf.ListenerIP, conf.GMPort,
                stoppingToken);
            if (gmClient == null)
            {
                return;
            }

            // Singleton initialization
            container.GameStarted = false;
            container.ConfirmedAgents = new Dictionary<int, bool>();
            container.GMClient = gmClient;
            logger.Information("GM connected");

            // GM connected, ServiceShareContainer initiated, release another services and start async section.
            sync.SemaphoreSlim.Release(3);
            await Task.Yield();
            await ClientHandler(gmClient, stoppingToken);
        }

        private async Task<TcpSocketClient<Message, Message>> ConnectGM(string ip, int port,
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

                        return new TcpSocketClient<Message, Message>(client, log);
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
