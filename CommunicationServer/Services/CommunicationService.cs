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

namespace CommunicationServer.Services
{
    public class CommunicationService : BackgroundService
    {
        private readonly ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage> manager;
        private readonly ServiceShareContainer container;
        private readonly BufferBlock<Message> queue;
        private readonly ILogger logger;
        private readonly ServiceSynchronization sync;
        private ISocketClient<GMMessage, PlayerMessage> gmClient;

        public CommunicationService(ISocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage> manager,
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
                switch (message)
                {
                    case GMMessage gm:
                        await manager.SendMessageAsync(gm.AgentID, gm, stoppingToken);
                        if (gm.MessageID == GMMessageId.StartGame) container.CanConnect = false;
                        logger.Verbose(MessageLogger.Received(gm) + ". Sent message to Player");
                        break;

                    case PlayerMessage pm:
                        await gmClient.SendAsync(pm, stoppingToken);
                        logger.Verbose(MessageLogger.Received(pm) + ". Sent message to GM");
                        break;

                    case null when stoppingToken.IsCancellationRequested:
                        logger.Information("Stopping CommunicationService");
                        return;

                    default:
                        logger.Warning("Unknown message type. Exiting service");
                        return;
                }
            }
        }
    }
}
