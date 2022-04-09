using Player.Models.Strategies.Utils;
using Shared.Enums;

namespace IntegrationTests.GameTests.Abstractions;

internal class ConfigurationGenerator
{
    private readonly string csIP;
    private readonly int csPort;

    public ConfigurationGenerator(string csIP, int csPort)
    {
        this.csIP = csIP;
        this.csPort = csPort;
    }

    public (string[] csArgs, string[] gmArgs, string[] redArgs, string[] blueArgs) GenerateAll(
    string gmUrl)
    {
        int playerPort = csPort + 1000;
        var csArgs = new string[]
        {
            "urls=http://127.0.0.1:0",
            $"GMPort={csPort}",
            $"PlayerPort={playerPort}",
            $"ListenerIP={csIP}",
            "Verbose=false"
        };
        var gmArgs = new string[]
        {
            $"urls={gmUrl}",
            "Verbose=false"
        };

        var redArgs = CreatePlayerConfig(Team.Red, playerPort, StrategyEnum.AdvancedStrategy);
        var blueArgs = CreatePlayerConfig(Team.Blue, playerPort, StrategyEnum.AdvancedStrategy);

        return (csArgs, gmArgs, redArgs, blueArgs);
    }

    private string[] CreatePlayerConfig(Team team, int port, StrategyEnum strategy)
    {
        return new[]
        {
            $"TeamID={team.ToString().ToLower()}",
            "urls=http://127.0.0.1:0",
            $"CsIP={csIP}",
            $"CsPort={port}",
            $"Strategy={(int)strategy}",
            "Verbose=false"
        };
    }
}
