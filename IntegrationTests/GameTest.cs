﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums;
using TestsShared;
using Xunit;

namespace IntegrationTests
{
    public abstract class GameTest : IDisposable
    {
        protected readonly CancellationTokenSource tokenSource;

        protected GameConfiguration conf;
        protected int positionsCheckTime = 5000;
        protected int positionNotChangedCount = 3;

        protected IWebHost gmHost;
        protected IWebHost[] redPlayersHosts;
        protected IWebHost[] bluePlayersHosts;

        public GameTest()
        {
            tokenSource = new CancellationTokenSource();
        }

        public HttpClient Client { get; set; }

        protected async Task StartGame()
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

            await Task.Delay(1000);

            var playerRed = redPlayersHosts[0].Services.GetService<Player.Models.Player>();
            Assert.True(playerRed.Team == Team.Red, "Player should have team passed with conf");
            Assert.True(playerRed.Position.y >= 0, "Player should have position set.");
            Assert.True(playerRed.Position.y < conf.Height - conf.GoalAreaHeight, "Player should not be present on enemy team field");

            var playerBlue = bluePlayersHosts[0].Services.GetService<Player.Models.Player>();
            Assert.True(playerBlue.Team == Team.Blue, "Player should have team passed with conf");
            Assert.True(playerBlue.Position.y >= 0, "Player should have position set.");
            Assert.True(playerBlue.Position.y >= conf.GoalAreaHeight, "Player should not be present on enemy team field");
        }

        // TODO: Add strategy here, when we will have second strategy :) 
        protected string[] CreatePlayerConfig(Team team)
        {
            return new[] { $"TeamID={team.ToString().ToLower()}", "urls=http://127.0.0.1:0", $"CsIP={conf.CsIP}", $"CsPort={conf.CsPort}" };
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
                await Task.Delay(positionsCheckTime);
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
        }

        public abstract void RunGameWithConfiguration();

        private void AssertPositionsChange(List<Player.Models.Player> team, List<(int y, int x)> teamPositions, int[] positionsCounter)
        {
            for (int i = 0; i < team.Count; ++i)
            {
                if (team[i].Position == teamPositions[i])
                {
                    ++positionsCounter[i];
                    Assert.False(positionsCounter[i] > positionNotChangedCount, "Player should not be stuck on one position");
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
