using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums;
using TestsShared;
using Xunit;

namespace IntegrationTests
{
    public class GameTest : IDisposable
    {
        private readonly GameConfiguration conf;

        private IWebHost gmHost;
        private IWebHost[] redPlayersHosts;
        private IWebHost[] bluePlayersHosts;

        public GameTest()
        {
            conf = new GameConfiguration
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
        }

        public HttpClient Client { get; set; }

        [Fact]
        public async void Test()
        {
            // Arrange
            var tokenSource = new CancellationTokenSource();

            // TODO: Switch to CS
            string gmUrl = $"http://{conf.CsIP}:{conf.CsPort}";
            string[] args = new string[] { $"urls={gmUrl}" };
            gmHost = Utilities.CreateWebHost(typeof(GameMaster.Startup), args);

            await gmHost.StartAsync(tokenSource.Token);

            Client = new HttpClient()
            {
                BaseAddress = new Uri(gmUrl),
            };
            var responseConf = await Client.PostAsJsonAsync("api/Configuration", conf);
            var responseInit = await Client.PostAsync("api/InitGame", null);

            await Task.Yield();

            string[] argsRed = new string[] { "TeamID=red", "urls=http://127.0.0.1:0", $"CsIP={conf.CsIP}", $"CsPort={conf.CsPort}" };
            string[] argsBlue = new string[] { "TeamID=blue", "urls=http://127.0.0.1:0", $"CsIP={conf.CsIP}", $"CsPort={conf.CsPort}" };
            int playersCount = conf.NumberOfPlayersPerTeam;
            redPlayersHosts = new IWebHost[playersCount];
            bluePlayersHosts = new IWebHost[playersCount];
            for (int i = 0; i < playersCount; ++i)
            {
                redPlayersHosts[i] = Utilities.CreateWebHost(typeof(Player.Startup), argsRed);
                await redPlayersHosts[i].StartAsync(tokenSource.Token);

                bluePlayersHosts[i] = Utilities.CreateWebHost(typeof(Player.Startup), argsBlue);
                await bluePlayersHosts[i].StartAsync(tokenSource.Token);
            }

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, responseConf.StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseInit.StatusCode);

            await Task.Delay(5000);

            var gameMaster = gmHost.Services.GetService<GM>();
            Assert.True(gameMaster.WasGameInitialized, "Game should be initialized");
            Assert.True(gameMaster.WasGameStarted, "Game should be started");
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
        }
    }
}
