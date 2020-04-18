using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using CommunicationServer.Models;
using Microsoft.Extensions.Hosting;
using Serilog;
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
        private ISocketClient<GMMessage, PlayerMessage> gmClient;

        public CommunicationService(ISocketManager<TcpSocketClient<PlayerMessage, GMMessage>, GMMessage> manager,
            ServiceShareContainer container, BufferBlock<Message> queue, ILogger log)
        {
            this.manager = manager;
            this.queue = queue;
            this.container = container;
            this.logger = log.ForContext<CommunicationService>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            logger.Information("Started CommunicationService");
            gmClient = container.GMClient;
    
            while (!stoppingToken.IsCancellationRequested)
            {
                Message message = null;
                try
                {
                    logger.Information($"Waiting for message");
                    message = await queue.ReceiveAsync(stoppingToken);
                    logger.Information($"Got message: {message}");

                    switch (message)
                    {
                        case GMMessage gm:
                            await manager.SendMessageAsync(gm.PlayerId, gm, stoppingToken);
                            break;

                        case PlayerMessage pm:
                            await gmClient.SendAsync(pm, stoppingToken);
                            break;

                        default:
                            throw new Exception("Unknown message type");
                    }
                }
                catch (Exception e)
                {
                    logger.Information(e, "Exception in receive");
                }
            }
        }
    }
}
