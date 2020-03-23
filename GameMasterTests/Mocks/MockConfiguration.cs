using GameMaster.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameMaster.Tests.Mocks
{
    class MockConfiguration : Configuration
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
