using GameMaster.Models;
using Xunit;

namespace IntegrationTests
{
    public class SimpleGameTest : GameTest
    {
        public SimpleGameTest()
        {
            this.conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 5001,
                AskPenalty = 1000,
                PutPenalty = 750,
                CheckPenalty = 400,
                MovePenalty = 300,
                DestroyPenalty = 100,
                DiscoverPenalty = 1500,
                ResponsePenalty = 600,
                Height = 12,
                Width = 6,
                GoalAreaHeight = 3,
                NumberOfGoals = 4,
                NumberOfPiecesOnBoard = 6,
                NumberOfPlayersPerTeam = 3,
                ShamPieceProbability = 0.2f,
            };

            this.positionNotChangedCount = 4;
            this.positionsCheckTime = 5000;
        }

        [Fact(Timeout = 3 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
