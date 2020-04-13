using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.Hosting;
using Player.Models;
using Serilog;
using Shared;
using Shared.Clients;
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
        private readonly Uri connectUri;

        public SocketService(ISocketClient<GMMessage, PlayerMessage> client, PlayerConfiguration conf,
            BufferBlock<GMMessage> queue, IApplicationLifetime lifetime)
        {
            this.logger = Log.ForContext<SocketService>();
            this.client = client;
            this.conf = conf;
            this.queue = queue;
            this.lifetime = lifetime;
            this.connectUri = new Uri($"http://{conf.CsIP}:{conf.CsPort}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var (success, errorMessage) = await Helpers.Retry(async () =>
            {
                await client.ConnectAsync(connectUri, stoppingToken);
                return true;
            }, 60, 1000, stoppingToken);
            if (!success)
            {
                logger.Error($"No connection could be made. Error: {errorMessage}");
                lifetime.StopApplication();
                return;
            }

            // Increment semafor initial count to 1
            SynchronizationContext.SemaphoreSlim.Release();

            // Wait for player service to pick up semaphore
            while (SynchronizationContext.SemaphoreSlim.CurrentCount > 0 && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(200);
            }

            // Block until joinTheGame is sent
            await SynchronizationContext.SemaphoreSlim.WaitAsync(stoppingToken);

            // Wait for JoinTheGame response for 30 seconds
            (bool receivedMessage, GMMessage message) = (false, null);
            (success, errorMessage) = await Helpers.Retry(async () =>
            {
                (receivedMessage, message) = await client.ReceiveAsync(stoppingToken);
                return receivedMessage;
            }, 60, 500, stoppingToken);
            if (!success)
            {
                logger.Error($"Did not receive JoinTheGame response. Error: {errorMessage}");
                lifetime.StopApplication();
                return;
            }

            while (!stoppingToken.IsCancellationRequested && client.IsOpen)
            {
                if (receivedMessage)
                {
                    bool sended = await queue.SendAsync(message, stoppingToken);
                    if (!sended)
                    {
                        logger.Warning($"SocketService| GMMessage id: {message.Id} has been lost");
                    }
                }
                (receivedMessage, message) = await client.ReceiveAsync(stoppingToken);
            }
        }
    }
}
