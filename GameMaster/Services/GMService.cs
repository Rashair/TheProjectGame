using System;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Serilog;

namespace GameMaster.Services
{
    public class GMService : WaitForInitService
    {
        public GMService(GM gameMaster, ILogger log)
            : base(gameMaster, log.ForContext<GMService>())
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
