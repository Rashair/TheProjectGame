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
                CsPort = 6003,
                MovePenalty = 300,
                AskPenalty = 1500,
                PutPenalty = 400,
                CheckForShamPenalty = 300,
                DestroyPenalty = 150,
                PickupPenalty = 150,
                DiscoveryPenalty = 1000,
                ResponsePenalty = 1500,
                PrematureRequestPenalty = 1000,
                Height = 20,
                Width = 18,
                GoalAreaHeight = 5,
                NumberOfGoals = 10,
                NumberOfPiecesOnBoard = 10,
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
