using GameMaster.Models;
using IntegrationTests.GameTests.Abstractions;
using Xunit;

namespace IntegrationTests.GameTests
{
    public class SimpleGameTest : GameTest
    {
        public SimpleGameTest()
        {
            Conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 5001,
                MovePenalty = 200,
                AskPenalty = 10000,
                PutPenalty = 500,
                CheckForShamPenalty = 750,
                DestroyPenalty = 100,
                PickupPenalty = 100,
                DiscoverPenalty = 1500,
                ResponsePenalty = 5000,
                PrematureRequestPenalty = 1000,
                Height = 12,
                Width = 4,
                GoalAreaHeight = 4,
                NumberOfGoals = 4,
                NumberOfPiecesOnBoard = 6,
                NumberOfPlayersPerTeam = 4,
                ShamPieceProbability = 0.3f,
            };

            TestConf.CheckInterval = 4000;
        }

        [Fact(Timeout = 8 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
