using GameMaster.Models;
using Xunit;

namespace IntegrationTests
{
    public class ManyShamsGameTest : GameTest
    {
        public ManyShamsGameTest()
        {
            this.conf = new GameConfiguration
            {
                CsIP = "127.0.0.1",
                CsPort = 5002,
                AskPenalty = 1000,
                PutPenalty = 300,
                CheckPenalty = 300,
                MovePenalty = 300,
                DestroyPenalty = 100,
                PickPenalty = 100,
                DiscoverPenalty = 1500,
                ResponsePenalty = 600,
                Height = 12,
                Width = 10,
                GoalAreaHeight = 3,
                NumberOfGoals = 6,
                NumberOfPiecesOnBoard = 12,
                NumberOfPlayersPerTeam = 4,
                ShamPieceProbability = 0.8f,
            };

            this.positionNotChangedCount = 3;
            this.positionsCheckTime = 6000;
        }

        [Fact(Timeout = 8 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            await RunGame();
        }
    }
}
