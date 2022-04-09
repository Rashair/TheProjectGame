using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using CommunicationServer.Models;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared;
using Shared.Clients;
using Shared.Enums;
using Shared.Managers;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads.GMPayloads;

namespace CommunicationServer.Services;

public class CommunicationService : BackgroundService
{
    private readonly ISocketManager<ISocketClient<Message, Message>, Message> manager;
    private readonly ServiceShareContainer container;
    private readonly BufferBlock<Message> queue;
    private readonly ILogger logger;
    private readonly ServiceSynchronization sync;
    private ISocketClient<Message, Message> gmClient;

    public CommunicationService(ISocketManager<ISocketClient<Message, Message>, Message> manager,
        ServiceShareContainer container, BufferBlock<Message> queue, ILogger log, ServiceSynchronization sync)
    {
        this.manager = manager;
        this.queue = queue;
        this.container = container;
        this.logger = log.ForContext<CommunicationService>();
        this.sync = sync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await sync.SemaphoreSlim.WaitAsync();
        logger.Information("Started CommunicationService");
        gmClient = container.GMClient;

        while (!stoppingToken.IsCancellationRequested)
        {
            Message message = await queue.ReceiveAsync(stoppingToken);
            if (message == null)
            {
                logger.Information("Stopping CommunicationService");
                return;
            }

            if (message.IsMessageToGM())
            {
                await gmClient.SendAsync(message, stoppingToken);
                logger.Verbose(MessageLogger.Received(message) + ". Sent message to GM");
            }
            else
            {
                await manager.SendMessageAsync(message.AgentID.Value, message, stoppingToken);
                logger.Verbose(MessageLogger.Received(message) + ". Sent message to Player");
                await sync.SemaphoreSlim.WaitAsync();
                if (!container.GameStarted)
                {
                    if (message.MessageID == MessageID.JoinTheGameAnswer)
                    {
                        JoinAnswerPayload payload = (JoinAnswerPayload)message.Payload;
                        if (payload.Accepted)
                        {
                            ConfirmSocket(message);
                        }
                    }
                    else if (message.MessageID == MessageID.StartGame)
                    {
                        await CloseUnconfirmedSockets(stoppingToken);
                        container.GameStarted = true;
                    }
                }
                sync.SemaphoreSlim.Release(1);
            }
        }
    }

    private void ConfirmSocket(Message message)
    {
        if (message.AgentID != null && container.ConfirmedAgents.ContainsKey(message.AgentID.Value))
        {
            container.ConfirmedAgents[message.AgentID.Value] = true;
        }
    }

    private async Task CloseUnconfirmedSockets(CancellationToken stoppingToken)
    {
        foreach (var elem in container.ConfirmedAgents)
        {
            if (!elem.Value)
            {
                bool result = await manager.RemoveSocketAsync(elem.Key, stoppingToken);
                if (!result)
                {
                    ISocketClient<Message, Message> socket = manager.GetSocketById(elem.Key);
                    logger.Error($"Failed to remove socket: {socket.GetSocket().Endpoint}");
                }
                logger.Information($"Player {elem.Key} has been forced to disconnect - connection after StartGame");
            }
        }
    }
}
