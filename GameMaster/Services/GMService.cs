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

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            base.StartAsync(cancellationToken);

            gameMaster.StartGame();

            return Task.CompletedTask;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TryToAcceptMessage();
                Thread.Sleep(50);
            }

            return Task.CompletedTask;
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

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            base.StopAsync(cancellationToken);

            gameMaster.EndGame();

            return Task.CompletedTask;
        }
    }
}
