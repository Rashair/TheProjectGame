using System;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GameMaster.Services
{
    public class GMService : WaitForInitService
    {
        public GMService(GM gameMaster)
            : base(gameMaster, Log.ForContext<GMService>())
        {
        }

        protected override async Task RunService(CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                try
                {
                    await gameMaster.Work(cancellationToken);
                }
                catch (Exception e)
                {
                    logger.Error($"{e.Message}");
                }
            });
        }
    }
}
