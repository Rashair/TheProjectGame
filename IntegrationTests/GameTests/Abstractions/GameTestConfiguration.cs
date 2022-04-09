namespace IntegrationTests.GameTests.Abstractions;

public class GameTestConfiguration
{
    public int CheckInterval { get; set; }

    public int PositionNotChangedThreshold { get; set; }

    public int NoNewPiecesThreshold { get; set; }

    public int MinimumRunTimeSec { get; set; }
}
