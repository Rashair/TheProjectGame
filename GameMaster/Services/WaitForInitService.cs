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

    protected WaitForInitService(GM gameMaster, ILogger logger)
    {
        this.logger = logger;
        this.gameMaster = gameMaster;
        WaitForInitDelay = 1000;
    }

    protected abstract Task RunService(CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.Information($"Started service. Waiting for init...");
        await WaitForInit(stoppingToken);

        logger.Information("Finished waiting");

        if (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunService(stoppingToken);
            }
            catch (Exception e)
            {
                logger.Error($"Error running service: {e}");
            }
        }
    }

    private async Task WaitForInit(CancellationToken stoppingToken)
    {
        while (!(gameMaster.WasGameInitialized || stoppingToken.IsCancellationRequested))
        {
            await Task.Delay(WaitForInitDelay, stoppingToken);
        }
    }
}
