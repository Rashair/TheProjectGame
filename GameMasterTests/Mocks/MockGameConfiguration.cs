using GameMaster.Models;

namespace GameMaster.Tests.Mocks
{
    internal class MockGameConfiguration : GameConfiguration
    {
        public MockGameConfiguration()
        {
            this.CsIP = "192.168.0.0";
            this.CsPort = 3729;
            this.MovePenalty = 1500;
            this.AskPenalty = 1000;
            this.DiscoverPenalty = 700;
            this.PutPenalty = 500;
            this.CheckPenalty = 700;
            this.ResponsePenalty = 1000;
            this.GoalAreaHeight = 5;
            this.Height = 12;
            this.Width = 10;
            this.NumberOfGoals = 4;
            this.GoalAreaHeight = 3;
            this.ShamPieceProbability = 40;
            this.NumberOfPiecesOnBoard = 10;
            this.NumberOfPlayersPerTeam = 6;
        }
    }
}
