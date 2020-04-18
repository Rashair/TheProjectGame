using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using CommunicationServer.Models;
using Serilog;
using Shared.Clients;
using Shared.Managers;
using Shared.Messages;

namespace CommunicationServer.Services
{
    public class PlayersTcpSocketService : TcpSocketService<PlayerMessage, GMMessage>
    {
        private readonly ISocketManager<TcpSocketClient<PlayerMessage, GMMessage>, GMMessage> manager;
        private readonly BufferBlock<Message> queue;
        private readonly ServerConfigurations conf;
        protected readonly ILogger log;

        public PlayersTcpSocketService(ISocketManager<TcpSocketClient<PlayerMessage, GMMessage>, GMMessage> manager,
            BufferBlock<Message> queue, ServerConfigurations conf, ILogger log)
            : base(log.ForContext<PlayersTcpSocketService>())
        {
            this.manager = manager;
            this.queue = queue;
            this.conf = conf;
            this.log = log;
        }

        public override async Task OnMessageAsync(TcpSocketClient<PlayerMessage, GMMessage> client,
            PlayerMessage message, CancellationToken cancellationToken)
        {
            message.PlayerId = manager.GetId(client);
            await queue.SendAsync(message, cancellationToken);
        }

        public override void OnConnect(TcpSocketClient<PlayerMessage, GMMessage> client)
        {
            bool result = manager.AddSocket(client);
            if (!result)
            {
                TcpClient socket = (TcpClient)client.GetSocket();
                logger.Error($"Failed to add socket: {socket.Client.RemoteEndPoint}");
            }
        }

        public override async Task OnDisconnectAsync(TcpSocketClient<PlayerMessage, GMMessage> client,
            CancellationToken cancellationToken)
        {
            bool result = await manager.RemoveSocketAsync(manager.GetId(client), cancellationToken);
            if (!result)
            {
                TcpClient socket = (TcpClient)client.GetSocket();
                logger.Error($"Failed to remove socket: {socket.Client.RemoteEndPoint}");
            }
        }

        public override async Task OnExceptionAsync(TcpSocketClient<PlayerMessage, GMMessage> client, Exception e,
            CancellationToken cancellationToken)
        {
            logger.Warning($"IsOpen: {client.IsOpen}");
            await Task.CompletedTask;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            logger.Information("Started PlayersTcpSocketService");
            TcpListener listener = StartListener(conf.ListenerIP, conf.PlayerPort);
            List<ConfiguredTaskAwaitable> tasks = new List<ConfiguredTaskAwaitable>();
            while (!stoppingToken.IsCancellationRequested)
            {
                if (listener.Pending())
                {
                    TcpClient socket = await listener.AcceptTcpClientAsync();
                    var client = new TcpSocketClient<PlayerMessage, GMMessage>(socket, log);
                    var handlerTask = ClientHandler(client, false, stoppingToken).ConfigureAwait(false);
                    tasks.Add(handlerTask);
                }
                else
                {
                    await Task.Delay(Wait);
                }
            }
            for (int i = 0; i < tasks.Count; ++i)
            {
                await tasks[i];
            }
        }
    }
}
