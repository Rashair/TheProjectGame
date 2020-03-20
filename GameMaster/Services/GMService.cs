using GameMaster.Models;
using Microsoft.Extensions.Hosting;
using System;
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await gameMaster.AcceptMessage(stoppingToken);
        }
    }
}
