using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using CommunicationServer.Models;
using Serilog;
using Shared;
using Shared.Clients;
using Shared.Enums;
using Shared.Managers;
using Shared.Messages;
using Shared.Payloads.PlayerPayloads;

namespace CommunicationServer.Services
{
    public class PlayersTcpSocketService : TcpSocketService<PlayerMessage, GMMessage>
    {
        private readonly ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage> manager;
        private readonly BufferBlock<Message> queue;
        private readonly ServiceShareContainer container;
        private readonly ServerConfigurations conf;
        private readonly ServiceSynchronization sync;
        protected readonly ILogger log;

        public PlayersTcpSocketService(ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage> manager,
            BufferBlock<Message> queue, ServiceShareContainer container, ServerConfigurations conf, ILogger log,
            ServiceSynchronization sync)
            : base(log.ForContext<PlayersTcpSocketService>())
        {
            this.manager = manager;
            this.queue = queue;
            this.container = container;
            this.conf = conf;
            this.log = log;
            this.sync = sync;
        }

        public override async Task OnMessageAsync(TcpSocketClient<PlayerMessage, GMMessage> client,
            PlayerMessage message, CancellationToken cancellationToken)
        {
            message.AgentID = manager.GetId(client);
            await queue.SendAsync(message, cancellationToken);
        }

        public override void OnConnect(TcpSocketClient<PlayerMessage, GMMessage> client)
        {
            int id = manager.AddSocket(client);
            if (id == -1)
            {
                IClient socket = client.GetSocket();
                logger.Error($"Failed to add socket: {socket.Endpoint}");
            }
        }

        public override async Task OnDisconnectAsync(TcpSocketClient<PlayerMessage, GMMessage> client,
            CancellationToken cancellationToken)
        {
            int id = manager.GetId(client);
            bool result = await manager.RemoveSocketAsync(id, cancellationToken);
            if (!result)
            {
                IClient socket = client.GetSocket();
                logger.Error($"Failed to remove socket: {socket.Endpoint}");
            }
            logger.Information($"Player {id} disconnected");

            PlayerMessage message = new PlayerMessage()
            {
                AgentID = id,
                MessageID = PlayerMessageId.Disconnected,
                Payload = new EmptyPayload()
            };
            await container.GMClient.SendAsync(message, cancellationToken);
        }

        public override async Task OnExceptionAsync(TcpSocketClient<PlayerMessage, GMMessage> client, Exception e,
            CancellationToken cancellationToken)
        {
            logger.Warning(e, $"IsOpen: {client.IsOpen}");
            await Task.CompletedTask;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await sync.SemaphoreSlim.WaitAsync();
            logger.Information("Started PlayersTcpSocketService");
            TcpListener listener = StartListener(conf.ListenerIP, conf.PlayerPort);
            List<ConfiguredTaskAwaitable> tasks = new List<ConfiguredTaskAwaitable>();
            while (!stoppingToken.IsCancellationRequested)
            {
                if (listener.Pending())
                {
                    var acceptedClient = await listener.AcceptTcpClientAsync();
                    IClient tcpClient = new TcpClientWrapper(acceptedClient);
                    var socketClient = new TcpSocketClient<PlayerMessage, GMMessage>(tcpClient, log);
                    var handlerTask = ClientHandler(socketClient, stoppingToken).ConfigureAwait(false);
                    tasks.Add(handlerTask);
                }
                else
                {
                    await Task.Delay(Wait, stoppingToken);
                }
            }

            for (int i = 0; i < tasks.Count && !stoppingToken.IsCancellationRequested; ++i)
            {
                await tasks[i];
            }
            logger.Information("Stopping PlayersTcpSocketService");
        }
    }
}
