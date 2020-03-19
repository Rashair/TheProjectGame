using GameMaster.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Services
{
    public class GMService : IHostedService
    {
        private readonly GM gameMaster;

        public GMService(GM gameMaster)
        {
            this.gameMaster = gameMaster;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            gameMaster.StartGame();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            gameMaster.EndGame();

            return Task.CompletedTask;
        }
    }
}
