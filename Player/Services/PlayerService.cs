﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Player.Models;
using Serilog;

namespace Player.Services
{
    public class PlayerService : BackgroundService
    {
        private ILogger logger;
        private readonly Models.Player player;

        public PlayerService(Models.Player player)
        {
            this.player = player;
            logger = Log.ForContext<PlayerService>();
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        await player.Work(cancellationToken);
                    }
                    catch (Exception e)
                    {
                        logger.Information(e.Message);
                    }
                });
            }
        }
    }
}
