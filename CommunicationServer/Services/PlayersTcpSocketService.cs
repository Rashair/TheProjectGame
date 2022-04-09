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
using Shared.Payloads.CommunicationServerPayloads;

namespace CommunicationServer.Services;

public class PlayersTcpSocketService : TcpSocketService<Message, Message>
{
    private readonly ISocketManager<ISocketClient<Message, Message>, Message> manager;
    private readonly BufferBlock<Message> queue;
    private readonly ServiceShareContainer container;
    private readonly ServerConfiguration conf;
    private readonly ServiceSynchronization sync;
    protected readonly ILogger log;

    public PlayersTcpSocketService(ISocketManager<ISocketClient<Message, Message>, Message> manager,
        BufferBlock<Message> queue, ServiceShareContainer container, ServerConfiguration conf, ILogger log,
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

    public override async Task OnMessageAsync(TcpSocketClient<Message, Message> client,
        Message message, CancellationToken cancellationToken)
    {
        message.AgentID = manager.GetId(client);
        await queue.SendAsync(message, cancellationToken);
    }

    public override void OnConnect(TcpSocketClient<Message, Message> client)
    {
        int id = manager.AddSocket(client);
        if (id == -1)
        {
            IClient socket = client.GetSocket();
            logger.Error($"Failed to add socket: {socket.Endpoint}");
        }
        else
        {
            sync.SemaphoreSlim.Wait();
            container.ConfirmedAgents.Add(manager.GetId(client), false);
            sync.SemaphoreSlim.Release(1);
        }
    }

    public override async Task OnDisconnectAsync(TcpSocketClient<Message, Message> client,
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

        DisconnectPayload payload = new DisconnectPayload()
        {
            AgentID = id
        };
        Message message = new Message()
        {
            AgentID = -1,
            MessageID = MessageID.PlayerDisconnected,
            Payload = payload
        };
        await container.GMClient.SendAsync(message, cancellationToken);
    }

    public override async Task OnExceptionAsync(TcpSocketClient<Message, Message> client, Exception e,
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
            await sync.SemaphoreSlim.WaitAsync();
            bool canConnect = !container.GameStarted;
            sync.SemaphoreSlim.Release(1);
            if (listener.Pending() && canConnect)
            {
                var acceptedClient = await listener.AcceptTcpClientAsync();
                IClient tcpClient = new TcpClientWrapper(acceptedClient);
                var socketClient = new TcpSocketClient<Message, Message>(tcpClient, log);
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
