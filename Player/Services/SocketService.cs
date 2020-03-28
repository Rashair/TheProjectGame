using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Player.Clients;
using Serilog;
using Shared.Messages;

namespace Player.Services
{
    public class SocketService : BackgroundService
    {
        private readonly ISocketClient<GMMessage, PlayerMessage> client;
        private readonly IConfiguration conf;
        private readonly ILogger logger;
        private readonly BufferBlock<GMMessage> queue;

        // TODO from player config, when ready
        public Uri ConnectUri => new Uri("ws://localhost:8111/palyer");

        public SocketService(ISocketClient<GMMessage, PlayerMessage> client, IConfiguration conf,
            BufferBlock<GMMessage> queue)
        {
            this.client = client;
            this.conf = conf;
            this.logger = Log.ForContext<SocketService>();
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
                        logger.Warning($"SocketService| GMMessage id: {message.Id} has been lost");
                    (result, message) = await client.ReceiveAsync(stoppingToken);
                }
            }
        }
    }
}
