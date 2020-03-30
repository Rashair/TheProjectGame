using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Serilog;

namespace Player.Services
{
    public class PlayerService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IApplicationLifetime lifetime;
        private readonly Models.Player player;

        public PlayerService(Models.Player player, IApplicationLifetime lifetime)
        {
            this.player = player;
            this.logger = Log.ForContext<PlayerService>();
            this.lifetime = lifetime;
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

                lifetime.StopApplication();
            }
        }
    }
}
