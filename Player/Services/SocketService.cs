using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.Hosting;
using Player.Clients;
using Player.Models;
using Serilog;
using Shared;
using Shared.Messages;

namespace Player.Services
{
    public class SocketService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly ISocketClient<GMMessage, PlayerMessage> client;
        private readonly PlayerConfiguration conf;
        private readonly BufferBlock<GMMessage> queue;
        private readonly IApplicationLifetime lifetime;

        public Uri ConnectUri => new Uri($"http://{conf.CsIP}:{conf.CsPort}");

        public SocketService(ISocketClient<GMMessage, PlayerMessage> client, PlayerConfiguration conf,
            BufferBlock<GMMessage> queue, IApplicationLifetime lifetime)
        {
            this.logger = Log.ForContext<SocketService>();
            this.client = client;
            this.conf = conf;
            this.queue = queue;
            this.lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                var (success, errorMessage) = await Helpers.Retry(async () =>
                {
                    await Task.Yield();
                    await client.ConnectAsync(ConnectUri, stoppingToken);
                }, 60, 1000, stoppingToken);
                if (!success)
                {
                    logger.Error($"No connection could be made. Error: {errorMessage}");
                    lifetime.StopApplication();
                    return;
                }

                (bool receivedMessage, GMMessage message) = await client.ReceiveAsync(stoppingToken);
                while (!stoppingToken.IsCancellationRequested && client.IsOpen)
                {
                    if (receivedMessage)
                    {
                        logger.Information($"Sending message to queue: {message}");
                        bool sended = await queue.SendAsync(message, stoppingToken);
                        if (!sended)
                        {
                            logger.Warning($"SocketService| GMMessage id: {message.Id} has been lost");
                        }
                    }
                    else
                    {
                        await Task.Delay(1000);
                        logger.Information("Waiting");
                    }
                    (receivedMessage, message) = await client.ReceiveAsync(stoppingToken);
                }
            }
        }
    }
}
