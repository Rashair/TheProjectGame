using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace Player.Services
{
    public class PlayerService : BackgroundService
    {
        private readonly Models.Player player;

        public int WaitForInitializeDelay { get; private set; }

        public PlayerService(Models.Player player)
        {
            this.player = player;
            WaitForInitializeDelay = 1000;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await WaitForInitialization(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Run(() => player.Work(cancellationToken));
            }
        }

        private async Task WaitForInitialization(CancellationToken cancellationToken)
        {
            while (!(player.WasInitialized || cancellationToken.IsCancellationRequested))
            {
                await Task.Delay(WaitForInitializeDelay, cancellationToken);
            }
        }
    }
}
