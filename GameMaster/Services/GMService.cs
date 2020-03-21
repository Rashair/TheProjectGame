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

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return gameMaster.AcceptMessage(stoppingToken);
        }
    }
}