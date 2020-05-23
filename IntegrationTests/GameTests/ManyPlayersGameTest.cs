using GameMaster.Models;
using IntegrationTests.GameTests.Abstractions;
using Xunit;

namespace IntegrationTests.GameTests
{
    public class ManyPlayersGameTest : GameTest
    {
        public ManyPlayersGameTest()
        {
            Conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 5003,
                MovePenalty = 200,
                AskPenalty = 1500,
                PutPenalty = 400,
                CheckForShamPenalty = 300,
                DestroyPenalty = 100,
                PickupPenalty = 100,
                DiscoveryPenalty = 1000,
                ResponsePenalty = 1500,
                PrematureRequestPenalty = 1000,
                Height = 20,
                Width = 18,
                GoalAreaHeight = 5,
                NumberOfGoals = 8,
                NumberOfPiecesOnBoard = 20,
                NumberOfPlayersPerTeam = 16,
                ShamPieceProbability = 0.2f,
            };

            TestConf.MinimumRunTimeSec = 60;
            TestConf.CheckInterval = 5000;
            TestConf.PositionNotChangedThreshold = 5;
        }

        [Fact(Timeout = 8 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
