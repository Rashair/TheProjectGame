using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Player.Clients;
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
        [Fact(Timeout = 1500)]
        public async Task TestExecuteAsyncShouldReadMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<WebSocketClient<GMMessage, PlayerMessage>>();
            var queue = new BufferBlock<GMMessage>();
            StartGamePayload payloadStart = new StartGamePayload
            {
                PlayerID = 1,
                AlliesIDs = new int[1] { 2 },
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
            services.AddSingleton<WebSocketClient<GMMessage, PlayerMessage>>();
            services.AddSingleton<Models.Player>();
            services.AddHostedService<SocketService>();
            services.AddHostedService<PlayerService>();

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = (PlayerService)serviceProvider.GetService<IHostedService>();
            var player = serviceProvider.GetService<Models.Player>();
            var initializePlayer = GetMethod("InitializePlayer", typeof(Models.Player));
            var parametrs = new object[] { Team.Red, null, CancellationToken.None };
            initializePlayer.Invoke(player, parametrs);

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

        public MethodInfo GetMethod(string methodName, Type type)
        {
            Assert.False(string.IsNullOrWhiteSpace(methodName), $"{nameof(methodName)} cannot be null or whitespace");

            MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.False(method == null, $"Method {methodName} not found");

            return method;
        }
    }
}
