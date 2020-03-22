using GameMaster.Models;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Services
{
    public class GMService : BackgroundService
    {
        private readonly GM gameMaster;

        public GMService(GM gameMaster)
        {
            this.gameMaster = gameMaster;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await WaitForStart(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                await gameMaster.Work(cancellationToken);
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