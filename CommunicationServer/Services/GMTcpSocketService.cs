using System;
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
        private readonly IApplicationLifetime lifetime;

        public GMTcpSocketService(BufferBlock<Message> queue, ServiceShareContainer container,
            IApplicationLifetime lifetime, ILogger logger)
            : base(logger)
        {
            this.queue = queue;
            this.container = container;
            this.lifetime = lifetime;
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
            await Task.CompletedTask;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TODO From config
            // Block another services untill GM connects, start sync section
            TcpSocketClient<GMMessage, PlayerMessage> gmClient = ConnectGM("127.0.0.1", 3000, stoppingToken);
            if (gmClient is null)
            {
                return;
            }

            // Singleton initialization
            container.GMClient = gmClient;

            // GM connected, ServiceShareContainer initiated, release another services and start asycn section.
            await Task.Yield();
            await ClientHandler(gmClient, true, stoppingToken);
        }

        private TcpSocketClient<GMMessage, PlayerMessage> ConnectGM(string ip, int port,
            CancellationToken cancellationToken)
        {
            TcpListener gmListener = StartListener(ip, port);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (gmListener.Pending())
                {
                    try
                    {
                        Task<TcpClient> acceptTaks = gmListener.AcceptTcpClientAsync();
                        acceptTaks.Wait(cancellationToken);
                        TcpClient gm = acceptTaks.Result;
                        return new TcpSocketClient<GMMessage, PlayerMessage>(gm, logger);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.Error("cancellationToken was canceled during GM connection.");
                        throw;
                    }
                }
                Thread.Sleep(Wait);
            }
            return null;
        }
    }
}
