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

namespace IntegrationTests.GameTests.Abstractions
{
    public abstract class GameTest : IDisposable
    {
        protected readonly CancellationTokenSource tokenSource;
        protected IWebHost csHost;
        protected IWebHost gmHost;
        protected IWebHost[] redPlayersHosts;
        protected IWebHost[] bluePlayersHosts;

        protected GameConfiguration Conf { get; set; }

        protected GameTestConfiguration TestConf { get; private set; }

        public bool ShouldLogPlayers { get; }

        public HttpClient Client { get; set; }

        public GameTest()
        {
            tokenSource = new CancellationTokenSource();
            TestConf = new GameTestConfiguration
            {
                PositionsCheckInterval = 5000,
                PositionNotChangedThreshold = 4
            };

            string env = Environment.GetEnvironmentVariable("PLAYER_LOGGING");
            ShouldLogPlayers = env != null ? bool.Parse(env) : true;
        }

        public abstract void RunGameWithConfiguration();

        public void RunGame()
        {
            StartGame().Wait();

            var gm = gmHost.Services.GetService<GM>();
            var teamRed = redPlayersHosts.Select(host => host.Services.GetService<Player.Models.Player>()).ToList();
            var teamBlue = bluePlayersHosts.Select(host => host.Services.GetService<Player.Models.Player>()).ToList();
            var gameAsserter = new GameAsserter(TestConf, teamRed, teamBlue, gm);

            gameAsserter.CheckRuntime().Wait();

            gameAsserter.CheckEnd();

            tokenSource.Cancel();
        }

        protected async Task StartGame()
        {
            Assert.NotNull(Conf);

            await Task.Run(InitGame);

            var gameMaster = gmHost.Services.GetService<GM>();
            Assert.True(gameMaster.WasGameInitialized, "Game should be initialized");

            var (success, errorMessage) = await Shared.Helpers.Retry(() =>
            {
                return Task.FromResult(gameMaster.WasGameStarted);
            }, Conf.NumberOfPlayersPerTeam, 3000, tokenSource.Token);
            Assert.Equal(Conf.NumberOfPlayersPerTeam, gameMaster.Invoke<GM, int>("GetPlayersCount", Team.Red));
            Assert.Equal(Conf.NumberOfPlayersPerTeam, gameMaster.Invoke<GM, int>("GetPlayersCount", Team.Blue));
            Assert.True(success, "Game should be started");

            var playerRed = redPlayersHosts[0].Services.GetService<Player.Models.Player>();
            Assert.True(playerRed.Team == Team.Red, "Player should have team passed with conf");
            Assert.True(playerRed.Position.y >= 0, "Player should have position set.");
            Assert.True(playerRed.Position.y < Conf.Height - Conf.GoalAreaHeight, "Player should not be present on enemy team field");

            var playerBlue = bluePlayersHosts[0].Services.GetService<Player.Models.Player>();
            Assert.True(playerBlue.Team == Team.Blue, "Player should have team passed with conf");
            Assert.True(playerBlue.Position.y >= 0, "Player should have position set.");
            Assert.True(playerBlue.Position.y >= Conf.GoalAreaHeight, "Player should not be present on enemy team field");
        }

        private async Task InitGame()
        {
            string gmUrl = $"http://127.0.0.1:{Conf.CsPort + 500}";
            var confGenerator = new ConfigurationGenerator(Conf.CsIP, Conf.CsPort);
            var (csArgs, gmArgs, redArgs, blueArgs) = confGenerator.GenerateAll(gmUrl);

            csHost = Utilities.CreateHostBuilder(typeof(CommunicationServer.Startup), csArgs).Build();
            gmHost = Utilities.CreateHostBuilder(typeof(GameMaster.Startup), gmArgs).Build();
            int playersCount = Conf.NumberOfPlayersPerTeam;
            redPlayersHosts = new IWebHost[playersCount];
            bluePlayersHosts = new IWebHost[playersCount];
            for (int i = 0; i < playersCount; ++i)
            {
                var builderRed = Utilities.CreateHostBuilder(typeof(Player.Startup), redArgs);
                var builderBlue = Utilities.CreateHostBuilder(typeof(Player.Startup), blueArgs);
                if (!ShouldLogPlayers)
                {
                    builderRed.ConfigureServices(serv =>
                                   serv.AddSingleton<ILogger>(MockGenerator.Get<ILogger>()));
                    builderBlue.ConfigureServices(serv =>
                                   serv.AddSingleton<ILogger>(MockGenerator.Get<ILogger>()));
                }
                redPlayersHosts[i] = builderRed.Build();
                bluePlayersHosts[i] = builderBlue.Build();
            }

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

            Assert.Equal(System.Net.HttpStatusCode.Created, responseConf.StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseInit.StatusCode);
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
