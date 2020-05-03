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
                CheckPenalty = 750,
                DestroyPenalty = 100,
                PickPenalty = 100,
                DiscoverPenalty = 3000,
                ResponsePenalty = 5000,
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
