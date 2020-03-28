using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Player.Models;

namespace Player.Services
{
    public class PlayerService : BackgroundService
    {
        private readonly Models.Player player;

        public PlayerService(Models.Player player, Configuration conf)
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
