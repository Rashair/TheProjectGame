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
using Shared.Models;

namespace CommunicationServer.Services
{
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
                }
            }
        }
    }
}
