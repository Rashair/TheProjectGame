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
using Newtonsoft.Json;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;
using TestsShared;
using Xunit;

namespace GameMaster.Tests
{
    public class GMTests
    {
        private readonly ILogger logger = MockGenerator.Get<ILogger>();
        private Stack<GMMessage> sendedMessages = new Stack<GMMessage>();

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
            GoalField field = new GoalField(x, y);
            bool result = field.Put(piece).putEvent == PutEvent.NormalOnGoalField ? true : false;
            Assert.True(result);
        }

        [Fact]
        public void TestShamPiecePut()
        {
            int x = 3;
            int y = 4;
            ShamPiece piece = new ShamPiece();
            NonGoalField field = new NonGoalField(x, y);
            bool result = field.Put(piece).putEvent == PutEvent.ShamOnGoalArea ? true : false;
            Assert.True(result);
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
            var discoveryActionResult = gameMaster.Invoke<GM, Dictionary<Direction, int?>>("Discover", field);

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
            var distances = gameMaster.Invoke<GM, Dictionary<Direction, int?>>("Discover", new TaskField(0, 5));
            var board = gameMaster.GetValue<GM, AbstractField[][]>("board");

            int? expectedResult = null;
            int? resultS = distances[Direction.S];
            int? resultSE = distances[Direction.SE];
            int? resultSW = distances[Direction.SW];

            // Assert
            Assert.Equal(expectedResult, resultS);
            Assert.Equal(expectedResult, resultSE);
            Assert.Equal(expectedResult, resultSW);
        }

        private GM ValidationConfGMHelper(GameConfiguration conf)
        {
            var queue = new BufferBlock<PlayerMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            return new GM(lifetime, conf, queue, client, logger);
        }

