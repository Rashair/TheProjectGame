using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace Player.Services
{
    public class PlayerService : BackgroundService
    {
        private readonly Models.Player player;

        public PlayerService(Models.Player player)
        {
            this.player = player;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Run(() => player.Work(cancellationToken));
            }
        }
    }
}
