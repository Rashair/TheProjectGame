using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using CommunicationServer.Models;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared;
using Shared.Clients;
using Shared.Managers;
using Shared.Messages;

namespace CommunicationServer.Services
{
    public class CommunicationService : BackgroundService
    {
        private readonly ISocketManager<TcpSocketClient<PlayerMessage, GMMessage>, GMMessage> manager;
        private readonly ServiceShareContainer container;
        private readonly BufferBlock<Message> queue;
        private readonly ILogger logger;
        private readonly ServiceSynchronization sync;
        private ISocketClient<GMMessage, PlayerMessage> gmClient;

        public CommunicationService(ISocketManager<TcpSocketClient<PlayerMessage, GMMessage>, GMMessage> manager,
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
                        await manager.SendMessageAsync(gm.PlayerId, gm, stoppingToken);
                        break;

                    case PlayerMessage pm:
                        await gmClient.SendAsync(pm, stoppingToken);
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
