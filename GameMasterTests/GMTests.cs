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
            var manager = new WebSocketManager<GMMessage>();
            var logger = Mock.Of<ILogger<GM>>();
            var gameMaster = new GM(conf, queue, manager, logger);
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
    }
}
