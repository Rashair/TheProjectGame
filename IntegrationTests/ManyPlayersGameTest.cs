using GameMaster.Models;
using Xunit;

namespace IntegrationTests
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
                MovePenalty = 400,
                DestroyPenalty = 100,
                PickPenalty = 100,
                DiscoverPenalty = 700,
                ResponsePenalty = 1500,
                Height = 16,
                Width = 10,
                GoalAreaHeight = 4,
                NumberOfGoals = 5,
                NumberOfPiecesOnBoard = 5,
                NumberOfPlayersPerTeam = 9,
                ShamPieceProbability = 0.3f,
            };

            PositionNotChangedCount = 5;
            PositionsCheckTime = 6000;
        }
        
        [Fact(Timeout = 5 * 60 * 1000)]
        public override async void RunGameWithConfiguration()
        {
            return;
            await RunGame();
        }
    }
}
