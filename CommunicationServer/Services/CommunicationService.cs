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
        private readonly ISocketClient<GMMessage, PlayerMessage> gmClient;
        private readonly BufferBlock<Message> queue;
        private readonly ILogger logger;

        public CommunicationService(ISocketManager<TcpSocketClient<PlayerMessage, GMMessage>, GMMessage> manager,
            ServiceShareContainer container, BufferBlock<Message> queue, ILogger logger) 
        {
            this.manager = manager;
            this.gmClient = container.GMClient;
            this.queue = queue;
            this.logger = logger.ForContext<CommunicationService>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            while (!stoppingToken.IsCancellationRequested)
            {
                Message message = await queue.ReceiveAsync(stoppingToken);
                switch (message)
                {
                    case GMMessage gm:
                        await manager.SendMessageAsync(gm.PlayerID, gm, stoppingToken);
                        break;

                    case PlayerMessage pm:
                        await gmClient.SendAsync(pm, stoppingToken);
                        break;

                    default:
                        throw new Exception("Unknown message type");
                }
            }
        }
    }
}
