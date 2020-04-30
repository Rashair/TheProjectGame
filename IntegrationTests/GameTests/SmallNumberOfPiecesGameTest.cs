﻿using GameMaster.Models;
using IntegrationTests.GameTests.Abstractions;
using Xunit;

namespace IntegrationTests.GameTests
{
    public class SmallNumberOfPiecesGameTest : GameTest
    {
        public SmallNumberOfPiecesGameTest()
        {
            Conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 5005,
                MovePenalty = 200,
                AskPenalty = 10000,
                PutPenalty = 500,
                CheckPenalty = 750,
                DestroyPenalty = 100,
                PickPenalty = 100,
                DiscoverPenalty = 3000,
                ResponsePenalty = 5000,
                Height = 12,
                Width = 6,
                GoalAreaHeight = 4,
                NumberOfGoals = 5,
                NumberOfPiecesOnBoard = 2,
                NumberOfPlayersPerTeam = 6,
                ShamPieceProbability = 0.3f,
            };

            TestConf.NoNewPiecesThreshold = 2;
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
