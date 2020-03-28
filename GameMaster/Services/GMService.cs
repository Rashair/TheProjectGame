using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GameMaster.Services
{
    public class GMService : BackgroundService
    {
        private readonly GM gameMaster;
        private readonly ILogger logger;

        public int WaitForStartDelay { get; private set; }

        public GMService(GM gameMaster)
        {
            this.logger = Log.ForContext<GMService>();
            this.gameMaster = gameMaster;
            WaitForStartDelay = 1000;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await WaitForStart(cancellationToken);

            logger.Information("Stopped waiting");
            if (!cancellationToken.IsCancellationRequested)
            {
                // Task.Run is important - if gameMaster have nothing to do blocks thread
                await Task.Run(() => gameMaster.Work(cancellationToken));
            }
        }

        private async Task WaitForStart(CancellationToken cancellationToken)
        {
            while (!(gameMaster.WasGameInitialized || cancellationToken.IsCancellationRequested))
            {
                await Task.Delay(WaitForStartDelay, cancellationToken);
            }
        }
    }
}
