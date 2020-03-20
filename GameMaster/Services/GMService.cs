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
            if (gameMaster.WasGameStarted)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    TryToAcceptMessage();
                    await Task.Delay(50);
                }
            }
        }

        private void TryToAcceptMessage()
        {
            try
            {
                gameMaster.AcceptMessage();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"Error occurred executing {nameof(gameMaster.AcceptMessage)}\n" +
                    ex.Message);
            }
        }
    }
}
