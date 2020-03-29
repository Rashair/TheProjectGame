using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Player.Clients;
using Player.Models;
using Player.Models.Strategies;
using Player.Services;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads;
using Xunit;

namespace Player.Tests
{
    public class PlayerServiceTests
    {
        [Fact(Timeout = 2500)]
        public async Task TestExecuteAsyncShouldReadMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<ISocketClient<GMMessage, PlayerMessage>, ClientMock<GMMessage, PlayerMessage>>();
            var queue = new BufferBlock<GMMessage>();
            StartGamePayload payloadStart = new StartGamePayload
            {
                PlayerID = 1,
                AlliesIDs = new int[2] { 1, 2 },
                LeaderID = 1,
                EnemiesIDs = new int[2] { 3, 4 },
                TeamId = Team.Red,
                BoardSize = new BoardSize { X = 3, Y = 3 },
                GoalAreaSize = 1,
                NumberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 },
                NumberOfPieces = 2,
                NumberOfGoals = 2,
                Penalties = new Penalties { Move = "0", CheckForSham = "0", Discovery = "0", DestroyPiece = "0", PutPiece = "0", InformationExchange = "0" },
                ShamPieceProbability = 0.5f,
                Position = new Position { X = 1, Y = 1 },
            };
            GMMessage messageStart = new GMMessage()
            {
                Id = GMMessageID.StartGame,
                Payload = payloadStart.Serialize(),
            };
            queue.Post(messageStart);
            services.AddSingleton(queue);
            services.AddSingleton<PlayerConfiguration>();
            services.AddSingleton<IStrategy, StrategyMock>();
            services.AddSingleton<Models.Player>();

            services.AddHostedService<PlayerService>();
            var serviceProvider = services.BuildServiceProvider();
            var hostedService = (PlayerService)serviceProvider.GetService<IHostedService>();

            // Act
            int delay = 500;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            Assert.Equal(0, queue.Count);
        }

        private class ClientMock<R, S> : ISocketClient<R, S>
        {
            public bool IsOpen => true;

            public Task CloseAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task<(bool, R)> ReceiveAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult((true, default(R)));
            }

            public async Task SendAsync(S message, CancellationToken cancellationToken)
            {
            }
        }

        private class StrategyMock : IStrategy
        {
            public Task MakeDecision(Models.Player player, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
