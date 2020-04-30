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
                AskPenalty = 1500,
                PutPenalty = 400,
                CheckPenalty = 300,
                MovePenalty = 300,
                DestroyPenalty = 100,
                PickPenalty = 100,
                DiscoverPenalty = 700,
                ResponsePenalty = 1500,
                Height = 20,
                Width = 20,
                GoalAreaHeight = 4,
                NumberOfGoals = 6,
                NumberOfPiecesOnBoard = 20,
                NumberOfPlayersPerTeam = 16,
                ShamPieceProbability = 0.2f,
            };

            TestConf.PositionNotChangedThreshold = 5;
            TestConf.CheckInterval = 6000;
        }

        [Fact(Timeout = 10 * 60 * 1000)]
        public override void RunGameWithConfiguration()
        {
            RunGame();
        }
    }
}
