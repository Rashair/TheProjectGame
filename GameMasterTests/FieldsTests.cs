using System.Collections;
using System.Collections.Generic;

using GameMaster.Managers;
using GameMaster.Models;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Moq;
using Serilog;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace GameMaster.Tests
{
    public class FieldsTests
    {
        private readonly ILogger logger = MockGenerator.Get<ILogger>();

        public class MoveHereTestData : IEnumerable<object[]>
        {
            private readonly ILogger logger = MockGenerator.Get<ILogger>();

            public IEnumerator<object[]> GetEnumerator()
            {
                var conf = Mock.Of<GameConfiguration>();
                var socketManager = new Mock<TcpSocketManager<GMMessage>>(logger).Object;
                yield return new object[]
                {
                    new List<GMPlayer>
                    {
                        new GMPlayer(1, conf, socketManager, Shared.Enums.Team.Red, logger),
                        new GMPlayer(2, conf, socketManager, Shared.Enums.Team.Red, logger),
                    },
                    false,
                };
                yield return new object[]
                {
                    new List<GMPlayer> { new GMPlayer(1, conf, socketManager, Shared.Enums.Team.Red, logger) },
                    true,
                };
                yield return new object[]
                {
                    new List<GMPlayer> { null, new GMPlayer(1, conf, socketManager, Shared.Enums.Team.Red, logger) },
                    true,
                };
                yield return new object[] { new List<GMPlayer> { null }, false };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(MoveHereTestData))]
        public void MoveHereTest(List<GMPlayer> players, bool expected)
        {
            // Arrange
            TaskField taskField = new TaskField(2, 2);
            bool result = false;

            // Act
            foreach (GMPlayer p in players)
            {
                result = taskField.MoveHere(p);
            }

            // Assert
            Assert.Equal(expected, result);
        }

        public class PutGoalTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { new List<AbstractPiece> { new NormalPiece(), new ShamPiece() }, null };
                yield return new object[] { new List<AbstractPiece> { new NormalPiece() }, true };
                yield return new object[] { new List<AbstractPiece> { new ShamPiece() }, null };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(PutGoalTestData))]
        public void PutGoalTest(List<AbstractPiece> pieces, bool? expected)
        {
            // Arrange
            GoalField goalField = new GoalField(5, 0);
            bool? result = null;

            // Act
            foreach (AbstractPiece p in pieces)
            {
                result = goalField.Put(p).Item1;
            }

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1, 2, false)]
        [InlineData(1, 1, true)]
        [InlineData(2, 4, false)]
        [InlineData(4, 3, true)]
        public void PickUpTaskTest(int numPut, int numPick, bool expected)
        {
            // Arrange
            var conf = Mock.Of<GameConfiguration>();
            var socketManager = new Mock<TcpSocketManager<GMMessage>>(logger).Object;
            GMPlayer gmPlayer = new GMPlayer(1, conf, socketManager, Shared.Enums.Team.Red, logger);
            TaskField taskField = new TaskField(2, 2);
            for (int i = 0; i < numPut; i++)
            {
                taskField.Put(new NormalPiece());
            }
            bool result = false;

            // Act
            for (int i = 0; i < numPick; i++)
            {
                result = taskField.PickUp(gmPlayer);
            }

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