        [Fact]
        public void TestValidateConf()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.True(gameMaster.WasGameInitialized);
        }

        [Fact]
        public void TestValidateConfWidth()
        {
            // Arrange
            var conf = new MockGameConfiguration()
            {
                Width = 0
            };
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.False(gameMaster.WasGameInitialized);
        }

        [Fact]
        public void TestValidateConfHeight()
        {
            // Arrange
            var conf = new MockGameConfiguration()
            {
                Height = 2
            };
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.False(gameMaster.WasGameInitialized);
        }

        [Fact]
        public void TestValidateConfGoalAreaHeight()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            conf.GoalAreaHeight = (conf.Height / 2) + 1;
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.False(gameMaster.WasGameInitialized);
        }

        [Fact]
        public void TestValidateConfNumberOfGoals()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            conf.NumberOfGoals = (conf.GoalAreaHeight * conf.Width) + 1;
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.False(gameMaster.WasGameInitialized);
        }

        [Fact]
        public void TestValidateConfNumberOfPlayersPerTeam()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            conf.NumberOfGoals = (conf.Height * conf.Width) + 1;
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.False(gameMaster.WasGameInitialized);
        }

        [Fact]
        public void TestValidateConfNumberOfPiecesOnBoard()
        {
            // Arrange
            var conf = new MockGameConfiguration();
            conf.NumberOfPiecesOnBoard = ((conf.Height - (conf.GoalAreaHeight * 2)) * conf.Width) + 1;
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.False(gameMaster.WasGameInitialized);
        }

        [Fact]
        public void TestValidateConfShamPieceProbability()
        {
            // Arrange
            var conf = new MockGameConfiguration()
            {
                ShamPieceProbability = 1
            };
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.False(gameMaster.WasGameInitialized);
        }

        [Fact]
        public void TestValidatePenalty()
        {
            // Arrange
            var conf = new MockGameConfiguration()
            {
                PickupPenalty = 0
            };
            var gameMaster = ValidationConfGMHelper(conf);

            // Act
            gameMaster.Invoke("InitGame");

            // Assert
            Assert.False(gameMaster.WasGameInitialized);
        }

        private GameConfiguration GenerateConfiguration()
        {
            return new MockGameConfiguration();
        }

        private BufferBlock<PlayerMessage> GenerateBuffer()
        {
            return new BufferBlock<PlayerMessage>();
        }

        private ISocketClient<PlayerMessage, GMMessage> GenerateSocketClient()
        {
            var mock = new Mock<ISocketClient<PlayerMessage, GMMessage>>();
            mock.Setup(c => c.SendAsync(It.IsAny<GMMessage>(), It.IsAny<CancellationToken>())).
                Callback<GMMessage, CancellationToken>((m, c) => sendedMessages.Push(m));
            return mock.Object;
        }

        public async Task InitializeAndBegForInfo(GM gameMaster, int player1ID, int player2ID)
        {
            JoinGamePayload joinGamePayload = new JoinGamePayload()
            {
                TeamId = Team.Blue,
            };

            PlayerMessage joinMessage1 = new PlayerMessage()
            {
                MessageID = PlayerMessageId.JoinTheGame,
                AgentID = player1ID,
                Payload = joinGamePayload.Serialize(),
            };
            PlayerMessage joinMessage2 = new PlayerMessage()
            {
                MessageID = PlayerMessageId.JoinTheGame,
                AgentID = player2ID,
                Payload = joinGamePayload.Serialize(),
            };
            await gameMaster.AcceptMessage(joinMessage1, CancellationToken.None);
            await gameMaster.AcceptMessage(joinMessage2, CancellationToken.None);
            gameMaster.Invoke("StartGame", CancellationToken.None);
            BegForInfoPayload begForInfoPayload = new BegForInfoPayload()
            {
                AskedAgentID = player2ID,
            };
            PlayerMessage askMessage = new PlayerMessage()
            {
                MessageID = PlayerMessageId.BegForInfo,
                AgentID = player1ID,
                Payload = begForInfoPayload.Serialize(),
            };

            await gameMaster.AcceptMessage(askMessage, CancellationToken.None);
        }

        [Fact]
        public async Task TestSendInformationExchangeRequesMessage()
        {
            var conf = GenerateConfiguration();
            var queue = GenerateBuffer();
            var client = GenerateSocketClient();
            var lifetime = Mock.Of<IApplicationLifetime>();

            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            gameMaster.Invoke("InitGame");

            int player1ID = 1;
            int player2ID = 2;
            int player3ID = 3;

            await InitializeAndBegForInfo(gameMaster, player1ID, player2ID);
            var lastMessage = sendedMessages.Pop();

            Assert.Equal(GMMessageId.InformationExchangeRequest, lastMessage.MessageID);
            Assert.True(JsonConvert.DeserializeObject<InformationExchangePayload>(lastMessage.Payload).WasSent);

            BegForInfoPayload begForInfoPayload = new BegForInfoPayload()
            {
                AskedAgentID = player3ID,
            };
            PlayerMessage askMessage = new PlayerMessage()
            {
                MessageID = PlayerMessageId.BegForInfo,
                AgentID = player1ID,
                Payload = begForInfoPayload.Serialize(),
            };

            await gameMaster.AcceptMessage(askMessage, CancellationToken.None);
            lastMessage = sendedMessages.Pop();

            Assert.Equal(GMMessageId.InformationExchangeRequest, lastMessage.MessageID);
            Assert.False(JsonConvert.DeserializeObject<InformationExchangePayload>(lastMessage.Payload).WasSent);
        }

        [Fact]
        public async Task TestSendInformationExchangeResponseMessage()
        {
            var conf = GenerateConfiguration();
            var queue = GenerateBuffer();
            var client = GenerateSocketClient();
            var lifetime = Mock.Of<IApplicationLifetime>();

            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            gameMaster.Invoke("InitGame");

            int player1ID = 1;
            int player2ID = 2;
            int player3ID = 3;

            await InitializeAndBegForInfo(gameMaster, player1ID, player2ID);

            GiveInfoPayload giveInfoPayload = new GiveInfoPayload()
            {
                RespondToId = player1ID,
                Distances = new int[,] { { 1, 1 } },
                RedTeamGoalAreaInformations = new GoalInfo[,] { { GoalInfo.IDK } },
                BlueTeamGoalAreaInformations = new GoalInfo[,] { { GoalInfo.IDK } },
            };
            PlayerMessage giveMessage1 = new PlayerMessage()
            {
                MessageID = PlayerMessageId.GiveInfo,
                AgentID = player2ID,
                Payload = giveInfoPayload.Serialize(),
            };

            PlayerMessage giveMessage2 = new PlayerMessage()
            {
                MessageID = PlayerMessageId.GiveInfo,
                AgentID = player3ID,
                Payload = giveInfoPayload.Serialize(),
            };

            await gameMaster.AcceptMessage(giveMessage1, CancellationToken.None);
            var lastMessage = sendedMessages.Pop();

            Assert.Equal(GMMessageId.InformationExchangeResponse, lastMessage.MessageID);
            Assert.True(JsonConvert.DeserializeObject<InformationExchangePayload>(lastMessage.Payload).WasSent);

            await gameMaster.AcceptMessage(giveMessage2, CancellationToken.None);
            lastMessage = sendedMessages.Pop();

            Assert.Equal(GMMessageId.InformationExchangeResponse, lastMessage.MessageID);
            Assert.False(JsonConvert.DeserializeObject<InformationExchangePayload>(lastMessage.Payload).WasSent);
        }
    }
}
