using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums;
using Xunit;

namespace IntegrationTests
{
    public class GameTest : IDisposable
    {
        protected readonly GameConfiguration conf;
        protected readonly CancellationTokenSource tokenSource;

        protected IWebHost gmHost;
        protected IWebHost[] redPlayersHosts;
        protected IWebHost[] bluePlayersHosts;

        public GameTest()
        {
            this.conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 5001,
                AskPenalty = 1000,
                PutPenalty = 750,
                CheckPenalty = 400,
                MovePenalty = 300,
                DestroyPenalty = 100,
                DiscoverPenalty = 1500,
                ResponsePenalty = 600,
                Height = 12,
                Width = 6,
                GoalAreaHeight = 3,
                NumberOfGoals = 4,
                NumberOfPiecesOnBoard = 6,
                NumberOfPlayersPerTeam = 3,
                ShamPieceProbability = 20,
            };
            tokenSource = new CancellationTokenSource();
        }

        public HttpClient Client { get; set; }

        [Fact]
        public async Task StartGame()
        {
            await Task.Run(async () =>
            {
                // Arrange
                // TODO: Switch to CS
                string gmUrl = $"http://{conf.CsIP}:{conf.CsPort}";
                string[] args = new string[] { $"urls={gmUrl}" };
                gmHost = Utilities.CreateWebHost(typeof(GameMaster.Startup), args);

                string[] argsRed = CreatePlayerConfig(Team.Red);
                string[] argsBlue = CreatePlayerConfig(Team.Blue);
                int playersCount = conf.NumberOfPlayersPerTeam;
                redPlayersHosts = new IWebHost[playersCount];
                bluePlayersHosts = new IWebHost[playersCount];
                for (int i = 0; i < playersCount; ++i)
                {
                    redPlayersHosts[i] = Utilities.CreateWebHost(typeof(Player.Startup), argsRed);
                    bluePlayersHosts[i] = Utilities.CreateWebHost(typeof(Player.Startup), argsBlue);
                }

                // Act
                await gmHost.StartAsync(tokenSource.Token);
                await Task.Yield();
                for (int i = 0; i < playersCount; ++i)
                {
                    await redPlayersHosts[i].StartAsync(tokenSource.Token);
                    await bluePlayersHosts[i].StartAsync(tokenSource.Token);
                }

                Client = new HttpClient()
                {
                    BaseAddress = new Uri(gmUrl),
                };
                var responseConf = await Client.PostAsJsonAsync("api/Configuration", conf);
                var responseInit = await Client.PostAsync("api/InitGame", null);

                // Assert
                Assert.Equal(System.Net.HttpStatusCode.Created, responseConf.StatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, responseInit.StatusCode);
            });

            await Task.Delay(5000);

            var gameMaster = gmHost.Services.GetService<GM>();
            Assert.True(gameMaster.WasGameInitialized, "Game should be initialized");
            Assert.True(gameMaster.WasGameStarted, "Game should be started");

            var playerRed = redPlayersHosts[0].Services.GetService<Player.Models.Player>();
            Assert.True(playerRed.Team == Team.Red, "Player should have team passed with conf");
            Assert.True(playerRed.Position.y < conf.Height - conf.GoalAreaHeight, "Player should not be present on enemy team field");

            var playerBlue = bluePlayersHosts[0].Services.GetService<Player.Models.Player>();
            Assert.True(playerBlue.Team == Team.Blue, "Player should have team passed with conf");
            Assert.True(playerBlue.Position.y >= conf.GoalAreaHeight, "Player should not be present on enemy team field");
        }

        protected string[] CreatePlayerConfig(Team team)
        {
            return new[] { $"TeamID={team.ToString().ToLower()}", "urls=http://127.0.0.1:0", $"CsIP={conf.CsIP}", $"CsPort={conf.CsPort}" };
        }

        public void Dispose()
        {
            Client?.Dispose();
            gmHost?.Dispose();

            for (int i = 0; i < conf?.NumberOfPlayersPerTeam; ++i)
            {
                redPlayersHosts[i].Dispose();
                bluePlayersHosts[i].Dispose();
            }

            tokenSource.Cancel();
        }
    }
}
