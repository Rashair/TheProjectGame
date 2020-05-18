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
                CsPort = 5004,
                MovePenalty = 200,
                AskPenalty = 10000,
                PutPenalty = 400,
                CheckPenalty = 500,
                DestroyPenalty = 100,
                PickupPenalty = 100,
                DiscoverPenalty = 3000,
                ResponsePenalty = 5000,
                PrematureRequestPenalty = 1000,
                Height = 16,
                Width = 3,
                GoalAreaHeight = 3,
                NumberOfGoals = 6,
                NumberOfPiecesOnBoard = 6,
                NumberOfPlayersPerTeam = 6,
                ShamPieceProbability = 0.4f,
            };

            TestConf.CheckInterval = 4000;
            TestConf.PositionNotChangedThreshold = 6;
        }

        [Fact(Timeout = 10 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
