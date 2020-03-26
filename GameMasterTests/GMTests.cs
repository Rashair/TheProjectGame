using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using GameMaster.Models;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using GameMaster.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Enums;
using Shared.Messages;
using Xunit;

using static GameMaster.Tests.Helpers.ReflectionHelpers;

namespace GameMaster.Tests
{
    public class GMTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void TestGeneratePieceXTimes(int x)
        {
            // Arrange
            var conf = new MockConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var logger = Mock.Of<ILogger<GM>>();
            var socketManager = new WebSocketManager<GMMessage>();
            var gameMaster = new GM(conf, queue, logger, socketManager);

            var startGame = GetMethod("StartGame");
            startGame.Invoke(gameMaster, null);
            var method = GetMethod("GeneratePiece");

            // Act
            for (int i = 0; i < x; ++i)
            {
                method.Invoke(gameMaster, null);
            }

            // Assert
            int pieceCount = 0;
            var fieldInfo = GetField("board");
            AbstractField[][] board = (AbstractField[][])fieldInfo.GetValue(gameMaster);
            for (int i = 0; i < board.Length; ++i)
            {
                for (int j = 0; j < board[i].Length; ++j)
                {
                    var field = board[i][j];
                    if (board[i][j] is TaskField taskField)
                    {
                        if (taskField.ContainsPieces())
                        {
                            pieceCount += GetPieceCount(taskField);
                        }
                    }
                    else
                    {
                        Assert.False(field.ContainsPieces(), "Pieces should not be generated on goal area");
                    }
                }
            }

            Assert.Equal(x, pieceCount);
        }

        private int GetPieceCount(TaskField taskField)
        {
            var taskFieldInfo = typeof(AbstractField).GetProperty("Pieces",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var pieces = (HashSet<AbstractPiece>)taskFieldInfo.GetValue(taskField);
            return pieces.Count;
        }

        [Fact]
        public void TestNormalPieceCheck()
        {
            NormalPiece piece = new NormalPiece();
            Assert.False(piece.CheckForSham());
        }

        [Fact]
        public void TestShamPieceCheck()
        {
            ShamPiece piece = new ShamPiece();
            Assert.True(piece.CheckForSham());
        }

        [Fact]
        public void TestNormalPiecePut()
        {
            int x = 3;
            int y = 4;
            NormalPiece piece = new NormalPiece();
            Mock<GoalField> field = new Mock<GoalField>(x, y);
            field.Setup(m => m.Put(piece)).Returns(true);
            Assert.True(piece.Put(field.Object));
        }

        [Fact]
        public void TestShamPiecePut()
        {
            int x = 3;
            int y = 4;
            ShamPiece piece = new ShamPiece();
            Mock<NonGoalField> field = new Mock<NonGoalField>(x, y);
            field.Setup(m => m.Put(piece)).Returns(false);
            Assert.False(piece.Put(field.Object));
        }

        public class DiscoverTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { new TaskField(1, 1), 8 };
                yield return new object[] { new TaskField(7, 0), 3 };
                yield return new object[] { new TaskField(7, 9), 4 };
                yield return new object[] { new TaskField(10, 0), 5 };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(DiscoverTestData))]
        public void DiscoverTest(TaskField field, int pieceCount)
        {
            var conf = new MockConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var logger = Mock.Of<ILogger<GM>>();
            var gameMaster = new GM(conf, queue, logger);
            var startGame = GetMethod("StartGame");
            startGame.Invoke(gameMaster, null);
            var generatePiece = GetMethod("GeneratePiece");

            // Act
            for (int i = 0; i < pieceCount; ++i)
            {
                generatePiece.Invoke(gameMaster, null);
            }

            // Act
            var discover = GetMethod("Discover");
            Dictionary<Direction, int> discoveryActionResult = (Dictionary<Direction, int>)discover.Invoke(gameMaster, new object[] { field });

            // Assert
            var fieldInfo = GetField("board");
            AbstractField[][] board = (AbstractField[][])fieldInfo.GetValue(gameMaster);
            List<(AbstractField, int, Direction)> neighbours = GetNeighbours(field, board, conf.GoalAreaHeight, conf.Height, conf.Width);

            for (int k = 0; k < neighbours.Count; k++)
            {
                for (int i = conf.GoalAreaHeight; i < conf.Height - conf.GoalAreaHeight; i++)
                {
                    for (int j = 0; j < board[i].Length; j++)
                    {
                        int dist = ManhattanDistance(neighbours[k].Item1, board[i][j]);
                        if (dist < neighbours[k].Item2)
                            neighbours[k] = (neighbours[k].Item1, dist, neighbours[k].Item3);
                    }
                }
                if (discoveryActionResult[neighbours[k].Item3] != neighbours[k].Item2)
                {
                    Assert.False(discoveryActionResult[neighbours[k].Item3] == neighbours[k].Item2, string.Format("Incorect Value for distance {0} != {1}", discoveryActionResult[neighbours[k].Item3], neighbours[k].Item2));
                }
            }
            Assert.Equal(neighbours.Count, discoveryActionResult.Count);
        }

        public int ManhattanDistance(AbstractField f1, AbstractField f2)
        {
            return Math.Abs(f1.GetPosition()[0] - f2.GetPosition()[0]) + Math.Abs(f1.GetPosition()[1] - f2.GetPosition()[1]);
        }

        public List<(AbstractField, int, Direction)> GetNeighbours(AbstractField field, AbstractField[][] board, int goalAreaHeight, int height, int width)
        {
            int[] center = field.GetPosition();
            List<(AbstractField, int, Direction)> neighbours = new List<(AbstractField, int, Direction)>();
            Direction[] directions = (Direction[])Enum.GetValues(typeof(Direction));
            Array.Sort(directions);

            (int, int)[] neighbourCoordinates = new (int, int)[]
             {
                 (center[0] - 1, center[1] - 1),  (center[0] - 1, center[1]), (center[0] - 1, center[1] + 1),
                 (center[0], center[1] - 1), (center[0], center[1]), (center[0], center[1] + 1),
                 (center[0] + 1, center[1] - 1), (center[0] + 1, center[1]), (center[0] + 1, center[1] + 1),
             };

            for (int i = 0; i < neighbourCoordinates.Length; i++)
            {
                int x = neighbourCoordinates[i].Item1;
                int y = neighbourCoordinates[i].Item2;
                if (x >= 0 && x < height && y >= 0 && y < width)
                {
                    neighbours.Add((board[x][y], int.MaxValue, directions[i]));
                }
            }
            return neighbours;
        }
    }
}
