using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using CommunicationServer.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Shared.Enums;
using TestsShared;
using Xunit;

namespace IntegrationTests
{
    public class StartTests : IDisposable
    {
        private readonly List<IWebHost> hosts = new List<IWebHost>(15);

        public StartTests()
        {
            var gameConfigPath = "gameConfig.json";
            if (File.Exists(gameConfigPath))
            {
                File.Delete(gameConfigPath);
            }
        }

        [Fact(Timeout = 60 * 1000)]
        public async void PlayersStart()
        {
            // Arrange
            int playersCount = 10;
            var source = new CancellationTokenSource();
            string[] argsRed = new string[] { "TeamId=red", "urls=https://127.0.0.1:0", "CsPort=1" };
            string[] argsBlue = new string[] { "TeamId=blue", "urls=https://127.0.0.1:0", "CsPort=1" };
            IWebHost[] webHostsRed = new IWebHost[playersCount];
            IWebHost[] webHostsBlue = new IWebHost[playersCount];
            for (int i = 0; i < playersCount; ++i)
            {
                webHostsRed[i] = Utilities.CreateHostBuilder(typeof(Player.Startup), argsRed).
                                     ConfigureServices(serv => serv.AddSingleton(MockGenerator.Get<ILogger>())).
                                     Build();
                webHostsBlue[i] = Utilities.CreateHostBuilder(typeof(Player.Startup), argsBlue).
                                    ConfigureServices(serv => serv.AddSingleton(MockGenerator.Get<ILogger>())).
                                    Build();

                hosts.Add(webHostsBlue[i]);
                hosts.Add(webHostsRed[i]);
            }

            // Act
            for (int i = 0; i < playersCount; ++i)
            {
                await webHostsRed[i].StartAsync(source.Token);
                await webHostsBlue[i].StartAsync(source.Token);
            }
            await Task.Delay(500);
            source.Cancel();

            // Assert
            var playerRed = webHostsRed.Last().Services.GetService<Player.Models.Player>();
            Assert.False(playerRed == null, "Player should not be null");
            Assert.True(playerRed.Team == Team.Red, "Player should get color provided via args");

            var playerBlue = webHostsBlue.Last().Services.GetService<Player.Models.Player>();
            Assert.False(playerBlue == null, "Player should not be null");
            Assert.True(playerBlue.Team == Team.Blue, "Player should get color provided via args");
        }

        [Fact(Timeout = 2 * 1000)]
        public async void GameMasterStarts()
        {
            // Arrange
            var source = new CancellationTokenSource();
            string url = "http://127.0.0.1:5000";
            string[] args = new string[] { $"urls={url}", "CsPort=1" };
            var webhost = Utilities.CreateHostBuilder(typeof(GameMaster.Startup), args).
                ConfigureServices(serv => serv.AddSingleton(MockGenerator.Get<ILogger>())).
                Build();
            hosts.Add(webhost);

            // Act
            await webhost.StartAsync(source.Token);
            HttpResponseMessage response;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"{url}");
                response = await client.PostAsync("api/InitGame", null);
            }
            await Task.Delay(500);
            source.Cancel();

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            var gameMaster = webhost.Services.GetService<GameMaster.Models.GM>();
            Assert.False(gameMaster == null, "GameMaster should not be null");
            Assert.True(gameMaster.WasGameInitialized, "Game should be initialized");
        }

        [Fact(Timeout = 30 * 1000)]
        public async void CommunicationServerStarts()
        {
            // Arrange
            var source = new CancellationTokenSource();
            string[] csArgs = new string[] { $"urls=http://127.0.0.1:1025", "PlayerPort=2" };
            var webhost = Utilities.CreateHostBuilder(typeof(CommunicationServer.Startup), csArgs).
                ConfigureServices(serv => serv.AddSingleton(MockGenerator.Get<ILogger>())).
                Build();
            string gmUrl = "http://127.0.0.1:4000";
            string[] gmArgs = new string[] { $"urls={gmUrl}" };
            var gmHost = Utilities.CreateHostBuilder(typeof(GameMaster.Startup), gmArgs).
                ConfigureServices(serv => serv.AddSingleton(MockGenerator.Get<ILogger>())).
                Build();
            hosts.Add(webhost);
            hosts.Add(gmHost);

            // Act
            await webhost.StartAsync(source.Token);
            await gmHost.StartAsync(source.Token);
            HttpResponseMessage response;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"{gmUrl}");
                response = await client.PostAsync("api/InitGame", null);
            }
            await Task.Delay(4000);
            source.Cancel();

            // Assert
            var container = webhost.Services.GetService<ServiceShareContainer>();
            Assert.True(container.GMClient != null, "GMClient in container should not be null");
        }

        public void Dispose()
        {
            for (int i = 0; i < hosts.Count; ++i)
            {
                hosts[i].Dispose();
            }
        }
    }
}
