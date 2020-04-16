using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Controllers;
using GameMaster.Managers;
using GameMaster.Models;
using GameMaster.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace GameMaster.Tests
{
    public class GameControllerTests
    {
        [Fact]
        public async Task TestConfigurationShouldReturnValidConfiguration()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                { "GameConfigPath", "gameConfig.json" },
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var gameConfig = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var manager = new TcpSocketManager<GMMessage>();
            var gameMaster = new GM(lifetime, gameConfig, queue, manager);
            GameController gameController = new GameController(config, gameConfig, gameMaster);

            // Act
            GameConfiguration newGameConfig = new MockGameConfiguration();
            newGameConfig.AskPenalty = 200;
            newGameConfig.Height = 10;
            newGameConfig.DestroyPenalty = 100;
            newGameConfig.PickPenalty = 100;

            var result = await gameController.Configuration(newGameConfig);
            var createdResult = (CreatedResult)result.Result;
            GameConfiguration returnedGameConfig = (GameConfiguration)createdResult.Value;

            // Assert
            Assert.Equal((int)HttpStatusCode.Created, createdResult.StatusCode);
            Assert.True(newGameConfig.Equals(returnedGameConfig), "Game config should be updated");
        }

        [Fact]
        public async Task TestConfigurationShouldReturnBadRequestCode()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                { "GameConfigPath", "gameConfig.json" },
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var gameConfig = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var manager = new TcpSocketManager<GMMessage>();
            var gameMaster = new GM(lifetime, gameConfig, queue, manager);
            GameController gameController = new GameController(config, gameConfig, gameMaster);

            // Act
            var result = await gameController.Configuration(null);
            var createdResult = (BadRequestObjectResult)result.Result;
            int expectedStatusCode = (int)HttpStatusCode.BadRequest;
            string expectedValue = "Received empty configuration";

            // Assert
            Assert.Equal(expectedStatusCode, createdResult.StatusCode);
            Assert.Equal(expectedValue, createdResult.Value);
        }

        [Fact]
        public void TestParameterlessConfigurationShouldReturnValidConfiguration()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                { "GameConfigPath", "gameConfig.json" },
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var gameConfig = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var manager = new TcpSocketManager<GMMessage>();
            var gameMaster = new GM(lifetime, gameConfig, queue, manager);
            GameController gameController = new GameController(config, gameConfig, gameMaster);

            // Act
            var result = gameController.Configuration();

            // Assert
            Assert.IsType<ActionResult<GameConfiguration>>(result);
            Assert.Equal(gameConfig.AskPenalty, result.Value.AskPenalty);
            Assert.Equal(gameConfig.CheckPenalty, result.Value.CheckPenalty);
            Assert.Equal(gameConfig.DiscoverPenalty, result.Value.DiscoverPenalty);
            Assert.Equal(gameConfig.MovePenalty, result.Value.MovePenalty);
            Assert.Equal(gameConfig.PutPenalty, result.Value.PutPenalty);
            Assert.Equal(gameConfig.ResponsePenalty, result.Value.ResponsePenalty);
            Assert.Equal(gameConfig.CsIP, result.Value.CsIP);
            Assert.Equal(gameConfig.CsPort, result.Value.CsPort);
            Assert.Equal(gameConfig.GoalAreaHeight, result.Value.GoalAreaHeight);
            Assert.Equal(gameConfig.Height, result.Value.Height);
            Assert.Equal(gameConfig.NumberOfGoals, result.Value.NumberOfGoals);
            Assert.Equal(gameConfig.NumberOfPiecesOnBoard, result.Value.NumberOfPiecesOnBoard);
            Assert.Equal(gameConfig.NumberOfPlayersPerTeam, result.Value.NumberOfPlayersPerTeam);
            Assert.Equal(gameConfig.ShamPieceProbability, result.Value.ShamPieceProbability);
        }

        [Fact]
        public void TestInitGameShouldReturnOkResult()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                { "GameConfigPath", "gameConfig.json" },
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var gameConfig = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var manager = new TcpSocketManager<GMMessage>();
            var gameMaster = new GM(lifetime, gameConfig, queue, manager);
            GameController gameController = new GameController(config, gameConfig, gameMaster);

            // Act
            var result = gameController.InitGame();

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public void TestWasGameStartedShouldReturnBool()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                { "GameConfigPath", "gameConfig.json" },
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var gameConfig = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var manager = new TcpSocketManager<GMMessage>();
            gameConfig.NumberOfPlayersPerTeam = 0;
            var gameMaster = new GM(lifetime, gameConfig, queue, manager);
            GameController gameController = new GameController(config, gameConfig, gameMaster);
            CancellationToken cancellationToken = CancellationToken.None;

            // Act
            var result = gameController.WasGameStarted();
            bool expectedResult = false;
            gameMaster.Invoke("StartGame", cancellationToken);
            gameController = new GameController(config, gameConfig, gameMaster);
            var result2 = gameController.WasGameStarted();
            bool expectedResult2 = true;

            // Assert
            Assert.IsType<ActionResult<bool>>(result);
            Assert.Equal(expectedResult, result.Value);

            Assert.IsType<ActionResult<bool>>(result2);
            Assert.Equal(expectedResult2, result2.Value);
        }
    }
}
