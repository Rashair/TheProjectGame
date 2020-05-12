using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Models;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using GameMaster.Tests.Mocks;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace GameMaster.Tests
{
    public class GMTests
    {
        private readonly ILogger logger = MockGenerator.Get<ILogger>();

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void TestGeneratePieceXTimes(int x)
        {
            // Arrange
            var conf = new MockGameConfiguration
            {
                NumberOfPiecesOnBoard = 1
            };
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            gameMaster.Invoke("InitGame");

            // Act
            for (int i = 0; i < x; ++i)
            {
                gameMaster.Invoke("GeneratePiece");
            }

            // Assert
            int pieceCount = 0;
            var board = gameMaster.GetValue<GM, AbstractField[][]>("board");
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

            Assert.Equal(x + 1, pieceCount);
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
            field.Setup(m => m.Put(piece)).Returns((true, true));
            Assert.True(piece.Put(field.Object).goal);
        }

        [Fact]
        public void TestShamPiecePut()
        {
            int x = 3;
            int y = 4;
            ShamPiece piece = new ShamPiece();
            Mock<NonGoalField> field = new Mock<NonGoalField>(x, y);
            field.Setup(m => m.Put(piece)).Returns((false, true));
            Assert.False(piece.Put(field.Object).goal);
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
            var conf = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            gameMaster.Invoke("InitGame");
            for (int i = 0; i < pieceCount; ++i)
            {
                gameMaster.Invoke("GeneratePiece");
            }

            // Act
            var discoveryActionResult = gameMaster.Invoke<GM, Dictionary<Direction, int>>("Discover", field);

            // Assert
            var board = gameMaster.GetValue<GM, AbstractField[][]>("board");
            List<(AbstractField field, int dist, Direction dir)> neighbours = GetNeighbours(field, board, conf.Height, conf.Width);

            for (int k = 0; k < neighbours.Count; k++)
            {
                for (int i = conf.GoalAreaHeight; i < conf.Height - conf.GoalAreaHeight; i++)
                {
                    for (int j = 0; j < board[i].Length; j++)
                    {
                        int dist = ManhattanDistance(neighbours[k].field, board[i][j]);
                        if (dist < neighbours[k].dist)
                            neighbours[k] = (neighbours[k].field, dist, neighbours[k].dir);
                    }
                }
                if (discoveryActionResult[neighbours[k].dir] != neighbours[k].dist)
                {
                    Assert.False(discoveryActionResult[neighbours[k].dir] == neighbours[k].dist,
                        $"Incorect value for distance: {discoveryActionResult[neighbours[k].dir]} != {neighbours[k].dist}");
                }
            }

            int discoveredFields = 0;
            foreach (var distance in discoveryActionResult)
            {
                if (distance.Value >= 0)
                    ++discoveredFields;
            }
            Assert.Equal(neighbours.Count, discoveredFields);
        }

        public int ManhattanDistance(AbstractField f1, AbstractField f2)
        {
            return Math.Abs(f1.GetPosition()[0] - f2.GetPosition()[0]) + Math.Abs(f1.GetPosition()[1] - f2.GetPosition()[1]);
        }

        public List<(AbstractField, int, Direction)> GetNeighbours(AbstractField field, AbstractField[][] board, int height, int width)
        {
            int[] center = field.GetPosition();
            List<(AbstractField, int, Direction)> neighbours = new List<(AbstractField, int, Direction)>();
            var neighbourCoordinates = DirectionExtensions.GetCoordinatesAroundCenter(center);

            for (int i = 0; i < neighbourCoordinates.Length; i++)
            {
                var (dir, y, x) = neighbourCoordinates[i];
                if (y >= 0 && y < height && x >= 0 && x < width)
                {
                    neighbours.Add((board[y][x], int.MaxValue, dir));
                }
            }
            return neighbours;
        }

        [Fact]
        public void TestInitializePlayersPositions()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            var players = gameMaster.GetValue<GM, Dictionary<int, GMPlayer>>("players");
            for (int i = 0; i < conf.NumberOfPlayersPerTeam; ++i)
            {
                players.Add(i, new GMPlayer(i, conf, client, Team.Red, logger));
                int j = i + conf.NumberOfPlayersPerTeam;
                players.Add(j, new GMPlayer(j, conf, client, Team.Blue, logger));
            }
            gameMaster.Invoke("InitGame");
            var board = gameMaster.GetValue<GM, AbstractField[][]>("board");
            var initializer = new GMInitializer(conf, board);

            // Act
            initializer.InitializePlayersPoisitions(players);

            // Assert
            Assert.All(players.Values, p =>
            {
                Assert.False(p.GetPosition() == null, "All players have positions");
            });

            Func<int[], int[], bool> arePositionsTheSame =
                (int[] posA, int[] posB) => posA[0] == posB[0] && posA[1] == posB[1];
            for (int i = 0; i < players.Count; ++i)
            {
                var playerA = players[i];
                var posA = playerA.GetPosition();
                (int y1, int y2) = playerA.Team == Team.Red ? (0, conf.Height - conf.GoalAreaHeight) :
                    (conf.GoalAreaHeight, conf.Height);
                Assert.False(posA[0] < y1 || posA[0] >= y2, "No players are placed on GoalArea of enemy");
                for (int j = i + 1; j < players.Count; ++j)
                {
                    var playerB = players[j];
                    var posB = playerB.GetPosition();
                    Assert.False(arePositionsTheSame(posA, posB), "No 2 players share the same position");
                }
            }
        }

        [Fact]
        public void TestInitGame()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            var gameMaster = new GM(lifetime, conf, queue, client, logger);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.True(gameMaster.WasGameInitialized);

            int redGoalFieldsCount = 0;
            int blueGoalFieldsCount = 0;
            int taskFieldsCount = 0;
            int piecesCount = 0;
            var board = gameMaster.GetValue<GM, AbstractField[][]>("board");
            for (int i = 0; i < board.Length; ++i)
            {
                for (int j = 0; j < board[i].Length; ++j)
                {
                    AbstractField field = board[i][j];
                    if (field is GoalField)
                    {
                        if (i < conf.GoalAreaHeight)
                        {
                            ++redGoalFieldsCount;
                        }
                        else if (i >= gameMaster.SecondGoalAreaStart)
                        {
                            ++blueGoalFieldsCount;
                        }
                        else
                        {
                            Assert.True(false, "Goal field should be on correct position");
                        }

                        if (field.ContainsPieces())
                        {
                            Assert.True(false, "Goal field should not contain any pieces");
                        }
                    }
                    else if (field is TaskField taskField)
                    {
                        ++taskFieldsCount;
                        if (field.ContainsPieces())
                        {
                            piecesCount += GetPieceCount(taskField);
                        }
                    }
                }
            }

            Assert.True(conf.NumberOfGoals == redGoalFieldsCount,
                $"Number of red goal fields should match configuration setting.\n" +
                $"Have: {redGoalFieldsCount}\n" +
                $"Expected: {conf.NumberOfGoals}");
            Assert.True(conf.NumberOfGoals == blueGoalFieldsCount,
             $"Number of red goal fields should match configuration setting.\n" +
             $"Have: {blueGoalFieldsCount}\n" +
             $"Expected: {conf.NumberOfGoals}");

            int expectedTaskFieldsCount = (conf.Height - (2 * conf.GoalAreaHeight)) * conf.Width;
            Assert.True(expectedTaskFieldsCount == taskFieldsCount,
                "Task fields should cover all fields except goal areas.\n" +
                 $"Have: {taskFieldsCount}\n" +
                 $"Expected: {expectedTaskFieldsCount}");

            Assert.True(conf.NumberOfPiecesOnBoard == piecesCount,
                "GM should generate enough pieces.\n" +
                 $"Have: {piecesCount}\n" +
                 $"Expected: {conf.NumberOfPiecesOnBoard}");
        }

        [Fact]
        public void TestStartGame()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            var players = gameMaster.GetValue<GM, Dictionary<int, GMPlayer>>("players");

            for (int idRed = 0; idRed < conf.NumberOfPlayersPerTeam; ++idRed)
            {
                var player = new GMPlayer(idRed, conf, client, Team.Red, logger);
                players.Add(idRed, player);

                int idBlue = idRed + conf.NumberOfPlayersPerTeam;
                player = new GMPlayer(idBlue, conf, client, Team.Blue, logger);
                players.Add(idBlue, player);
            }
            gameMaster.Invoke("InitGame");

            // Act
            var task = gameMaster.Invoke<GM, Task>("StartGame", CancellationToken.None);
            task.Wait();

            // Assert
            Assert.True(gameMaster.WasGameStarted);

            // TODO create mock of socket and check if GM sends messages
        }

        [Fact]
        public void TestDiscoverShouldReturnNegativeNumbers()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            gameMaster.Invoke("InitGame");

            // Act
            var distances = gameMaster.Invoke<GM, Dictionary<Direction, int>>("Discover", new TaskField(0, 5));
            var board = gameMaster.GetValue<GM, AbstractField[][]>("board");

            int expectedResult = -1;
            int resultS = distances[Direction.S];
            int resultSE = distances[Direction.SE];
            int resultSW = distances[Direction.SW];

            // Assert
            Assert.Equal(expectedResult, resultS);
            Assert.Equal(expectedResult, resultSE);
            Assert.Equal(expectedResult, resultSW);
        }
    }
}
