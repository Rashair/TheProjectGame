using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Shared.Enums;
using Xunit;

namespace IntegrationTests
{
    public class StartTests : IDisposable
    {
        private readonly List<IWebHost> hosts = new List<IWebHost>(20);
        private string gameConfigPath;

        [Fact]
        public async void PlayersStart()
        {
            // Arrange
            int playersCount = 10;
            var source = new CancellationTokenSource();
            string[] argsRed = new string[] { "TeamID=red", "urls=https://127.0.0.1:0", "CsPort=1" };
            string[] argsBlue = new string[] { "TeamID=blue", "urls=https://127.0.0.1:0", "CsPort=1" };
            IWebHost[] webHostsRed = new IWebHost[playersCount];
            IWebHost[] webHostsBlue = new IWebHost[playersCount];
            for (int i = 0; i < playersCount; ++i)
            {
                webHostsRed[i] = Player.Program.CreateWebHostBuilder(argsRed).
                                     UseSerilog((Logger)null, true).
                                     Build();
                webHostsBlue[i] = Player.Program.CreateWebHostBuilder(argsBlue).
                                    UseSerilog((Logger)null, true).
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

        [Fact(Timeout = 2000)]
        public async void GameMasterStarts()
        {
            // Arrange
            var source = new CancellationTokenSource();
            string url = "http://127.0.0.1:5000";
            string[] args = new string[] { $"urls={url}" };
            var webhost = Utilities.CreateWebHost(typeof(GameMaster.Startup), args).
                Build();
            hosts.Add(webhost);
            gameConfigPath = webhost.Services.GetService<IConfiguration>().GetValue<string>("GameConfigPath");

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

        [Fact(Timeout = 1500)]
        public async void CommunicationServerStarts()
        {
            // Arrange
            var source = new CancellationTokenSource();
            string[] args = new string[] { "urls=https://127.0.0.1:0" };
            var webhost = CommunicationServer.Program.CreateWebHostBuilder(args).
                ConfigureLogging((ILoggingBuilder logging) => logging.ClearProviders()).
                Build();
            hosts.Add(webhost);

            // Act
            await webhost.StartAsync(source.Token);
            await Task.Delay(500);
            source.Cancel();

            // Assert
            // TODO: ...
        }

        public void Dispose()
        {
            for (int i = 0; i < hosts.Count; ++i)
            {
                hosts[i].Dispose();
            }

            if (File.Exists(gameConfigPath))
            {
                File.Delete(gameConfigPath);
            }
        }
    }
}
