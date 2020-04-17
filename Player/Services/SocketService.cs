﻿using System;
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
        private const int ConnectRetries = 60;
        private const int RetryIntervalMs = 1000;

        private readonly ISocketClient<GMMessage, PlayerMessage> client;
        private readonly PlayerConfiguration conf;
        private readonly BufferBlock<GMMessage> queue;
        private readonly IApplicationLifetime lifetime;
        private readonly ILogger logger;
        private readonly SynchronizationContext synchronizationContext;

        public SocketService(ISocketClient<GMMessage, PlayerMessage> client, PlayerConfiguration conf,
            BufferBlock<GMMessage> queue, IApplicationLifetime lifetime, ILogger log, 
            SynchronizationContext synchronizationContext)
        {
            this.client = client;
            this.conf = conf;
            this.queue = queue;
            this.lifetime = lifetime;
            this.logger = log.ForContext<SocketService>();
            this.synchronizationContext = synchronizationContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var (success, errorMessage) = await Helpers.Retry(async () =>
            {
                await client.ConnectAsync(conf.CsIP, conf.CsPort, stoppingToken);
                return true;
            }, ConnectRetries, RetryIntervalMs, stoppingToken);
            if (!success)
            {
                logger.Error($"No connection could be made. Error: {errorMessage}");
                lifetime.StopApplication();
                return;
            }

            // Increment semafor initial count to 1
            synchronizationContext.SemaphoreSlim.Release();

            // Wait for player service to pick up semaphore
            while (synchronizationContext.SemaphoreSlim.CurrentCount > 0 && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(200);
            }

            // Block until joinTheGame is sent
            await synchronizationContext.SemaphoreSlim.WaitAsync(stoppingToken);

            // Wait for JoinTheGame response
            (bool receivedMessage, GMMessage message) = await client.ReceiveAsync(stoppingToken);
            if (!receivedMessage)
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
