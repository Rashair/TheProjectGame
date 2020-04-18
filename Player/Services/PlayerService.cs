using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Serilog;

namespace Player.Services
{
    public class PlayerService : BackgroundService
    {
        private readonly Models.Player player;
        private readonly IApplicationLifetime lifetime;
        private readonly ILogger logger;
        private readonly SynchronizationContext synchronizationContext;

        public PlayerService(Models.Player player, IApplicationLifetime lifetime, 
            ILogger logger, SynchronizationContext synchronizationContext)
        {
            this.player = player;
            this.logger = logger.ForContext<PlayerService>();
            this.lifetime = lifetime;
            this.synchronizationContext = synchronizationContext;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Inital count = 0, so it will wait until socket service connects
            await synchronizationContext.SemaphoreSlim.WaitAsync(cancellationToken);

            await Task.Run(async () =>
            {
                try
                {
                    logger.Information("Player service working");
                    await player.JoinTheGame(cancellationToken);

                    // Now socketService can proceed with reading message
                    synchronizationContext.SemaphoreSlim.Release();
                    await Task.Delay(100);

                    await player.Start(cancellationToken);
                }
                catch (Exception e)
                {
                    logger.Error($"Error running service: {e}");
                }
            }, cancellationToken);

            lifetime.StopApplication();
        }
    }
}
