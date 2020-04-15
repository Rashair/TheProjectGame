using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.Hosting;
using Player.Clients;
using Player.Models;
using Serilog;
using Shared.Messages;

namespace Player.Services
{
    public class SocketService : BackgroundService
    {
        private readonly PlayerConfiguration conf;
        private readonly ILogger logger;
        private readonly ISocketClient<GMMessage, PlayerMessage> client;
        private readonly BufferBlock<GMMessage> queue;

        public Uri ConnectUri => new Uri($"ws://{conf.CsIP}:{conf.CsPort}/player");

        public SocketService(ISocketClient<GMMessage, PlayerMessage> client, PlayerConfiguration conf,
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
                await Task.Yield();
                try
                {
                    await client.ConnectAsync(ConnectUri, stoppingToken);
                }
                catch (Exception e)
                {
                    logger.Error($"Connect error: {e.Message}");
                    return;
                }

                (bool result, GMMessage message) = await client.ReceiveAsync(stoppingToken);
                while (!stoppingToken.IsCancellationRequested && result)
                {
                    bool sended = await queue.SendAsync(message, stoppingToken);
                    if (!sended)
                    {
                        logger.Warning($"SocketService| GMMessage id: {message.Id} has been lost");
                    }
                    (result, message) = await client.ReceiveAsync(stoppingToken);
                }
            }
        }
    }
}
