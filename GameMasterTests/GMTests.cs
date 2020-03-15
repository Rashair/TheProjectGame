using GameMaster.Models;
using GameMaster.Models.Fields;
using GameMaster.Tests.Mocks;
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

        [Fact]
        public void TestGeneratePieceOnce()
        {
            // Arrange
            var conf = new MockConfiguration();
            var gameMaster = new GM(conf);
            var method = GetMethod("GeneratePiece");

            // Act
            method.Invoke(gameMaster, new object[] { });

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
                            ++pieceCount;
                        }
                    }
                    else
                    {
                        Assert.False(field.ContainsPieces(), "Pieces should not be generated on goal area");
                    }   
                }
            }

            Assert.Equal(1, pieceCount);
        }

        private MethodInfo GetMethod(string methodName)
        {
            Assert.False(string.IsNullOrWhiteSpace(methodName), $"{nameof(methodName)} cannot be null or whitespace");

            MethodInfo method = typeof(GM).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.False(method == null, $"Method {methodName} not found");

            return method;
        }

        private FieldInfo GetField(string fieldName)
        {
            Assert.False(string.IsNullOrWhiteSpace(fieldName), $"{nameof(fieldName)} cannot be null or whitespace");

            FieldInfo field = typeof(GM).GetField(fieldName, BindingFlags.NonPublic);

            Assert.False(field == null, $"Field {fieldName} not found");

            return field;
        }
    }
}