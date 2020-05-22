using GameMaster.Models;
using IntegrationTests.GameTests.Abstractions;
using Xunit;

namespace IntegrationTests.GameTests
{
    public class ManyShamsGameTest : GameTest
    {
        public ManyShamsGameTest()
        {
            Conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 5002,
                MovePenalty = 200,
                AskPenalty = 1000,
                PutPenalty = 300,
                CheckPenalty = 300,
                DestroyPenalty = 50,
                PickupPenalty = 100,
                DiscoverPenalty = 1200,
                ResponsePenalty = 600,
                PrematureRequestPenalty = 1000,
                Height = 12,
                Width = 10,
                GoalAreaHeight = 3,
                NumberOfGoals = 6,
                NumberOfPiecesOnBoard = 12,
                NumberOfPlayersPerTeam = 5,
                ShamPieceProbability = 0.75f,
            };

            TestConf.MinimumRunTimeSec = 60;
            TestConf.CheckInterval = 5000;
        }

        [Fact(Timeout = 8 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
