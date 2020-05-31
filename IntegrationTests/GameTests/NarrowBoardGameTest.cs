using GameMaster.Models;
using IntegrationTests.GameTests.Abstractions;
using Xunit;

namespace IntegrationTests.GameTests
{
    public class NarrowBoardGameTest : GameTest
    {
        public NarrowBoardGameTest()
        {
            Conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 6004,
                MovePenalty = 350,
                AskPenalty = 10000,
                PutPenalty = 400,
                CheckForShamPenalty = 500,
                DestroyPenalty = 250,
                PickupPenalty = 250,
                DiscoveryPenalty = 1500,
                ResponsePenalty = 5000,
                PrematureRequestPenalty = 1000,
                Height = 16,
                Width = 3,
                GoalAreaHeight = 3,
                NumberOfGoals = 6,
                NumberOfPiecesOnBoard = 4,
                NumberOfPlayersPerTeam = 6,
                ShamPieceProbability = 0.4f,
            };

            TestConf.CheckInterval = 4000;
            TestConf.PositionNotChangedThreshold = 6;
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
