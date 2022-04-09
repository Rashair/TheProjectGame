using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Player.Models;
using Player.Models.Strategies;
using Player.Services;
using Serilog;
using Shared;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads.GMPayloads;
using TestsShared;
using Xunit;

namespace Player.Tests;

public class PlayerServiceTests
{
    private readonly ILogger logger = MockGenerator.Get<ILogger>();

    [Fact(Timeout = 2500)]
    [Obsolete]
    public async Task TestExecuteAsyncShouldReadMessages()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(logger);

        var clientMock = MockGenerator.Get<ISocketClient<Message, Message>>();
        services.AddSingleton(clientMock);
        var queue = new BufferBlock<Message>();
        StartGamePayload payloadStart = new StartGamePayload
        {
            AgentID = 1,
            AlliesIDs = new int[2] { 1, 2 },
            LeaderID = 1,
            EnemiesIDs = new int[2] { 3, 4 },
            TeamID = Team.Red,
            BoardSize = new BoardSize { X = 3, Y = 3 },
            GoalAreaSize = 1,
            NumberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 },
            NumberOfPieces = 2,
            NumberOfGoals = 2,
            Penalties = new Penalties(),
            ShamPieceProbability = 0.5f,
            Position = new Position { X = 1, Y = 1 },
        };
        Message messageStart = new Message()
        {
            MessageID = MessageID.StartGame,
            Payload = payloadStart,
        };
        queue.Post(messageStart);
        services.AddSingleton(queue);
        services.AddSingleton<PlayerConfiguration>();
        services.AddSingleton(MockGenerator.Get<IStrategy>());
        services.AddSingleton<Models.Player>();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        var context = new ServiceSynchronization(1, 1);
        services.AddSingleton(context);

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
}
