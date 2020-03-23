using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Player.Clients;
using Shared.Models.Messages;

namespace Player.Services
{
    public class SocketService : BackgroundService
    {
        private readonly ISocketClient<GMMessage, PlayerMessage> client;
        private readonly IConfiguration conf;
        // TODO switch to our logger, when ready
        private readonly ILogger logger;
        private readonly BufferBlock<GMMessage> queue;

        // TODO from player config, when ready
        public Uri ConnectUri => new Uri("ws://localhost:8111/palyer");

        public SocketService(ISocketClient<GMMessage, PlayerMessage> client, IConfiguration conf,
            ILogger<SocketService> logger, BufferBlock<GMMessage> queue)
        {
            this.client = client;
            this.conf = conf;
            this.logger = logger;
            this.queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // TODO add reconnect policy in the future
            if (!stoppingToken.IsCancellationRequested)
            {
                await client.ConnectAsync(ConnectUri, stoppingToken);
                await Task.Yield();
                (bool result, GMMessage message) = await client.ReceiveAsync(stoppingToken);
                while (!stoppingToken.IsCancellationRequested && result)
                {
                    bool sended = await queue.SendAsync(message, stoppingToken);
                    if (!sended)
                        logger.LogWarning($"SocketService| GMMessage id: {message.id} has been lost");
                    (result, message) = await client.ReceiveAsync(stoppingToken);
                }
            }
        }
    }
}
