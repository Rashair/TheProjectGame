using System.Collections.Generic;

using Player.Models.Strategies.AdvancedStrategyUtils;
using Xunit;

namespace Player.StrategyTests
{
    public class ColumnGenerationTest
    {
        public static IEnumerable<object[]> TestData =>
        new List<object[]>
        {
            //// Number of players divisible by width
            new object[] { 6, 3 },
            //// Number of players not divisible by width
            new object[] { 7, 4 },
            //// Width less than number of players divisible
            new object[] { 5, 8 },
            //// Width less than number of players not divisible
            new object[] { 6, 3 },
            //// One player
            new object[] { 6, 1 },
            //// Small width
            new object[] { 2, 2 },
            //// Huge width
            new object[] { 40, 4 },
            //// Huge number of players
            new object[] { 15, 20 },
        };

        [Theory]
        [MemberData(nameof(TestData))]
        public void TestGetColumnToHandleNeverReturnsSameColumnForParticularPlayer(int width, int numberOfPlayers)
        {
            // A & A & A
            for (int k = 0; k < numberOfPlayers; ++k)
            {
                var columnGenerator = new ColumnGenerator(k, width, numberOfPlayers);
                var handledColumns = new HashSet<int>();
                for (int i = 1; i <= width; ++i)
                {
                    int col = columnGenerator.GetColumnToHandle(i);
                    Assert.DoesNotContain(col, handledColumns);
                    handledColumns.Add(col);
                }
            }
        }
    }
}
