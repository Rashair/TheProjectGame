using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace IntegrationTests.GameTests.Abstractions;

public abstract class GameTest : IDisposable
{
    protected readonly CancellationTokenSource tokenSource;
    protected IHost csHost;
    protected IHost gmHost;
    protected IHost[] redPlayersHosts;
    protected IHost[] bluePlayersHosts;

    protected GameConfiguration Conf { get; set; }

    protected GameTestConfiguration TestConf { get; private set; }

    public HttpClient Client { get; set; }

    public GameTest()
    {
        tokenSource = new CancellationTokenSource();
        TestConf = new GameTestConfiguration
        {
            CheckInterval = 4000,
            PositionNotChangedThreshold = 4,
            NoNewPiecesThreshold = 5,
            MinimumRunTimeSec = 30,
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
        var startTime = DateTime.Now;

        await gameAsserter.CheckRuntime();

        await Task.Delay(2500);

        gameAsserter.CheckEnd(startTime);
    }

    private async Task InitGame()
    {
        string gmUrl = $"http://127.0.0.1:{Conf.CsPort + 500}";
        var confGenerator = new ConfigurationGenerator(Conf.CsIP, Conf.CsPort);
        var (csArgs, gmArgs, redArgs, blueArgs) = confGenerator.GenerateAll(gmUrl);

        csHost = Utilities.CreateHostBuilder(typeof(CommunicationServer.Startup), csArgs).Build();
        gmHost = Utilities.CreateHostBuilder(typeof(GameMaster.Startup), gmArgs).Build();
        int playersCount = Conf.NumberOfPlayersPerTeam;
        redPlayersHosts = new IHost[playersCount];
        bluePlayersHosts = new IHost[playersCount];
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

    private async Task StartGame()
    {
        var responseConf = await Client.PostAsJsonAsync("api/Configuration", Conf);
        var responseInit = await Client.PostAsync("api/InitGame", null);

        Assert.Equal(System.Net.HttpStatusCode.Created, responseConf.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, responseInit.StatusCode);
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
