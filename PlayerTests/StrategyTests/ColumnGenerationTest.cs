using System;
using System.Collections.Generic;

using Player.Models.Strategies.AdvancedStrategyUtils;
using Xunit;

namespace Player.StrategyTests
{
    public class ColumnGenerationTest
    {
        [Theory]
        [InlineData(6, 3)] //// Number of players divisible by width
        [InlineData(7, 4)] //// Number of players not divisible by width
        [InlineData(5, 8)] //// Width less than number of players divisible
        [InlineData(6, 1)] //// Width less than number of players not divisible
        [InlineData(2, 2)] //// Small width
        [InlineData(40, 4)] //// Huge width
        [InlineData(15, 20)] //// Huge number of players
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

        [Theory]
        [InlineData(6, 3)] //// Number of players divisible by width
        [InlineData(7, 4)] //// Number of players not divisible by width
        [InlineData(2, 2)] //// Small width
        [InlineData(40, 4)] //// Huge width, divisible one
        [InlineData(40, 5)] //// Huge width, divisible two
        [InlineData(40, 8)] //// Huge width, divisble three
        [InlineData(40, 7)] //// Huge width, not divisible one
        [InlineData(40, 9)] //// Huge width, not divisible two
        [InlineData(40, 11)] //// Huge width, not divisible three
        public void TestGetColumnToHandleNeverReturnsSameColumnForTwoPlayersInitialy(int width, int numberOfPlayers)
        {
            Assert.False(width < numberOfPlayers);

            // A & A & A
            var handledColumns = new HashSet<int>();
            for (int k = 0; k < numberOfPlayers; ++k)
            {
                var columnGenerator = new ColumnGenerator(k, width, numberOfPlayers);
                int col = columnGenerator.GetColumnToHandle(1);
                Assert.DoesNotContain(col, handledColumns);
                handledColumns.Add(col);
            }
        }

        [Theory]
        [InlineData(6, 3)] //// Number of players divisible by width one 
        [InlineData(9, 3)] //// Number of players divisible by width two
        [InlineData(7, 4)] //// Number of players not divisible by width
        [InlineData(40, 4)] //// Huge width, divisible one
        [InlineData(40, 5)] //// Huge width, divisible two
        [InlineData(40, 8)] //// Huge width, divisble three
        [InlineData(40, 7)] //// Huge width, not divisible one
        [InlineData(40, 9)] //// Huge width, not divisible two
        [InlineData(40, 11)] //// Huge width, not divisible three
        [InlineData(20, 11)] //// Huge width, not divisible three
        public void TestGetColumnToHandleRarelyReturnsSameColumnForTwoPlayersAfterSecondTime(int width, int numberOfPlayers)
        {
            // Arrange
            Assert.False(width < numberOfPlayers);
            var handledColumns = new HashSet<int>();
            int repeatCount = 0;

            // Act
            for (int k = 0; k < numberOfPlayers; ++k)
            {
                var columnGenerator = new ColumnGenerator(k, width, numberOfPlayers);

                int col1 = columnGenerator.GetColumnToHandle(1);
                int col2 = columnGenerator.GetColumnToHandle(2);
                repeatCount += handledColumns.Contains(col1) ? 1 : 0;
                repeatCount += handledColumns.Contains(col2) ? 1 : 0;
                handledColumns.Add(col1);
                handledColumns.Add(col2);
            }

            // Assert
            int val = Math.Max((numberOfPlayers * 2) - width, numberOfPlayers * 2 / width);
            Assert.True(repeatCount <= val, $"Should not repeat, \n expected {repeatCount} <= {val} ");
        }

        [Theory]
        [InlineData(9, 3)] //// Number of players divisible by width two
        [InlineData(40, 4)] //// Huge width, divisible one
        [InlineData(40, 5)] //// Huge width, divisible two
        [InlineData(40, 8)] //// Huge width, divisble three
        [InlineData(40, 7)] //// Huge width, not divisible one
        [InlineData(40, 9)] //// Huge width, not divisible two
        [InlineData(40, 11)] //// Huge width, not divisible three
        [InlineData(30, 11)] //// Big width, not divisible three
        public void TestGetColumnToHandleRarelyReturnsSameColumnForTwoPlayersAfterThirdTime(int width, int numberOfPlayers)
        {
            // Arrange
            Assert.False(width < numberOfPlayers);
            var handledColumns = new HashSet<int>();
            int repeatCount = 0;

            // Act
            for (int k = 0; k < numberOfPlayers; ++k)
            {
                var columnGenerator = new ColumnGenerator(k, width, numberOfPlayers);

                int col1 = columnGenerator.GetColumnToHandle(1);
                int col2 = columnGenerator.GetColumnToHandle(2);
                int col3 = columnGenerator.GetColumnToHandle(3);
                repeatCount += handledColumns.Contains(col1) ? 1 : 0;
                repeatCount += handledColumns.Contains(col2) ? 1 : 0;
                repeatCount += handledColumns.Contains(col3) ? 1 : 0;
                handledColumns.Add(col1);
                handledColumns.Add(col2);
                handledColumns.Add(col3);
            }

            // Assert
            int val = Math.Max((numberOfPlayers * 3) - width, numberOfPlayers * 3 / width);
            Assert.True(repeatCount <= val, $"Should not repeat, \n expected {repeatCount} <= {val} ");
        }
    }
}
