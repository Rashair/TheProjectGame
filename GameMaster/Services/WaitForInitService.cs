using System;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GameMaster.Services;

public abstract class WaitForInitService : BackgroundService
{
    protected readonly ILogger logger;
    protected readonly GM gameMaster;

    public int WaitForInitDelay { get; protected set; }

    public WaitForInitService(GM gameMaster, ILogger logger)
    {
        this.logger = logger;
        this.gameMaster = gameMaster;
        WaitForInitDelay = 1000;
    }

    protected abstract Task RunService(CancellationToken cancellationToken);

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await WaitForInit(cancellationToken);

        logger.Information("Finished waiting");

        if (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunService(cancellationToken);
            }
            catch (Exception e)
            {
                logger.Error($"Error running service: {e}");
            }
        }
    }

    private async Task WaitForInit(CancellationToken cancellationToken)
    {
        while (!(gameMaster.WasGameInitialized || cancellationToken.IsCancellationRequested))
        {
            await Task.Delay(WaitForInitDelay, cancellationToken);
        }
    }
}
