using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.Extensions.Hosting;

namespace GameMaster.Services
{
    public class GMService : BackgroundService
    {
        private readonly GM gameMaster;

        public int WaitForStartDelay { get; private set; }

        public GMService(GM gameMaster)
        {
            this.gameMaster = gameMaster;
            WaitForStartDelay = 1000;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await WaitForStart(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                // Task.Run is important - if gameMaster have nothing to do blocks thread
                await Task.Run(() => gameMaster.Work(cancellationToken));
            }
        }

        private async Task WaitForStart(CancellationToken cancellationToken)
        {
            while (!(gameMaster.WasGameStarted || cancellationToken.IsCancellationRequested))
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
