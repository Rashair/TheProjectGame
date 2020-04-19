using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Player.Models;
using Serilog;
using Shared.Enums;
using TestsShared;
using Xunit;

namespace IntegrationTests
{
    public abstract class GameTest : IDisposable
    {
        protected readonly CancellationTokenSource tokenSource;
        protected IWebHost csHost;
        protected IWebHost gmHost;
        protected IWebHost[] redPlayersHosts;
        protected IWebHost[] bluePlayersHosts;

        protected GameConfiguration Conf { get; set; }

        protected int PositionsCheckTime { get; set; }

        protected int PositionNotChangedCount { get; set; }

        public GameTest()
        {
            tokenSource = new CancellationTokenSource();
            PositionsCheckTime = 5000;
            PositionNotChangedCount = 3;
        }

        public HttpClient Client { get; set; }

        protected async Task StartGame()
        {
            Assert.NotNull(Conf);

            await Task.Run(async () =>
            {
                // Arrange
                string gmUrl = $"http://127.0.0.1:{Conf.CsPort + 500}";
                var (csArgs, gmArgs, redArgs, blueArgs) = CreateConfigurations(gmUrl);

                csHost = Utilities.CreateHostBuilder(typeof(CommunicationServer.Startup), csArgs).Build();
                gmHost = Utilities.CreateHostBuilder(typeof(GameMaster.Startup), gmArgs).Build();
                int playersCount = Conf.NumberOfPlayersPerTeam;
                redPlayersHosts = new IWebHost[playersCount];
                bluePlayersHosts = new IWebHost[playersCount];
                for (int i = 0; i < playersCount; ++i)
                {
                    redPlayersHosts[i] = Utilities.CreateHostBuilder(typeof(Player.Startup), redArgs).
                                ConfigureServices(serv => serv.AddSingleton(MockGenerator.Get<ILogger>())).
                                Build();
                    bluePlayersHosts[i] = Utilities.CreateHostBuilder(typeof(Player.Startup), blueArgs).
                                ConfigureServices(serv => serv.AddSingleton(MockGenerator.Get<ILogger>())).
                                Build();
                }

                // Act
                await csHost.StartAsync(tokenSource.Token);
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
                var responseConf = await Client.PostAsJsonAsync("api/Configuration", Conf);
                var responseInit = await Client.PostAsync("api/InitGame", null);

                // Assert
                Assert.Equal(System.Net.HttpStatusCode.Created, responseConf.StatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, responseInit.StatusCode);
            });

            await Task.Delay(5000);

            var gameMaster = gmHost.Services.GetService<GM>();
            Assert.True(gameMaster.WasGameInitialized, "Game should be initialized");

            var (success, errorMessage) = await Shared.Helpers.Retry(() =>
            {
                return Task.FromResult(gameMaster.WasGameStarted);
            }, Conf.NumberOfPlayersPerTeam, 3000, tokenSource.Token);
            Assert.True(success, "Game should be started");

            await Task.Delay(1000);

            var playerRed = redPlayersHosts[0].Services.GetService<Player.Models.Player>();
            Assert.True(playerRed.Team == Team.Red, "Player should have team passed with conf");
            Assert.True(playerRed.Position.y >= 0, "Player should have position set.");
            Assert.True(playerRed.Position.y < Conf.Height - Conf.GoalAreaHeight, "Player should not be present on enemy team field");

            var playerBlue = bluePlayersHosts[0].Services.GetService<Player.Models.Player>();
            Assert.True(playerBlue.Team == Team.Blue, "Player should have team passed with conf");
            Assert.True(playerBlue.Position.y >= 0, "Player should have position set.");
            Assert.True(playerBlue.Position.y >= Conf.GoalAreaHeight, "Player should not be present on enemy team field");
        }

        protected (string[] csArgs, string[] gmArgs, string[] redArgs, string[] blueArgs) CreateConfigurations(
            string gmUrl)
        {
            int playerPort = Conf.CsPort + 1000;
            var csArgs = new string[]
            {
                "urls=http://127.0.0.1:0",
                $"GMPort={Conf.CsPort}",
                $"PlayerPort={playerPort}",
                $"ListenerIP={Conf.CsIP}"
            };
            var gmArgs = new string[]
            {
                $"urls={gmUrl}",
            };

            var redArgs = CreatePlayerConfig(Team.Red, playerPort);
            var blueArgs = CreatePlayerConfig(Team.Blue, playerPort);

            return (csArgs, gmArgs, redArgs, blueArgs);
        }

        // TODO: Add strategy here, when we will have second strategy :) 
        private string[] CreatePlayerConfig(Team team, int port)
        {
            return new[]
            {
                $"TeamId={team.ToString().ToLower()}",
                "urls=http://127.0.0.1:0",
                $"CsIP={Conf.CsIP}",
                $"CsPort={port}"
            };
        }

        public async Task RunGame()
        {
            await StartGame();

            var teamRed = redPlayersHosts.Select(host => host.Services.GetService<Player.Models.Player>()).ToList();
            var teamRedPositions = teamRed.Select(player => player.Position).ToList();
            int[] positionsCounterRed = new int[teamRedPositions.Count];

            var teamBlue = redPlayersHosts.Select(host => host.Services.GetService<Player.Models.Player>()).ToList();
            var teamBluePositions = teamBlue.Select(player => player.Position).ToList();
            int[] positionsCounterBlue = new int[teamBluePositions.Count];

            while (teamRed[0].GetValue<Player.Models.Player, bool>("working"))
            {
                await Task.Delay(PositionsCheckTime);
                AssertPositionsChange(teamRed, teamRedPositions, positionsCounterRed);
                AssertPositionsChange(teamBlue, teamBluePositions, positionsCounterBlue);
            }

            var winner = teamRed[0].GetValue<Player.Models.Player, Team?>("winner");
            Assert.False(winner == null, "Winner should not be null");
            Assert.True(winner == teamBlue[0].GetValue<Player.Models.Player, Team?>("winner"),
                "Players should have same winner saved");

            var gm = gmHost.Services.GetService<GM>();
            var redPoints = gm.GetValue<GM, int>("redTeamPoints");
            var bluePoints = gm.GetValue<GM, int>("blueTeamPoints");
            var expectedWinner = redPoints > bluePoints ? Team.Red : Team.Blue;
            Assert.True(winner == expectedWinner, "GM and players should have same winner");

            tokenSource.Cancel();
        }

        public abstract void RunGameWithConfiguration();

        private void AssertPositionsChange(List<Player.Models.Player> team, List<(int y, int x)> teamPositions, int[] positionsCounter)
        {
            for (int i = 0; i < team.Count; ++i)
            {
                if (team[i].Position == teamPositions[i])
                {
                    ++positionsCounter[i];
                    Assert.False(positionsCounter[i] > PositionNotChangedCount, "Player should not be stuck on one position");
                }
                else
                {
                    teamPositions[i] = team[i].Position;
                    positionsCounter[i] = 0;
                }
            }
        }

        public void Dispose()
        {
            Client?.Dispose();
            csHost?.Dispose();
            gmHost?.Dispose();

            for (int i = 0; i < Conf?.NumberOfPlayersPerTeam; ++i)
            {
                redPlayersHosts?[i].Dispose();
                bluePlayersHosts?[i].Dispose();
            }
        }
    }
}
