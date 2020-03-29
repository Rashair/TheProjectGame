using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.Messages;

namespace Player.Services
{
    public class PlayerService : BackgroundService
    {
        private readonly Models.Player player;
        private readonly ILogger logger;

        public PlayerService(Models.Player player)
        {
            this.player = player;
            this.logger = Log.ForContext<PlayerService>();
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                logger.Information("Started execution");
                await Task.Run(() => player.Work(cancellationToken));
            }
        }
    }
}
