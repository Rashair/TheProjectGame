using GameMaster.Models;

namespace GameMaster.Tests.Mocks
{
    internal class MockConfiguration : Configuration
    {
        public MockConfiguration() : base()
        {
            this.Height = 12;
            this.Width = 10;
            this.NumberOfGoals = 4;
            this.GoalAreaHeight = 3;
            this.ShamPieceProbability = 40;
        }
    }
}
