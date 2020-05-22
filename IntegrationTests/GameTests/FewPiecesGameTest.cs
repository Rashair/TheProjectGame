using GameMaster.Models;
using IntegrationTests.GameTests.Abstractions;
using Xunit;

namespace IntegrationTests.GameTests
{
    public class FewPiecesGameTest : GameTest
    {
        public FewPiecesGameTest()
        {
            Conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 5005,
                MovePenalty = 200,
                AskPenalty = 10000,
                PutPenalty = 500,
                CheckForShamPenalty = 750,
                DestroyPenalty = 100,
                PickupPenalty = 100,
                DiscoverPenalty = 1000,
                PrematureRequestPenalty = 1000,
                ResponsePenalty = 5000,
                Height = 12,
                Width = 5,
                GoalAreaHeight = 3,
                NumberOfGoals = 4,
                NumberOfPiecesOnBoard = 3,
                NumberOfPlayersPerTeam = 8,
                ShamPieceProbability = 0.3f,
            };

            TestConf.NoNewPiecesThreshold = 6;
            TestConf.CheckInterval = 5000;
        }

        [Fact(Timeout = 5 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
