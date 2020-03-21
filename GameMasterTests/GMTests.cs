using GameMaster.Models;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using GameMaster.Tests.Mocks;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace GameMasterTests
{
    public class GMTests
    {

        [Fact]
        public void TestAcceptMessageMoveMessage()
        {
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void TestGeneratePieceXTimes(int x)
        {
            // Arrange
            var conf = new MockConfiguration();
            var gameMaster = new GM(conf);
            var method = GetMethod("GeneratePiece");

            // Act
            for (int i = 0; i < x; ++i)
            {
                method.Invoke(gameMaster, new object[] { });
            }

            // Assert 
            int pieceCount = 0;
            var fieldInfo = GetField("board");
            AbstractField[][] board = (AbstractField[][])fieldInfo.GetValue(gameMaster);
            for (int i = 0; i < board.Length; ++i)
            {
                for(int j = 0; j < board[i].Length; ++j)
                {
                    var field = board[i][j];
                    if(board[i][j] is TaskField taskField)
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
            var taskFieldInfo = GetField("pieces", typeof(TaskField));
            var pieces = (HashSet<AbstractPiece>)taskFieldInfo.GetValue(taskField);
            return pieces.Count;
        }

        private MethodInfo GetMethod(string methodName, Type type)
        {
            Assert.False(string.IsNullOrWhiteSpace(methodName), $"{nameof(methodName)} cannot be null or whitespace");

            MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.False(method == null, $"Method {methodName} not found");

            return method;
        }

        private MethodInfo GetMethod(string methodName)
        {
            return GetMethod(methodName, typeof(GM));
        }

        private FieldInfo GetField(string fieldName, Type type)
        {
            Assert.False(string.IsNullOrWhiteSpace(fieldName), $"{nameof(fieldName)} cannot be null or whitespace");

            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.False(field == null, $"Field {fieldName} not found");

            return field;
        }

        private FieldInfo GetField(string fieldName)
        {
            return GetField(fieldName, typeof(GM));
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
            int x = 3, y = 4;
            NormalPiece piece = new NormalPiece();
            Mock<GoalField> field = new Mock<GoalField>(x,y);
            field.Setup(m => m.Put(piece)).Returns(true);
            Assert.True(piece.Put(field.Object));
        }

        [Fact]
        public void TestShamPiecePut()
        {
            int x = 3, y = 4;
            ShamPiece piece = new ShamPiece();
            Mock<NonGoalField> field = new Mock<NonGoalField>(x,y);
            field.Setup(m => m.Put(piece)).Returns(false);
            Assert.False(piece.Put(field.Object));
        }
    }
}