﻿using GameMaster.Models;
using IntegrationTests.GameTests.Abstractions;
using Xunit;

namespace IntegrationTests.GameTests;

public class SimpleGameTest : GameTest
{
    public SimpleGameTest()
    {
        Conf = new GameConfiguration
        {
            CsIP = "127.0.0.1",
            CsPort = 6001,
            MovePenalty = 350,
            AskPenalty = 10000,
            PutPenalty = 600,
            CheckForShamPenalty = 750,
            DestroyPenalty = 350,
            PickupPenalty = 350,
            DiscoveryPenalty = 5000,
            ResponsePenalty = 5000,
            PrematureRequestPenalty = 1000,
            Height = 12,
            Width = 4,
            GoalAreaHeight = 4,
            NumberOfGoals = 6,
            NumberOfPiecesOnBoard = 6,
            NumberOfPlayersPerTeam = 4,
            ShamPieceProbability = 0.3f,
        };

        TestConf.CheckInterval = 3500;
    }

    [Fact(Timeout = 5 * 60 * 1000)]
    public override async void RunGameWithConfiguration()
    {
        await RunGame();
    }
}
