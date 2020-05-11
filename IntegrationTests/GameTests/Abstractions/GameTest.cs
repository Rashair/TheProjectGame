using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
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

        public HttpClient Client { get; set; }

        public GameTest()
        {
            tokenSource = new CancellationTokenSource();
            TestConf = new GameTestConfiguration
            {
                CheckInterval = 5000,
                PositionNotChangedThreshold = 4,
                NoNewPiecesThreshold = 5,
            };
        }

        public abstract void RunGameWithConfiguration();

        protected async Task RunGame()
        {
            Assert.NotNull(Conf);

            await Task.Run(InitGame);

            var gm = gmHost.Services.GetService<GM>();
            var teamRed = redPlayersHosts.Select(host => host.Services.GetService<Player.Models.Player>()).ToList();
            var teamBlue = bluePlayersHosts.Select(host => host.Services.GetService<Player.Models.Player>()).ToList();
            var gameAsserter = new GameAsserter(TestConf, teamRed, teamBlue, gm);

            await StartGame();
            await gameAsserter.CheckStart();

            await gameAsserter.CheckRuntime();

            await Task.Delay(2500);

            gameAsserter.CheckEnd();
        }

        private async Task StartGame()
        {
            var responseConf = await Client.PostAsJsonAsync("api/Configuration", Conf);
            var responseInit = await Client.PostAsync("api/InitGame", null);

            Assert.Equal(System.Net.HttpStatusCode.Created, responseConf.StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.OK, responseInit.StatusCode);
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
        }

        public void Dispose()
        {
            tokenSource.Cancel();

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
