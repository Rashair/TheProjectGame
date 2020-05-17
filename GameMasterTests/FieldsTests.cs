using System.Collections;
using System.Collections.Generic;

using GameMaster.Models;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Moq;
using Serilog;
using Shared.Clients;
using Shared.Enums;
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
                var socketClient = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
                yield return new object[]
                {
                    new List<GMPlayer>
                    {
                        new GMPlayer(1, conf, socketClient, Shared.Enums.Team.Red, logger),
                        new GMPlayer(2, conf, socketClient, Shared.Enums.Team.Red, logger),
                    },
                    false,
                };
                yield return new object[]
                {
                    new List<GMPlayer> { new GMPlayer(1, conf, socketClient, Shared.Enums.Team.Red, logger) },
                    true,
                };
                yield return new object[]
                {
                    new List<GMPlayer> { null, new GMPlayer(1, conf, socketClient, Shared.Enums.Team.Red, logger) },
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
            private const int RandomY = 0;
            private const int RandomX = 5;

            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[]
                {
                    new GoalField(RandomY, RandomX),
                    new List<AbstractPiece> { new ShamPiece() },
                    new List<PutEvent> { PutEvent.ShamOnGoalArea }
                };
                yield return new object[]
                {
                    new GoalField(RandomY, RandomX),
                    new List<AbstractPiece> { new NormalPiece(), new ShamPiece() },
                    new List<PutEvent> { PutEvent.NormalOnGoalField, PutEvent.ShamOnGoalArea }
                };
                yield return new object[]
                {
                    new GoalField(RandomY, RandomX),
                    new List<AbstractPiece> { new NormalPiece(), new NormalPiece() },
                    new List<PutEvent> { PutEvent.NormalOnGoalField, PutEvent.NormalOnNonGoalField }
                };
                yield return new object[]
             {
                    new GoalField(RandomY, RandomX),
                    new List<AbstractPiece> { new ShamPiece(), new NormalPiece(), new NormalPiece() },
                    new List<PutEvent> { PutEvent.ShamOnGoalArea, PutEvent.NormalOnGoalField, PutEvent.NormalOnNonGoalField }
             };
                yield return new object[]
{
                    new NonGoalField(RandomY, RandomX),
                    new List<AbstractPiece> { new ShamPiece() },
                    new List<PutEvent> { PutEvent.ShamOnGoalArea }
};
                yield return new object[]
                {
                    new NonGoalField(RandomY, RandomX),
                    new List<AbstractPiece> { new NormalPiece(), new ShamPiece() },
                    new List<PutEvent> { PutEvent.NormalOnNonGoalField, PutEvent.ShamOnGoalArea }
                };
                yield return new object[]
                {
                    new NonGoalField(RandomY, RandomX),
                    new List<AbstractPiece> { new NormalPiece(), new NormalPiece() },
                    new List<PutEvent> { PutEvent.NormalOnNonGoalField, PutEvent.NormalOnNonGoalField }
                };
                yield return new object[]
{
                    new TaskField(RandomY, RandomX),
                    new List<AbstractPiece> { new ShamPiece() },
                    new List<PutEvent> { PutEvent.TaskField }
};
                yield return new object[]
                {
                    new TaskField(RandomY, RandomX),
                    new List<AbstractPiece> { new NormalPiece(), new ShamPiece() },
                    new List<PutEvent> { PutEvent.TaskField, PutEvent.TaskField }
                };
                yield return new object[]
                {
                    new TaskField(RandomY, RandomX),
                    new List<AbstractPiece> { new NormalPiece(), new NormalPiece() },
                    new List<PutEvent> { PutEvent.TaskField, PutEvent.TaskField }
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(PutGoalTestData))]
        public void PutGoalTest(AbstractField field, List<AbstractPiece> pieces, List<PutEvent> expectedEvents)
        {
            // Arrange

            // Act & Assert
            for (int i = 0; i < pieces.Count; ++i)
            {
                PutEvent result = field.Put(pieces[i]).putEvent;
                Assert.Equal(expectedEvents[i], result);
            }
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
            var socketClient = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            GMPlayer gmPlayer = new GMPlayer(1, conf, socketClient, Shared.Enums.Team.Red, logger);
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
