using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Moq;
using Newtonsoft.Json;
using Player.Models;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads.GMPayloads;
using TestsShared;
using Xunit;

namespace Player.Tests
{
    public class PlayerTests
    {
        private readonly ILogger logger = MockGenerator.Get<ILogger>();
        private readonly int agentID = 1;
        private PlayerMessage lastSended;
        private readonly BoardSize playerBoardSize = new BoardSize { X = 3, Y = 3 };

        private ISocketClient<GMMessage, PlayerMessage> GenerateSocketClient()
        {
            var mock = new Mock<ISocketClient<GMMessage, PlayerMessage>>();
            mock.Setup(c => c.SendAsync(It.IsAny<PlayerMessage>(), It.IsAny<CancellationToken>())).
                Callback<PlayerMessage, CancellationToken>((m, c) => lastSended = m);
            return mock.Object;
        }

        private PlayerConfiguration GenerateSampleConfiguration()
        {
            return new PlayerConfiguration() { CsIP = "192.168.0.0", CsPort = 3729, TeamId = Team.Red, Strategy = 3 };
        }

        [Fact]
        public async Task TestAcceptMessageDiscoverAccept()
        {
            DiscoveryAnswerPayload payloadDiscover = new DiscoveryAnswerPayload()
            {
                DistanceFromCurrent = 0,
                DistanceE = 0,
                DistanceW = 0,
                DistanceS = 0,
                DistanceN = 0,
                DistanceNE = 0,
                DistanceSE = 0,
                DistanceNW = 0,
                DistanceSW = 0,
            };
            GMMessage messageDiscover = new GMMessage(GMMessageId.DiscoverAnswer, agentID, payloadDiscover);
            GMMessage messageStart = CreateStartMessage();

            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            inputBuffer.Post(messageStart);
            inputBuffer.Post(messageDiscover);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, inputBuffer, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);

            Assert.Equal(0, player.Board[0, 0].DistToPiece);
        }

        [Fact]
        public async Task TestAcceptMessageBegForInfoAccept()
        {
            BegForInfoForwardedPayload payloadBeg = new BegForInfoForwardedPayload()
            {
                AskingID = 2,
                Leader = false,
                TeamID = Team.Red,
            };
            GMMessage messageBeg = new GMMessage(GMMessageId.BegForInfoForwarded, agentID, payloadBeg);
            GMMessage messageStart = CreateStartMessage();

            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            inputBuffer.Post(messageStart);
            inputBuffer.Post(messageBeg);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, inputBuffer, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);

            Assert.Single(player.WaitingPlayers);
        }

        [Fact]
        public async Task TestAcceptMessageCheckAnswerShouldChangeIsHeldPieceShamToTrue()
        {
            // Arrange
            CheckAnswerPayload payloadCheck = new CheckAnswerPayload()
            {
                Sham = true,
            };
            GMMessage messageCheck = new GMMessage()
            {
                MessageID = GMMessageId.CheckAnswer,
                Payload = payloadCheck.Serialize(),
            };
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            inputBuffer.Post(startMessage);
            inputBuffer.Post(messageCheck);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Models.Player(configuration, inputBuffer,
                new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            // Act
            bool expectedResult = true;

            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);
            bool? result = player.IsHeldPieceSham;

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task TestAcceptMessageDestructionAnswerShouldDestroyHoldingPiece()
        {
            // Arrange
            EmptyAnswerPayload destructionPayload = new EmptyAnswerPayload();
            GMMessage destructionMessage = new GMMessage(GMMessageId.DestructionAnswer, agentID, destructionPayload);

            EmptyAnswerPayload pickPayload = new EmptyAnswerPayload();
            GMMessage pickMessage = new GMMessage(GMMessageId.PickAnswer, agentID, pickPayload);
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            inputBuffer.Post(startMessage);
            inputBuffer.Post(pickMessage);
            inputBuffer.Post(destructionMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, inputBuffer, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            // Act
            bool expectedHasPieceValue = false;

            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);
            bool realHasPieceValue = player.HasPiece;
            bool? realIsHeldPieceShamValue = player.IsHeldPieceSham;

            // Assert
            Assert.Equal(expectedHasPieceValue, realHasPieceValue);
            Assert.Null(realIsHeldPieceShamValue);
        }

        [Fact]
        public async Task TestAcceptMessageStartGameShouldSetFields()
        {
            int agentID = 1;
            int leaderId = 1;
            Team teamId = Team.Red;
            int[] alliesId = new int[1] { 2 };
            int[] enemiesId = new int[2] { 3, 4 };
            BoardSize boardSize = new BoardSize { X = 3, Y = 3 };
            int goalAreaSize = 1;
            NumberOfPlayers numberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 };
            int numberOfPieces = 2;
            int numberOfGoals = 2;
            Penalties penalties = new Penalties()
            {
                Move = 100,
                Ask = 100,
                Response = 100,
                Discover = 100,
                PickupPiece = 100,
                CheckPiece = 100,
                PutPiece = 100,
                DestroyPiece = 100,
            };
            float shanProbability = 0.5f;
            Position position = new Position { X = 1, Y = 1 };

            // Arrange
            StartGamePayload startGamePayload = new StartGamePayload
            {
                AgentID = agentID,
                AlliesIDs = alliesId,
                LeaderId = leaderId,
                EnemiesIDs = enemiesId,
                TeamID = teamId,
                BoardSize = boardSize,
                GoalAreaSize = goalAreaSize,
                NumberOfPlayers = numberOfPlayers,
                NumberOfPieces = numberOfPieces,
                NumberOfGoals = numberOfGoals,
                Penalties = penalties,
                ShamPieceProbability = shanProbability,
                Position = position
            };
            GMMessage startMessage = new GMMessage(GMMessageId.StartGame, agentID, startGamePayload);

            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            inputBuffer.Post(startMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, inputBuffer, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            // Act
            bool expectedisLeader = true;
            (int, int) expectedBoardSize = (boardSize.X, boardSize.Y);
            (int, int) expectedPosition = (position.X, position.Y);

            await player.AcceptMessage(CancellationToken.None);

            var agentIDResult = player.GetValue<Player.Models.Player, int>("id");
            var leaderIdResult = player.LeaderId;
            var teamMatesResult = player.TeamMatesIds;
            var isLeaderResult = player.IsLeader;
            var teamResult = player.Team;
            var boardSizeResult = player.BoardSize;
            var penaltiesResult = player.PenaltiesTimes;
            var positionResult = player.Position;
            var enemiesResult = player.EnemiesIds;
            var goalAreaSizeResult = player.GoalAreaSize;
            var numOfPlayersResult = player.NumberOfPlayers;
            var numOfPiecesResult = player.NumberOfPieces;
            var numOfGoalsResult = player.NumberOfGoals;
            var shamProbabilityResult = player.ShamPieceProbability;

            // Assert
            Assert.Equal(agentID, agentIDResult);
            Assert.Equal(leaderId, leaderIdResult);
            Assert.Equal(alliesId, teamMatesResult);
            Assert.Equal(expectedisLeader, isLeaderResult);
            Assert.Equal(teamId, teamResult);
            Assert.Equal(expectedBoardSize, boardSizeResult);
            Assert.True(penalties.AreAllPropertiesTheSame(penaltiesResult),
                $"Penalties should be the same,\n expected: {penalties},\n actual {penaltiesResult}");
            Assert.Equal(expectedPosition, positionResult);
            Assert.Equal(enemiesId, enemiesResult);
            Assert.Equal(goalAreaSize, goalAreaSizeResult);
            Assert.True(numberOfPlayers.Allies == numOfPlayersResult.Allies
                && numberOfPlayers.Enemies == numOfPlayersResult.Enemies);
            Assert.Equal(numberOfPieces, numOfPiecesResult);
            Assert.Equal(numberOfGoals, numOfGoalsResult);
            Assert.Equal(shanProbability, shamProbabilityResult);
        }

        [Fact]
        public async Task TestAcceptMessageMoveAnswerShouldChangePlayerPosition()
        {
            // Arrange
            Position newPosition = new Position()
            {
                X = 1,
                Y = 2,
            };
            int distToClosestPiece = 1;

            MoveAnswerPayload moveAnswerPayload = new MoveAnswerPayload()
            {
                MadeMove = true,
                CurrentPosition = newPosition,
                ClosestPiece = distToClosestPiece,
            };
            GMMessage moveAnswerMessage = new GMMessage(GMMessageId.MoveAnswer, agentID, moveAnswerPayload);
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            inputBuffer.Post<GMMessage>(startMessage);
            inputBuffer.Post<GMMessage>(moveAnswerMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, inputBuffer, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            // Act
            (int, int) expectedPosition = (newPosition.Y, newPosition.X);

            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);
            var realPosition = player.Position;
            var realDistToClosestPiece = player.Board[player.Position.y, player.Position.x].DistToPiece;

            // Assert
            Assert.Equal(expectedPosition, realPosition);
            Assert.Equal(distToClosestPiece, realDistToClosestPiece);
        }

        [Fact]
        public async Task TestAcceptMessagePickAnswerShouldChangeDistToPieceAndPickPiece()
        {
            EmptyAnswerPayload pickPayload = new EmptyAnswerPayload();
            GMMessage pickAnswerMessage = new GMMessage(GMMessageId.PickAnswer, agentID, pickPayload);
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            inputBuffer.Post(startMessage);
            inputBuffer.Post(pickAnswerMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Models.Player(configuration, inputBuffer, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            // Act
            bool expectedHasPieceValue = true;
            var expectedDistance = int.MaxValue;

            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);

            var realHasPieceValue = player.HasPiece;
            var realDistToClosestPiece = player.Board[player.Position.y, player.Position.x].DistToPiece;

            // Assert
            Assert.Equal(expectedHasPieceValue, realHasPieceValue);
            Assert.Equal(expectedDistance, realDistToClosestPiece);
        }

        [Fact]
        public async Task TestAcceptMessageEndGameShouldSetWinnerField()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            var player = new Models.Player(configuration, inputBuffer,
                new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            GMMessage startMessage = CreateStartMessage();
            inputBuffer.Post<GMMessage>(startMessage);
            await player.AcceptMessage(CancellationToken.None);

            var endGamePayload = new EndGamePayload()
            {
                Winner = Team.Red,
            };
            GMMessage endGameMessage = new GMMessage(GMMessageId.EndGame, agentID, endGamePayload);
            inputBuffer.Post<GMMessage>(endGameMessage);

            // Act
            await player.AcceptMessage(CancellationToken.None);

            // Assert
            var realWinner = player.GetValue<Player.Models.Player, Team>("winner");
            Assert.Equal(Team.Red, realWinner);
        }

        [Fact]
        public async Task TestAcceptMessageBegForInfoForwardedShouldSendInfo()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload()
            {
                AskingID = 2,
                Leader = true,
                TeamID = Team.Red,
            };
            GMMessage beg4Info = new GMMessage(GMMessageId.BegForInfoForwarded, 1, payload);

            // Act
            inputBuffer.Post(beg4Info);
            await player.AcceptMessage(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.GiveInfo, lastSended.MessageID);
        }

        [Fact]
        public async Task TestAcceptMessageJoinTheGameShouldEndGame()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            JoinAnswerPayload payload = new JoinAnswerPayload()
            {
                Accepted = false,
                AgentID = 1,
            };
            GMMessage messageStart = new GMMessage(GMMessageId.JoinTheGameAnswer, agentID, payload);

            // Act
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Assert
            bool isNowWorking = player.GetValue<Player.Models.Player, bool>("isWorking");
            Assert.False(isNowWorking);
        }

        [Fact]
        public async Task TestAcceptMessagePutAnswerShouldMarkFields()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            PutAnswerPayload payload = new PutAnswerPayload()
            {
                PutEvent = PutEvent.NormalOnGoalField,
            };
            GMMessage putAnswer = new GMMessage(GMMessageId.PutAnswer, agentID, payload);

            // Act
            inputBuffer.Post(putAnswer);
            await player.AcceptMessage(CancellationToken.None);

            // Assert
            GoalInfo actualGoalInfo = player.Board[player.Position.y, player.Position.x].GoalInfo;
            Assert.Equal(GoalInfo.DiscoveredGoal, actualGoalInfo);
        }

        [Fact]
        public async Task TestAcceptMessagePutAnswerShouldChangeHasPieceValue()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            GMMessage pickMessage = new GMMessage(GMMessageId.PickAnswer, agentID, new EmptyAnswerPayload());
            inputBuffer.Post(pickMessage);
            await player.AcceptMessage(CancellationToken.None);

            PutAnswerPayload payload = new PutAnswerPayload()
            {
                PutEvent = PutEvent.NormalOnGoalField,
            };
            GMMessage putAnswer = new GMMessage(GMMessageId.PutAnswer, agentID, payload);

            // Act
            inputBuffer.Post(putAnswer);
            await player.AcceptMessage(CancellationToken.None);

            // Assert
            bool hasPiece = player.HasPiece;
            Assert.False(hasPiece);
        }

        [Fact]
        public async Task TestAcceptMessageGiveInfoForwardedShouldChangeBoardInfo()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            int randomDistance1 = 7;
            int randomDistance2 = 0;
            Position randomPosition1 = new Position { X = 1, Y = 1 };
            Position randomPosition2 = new Position { X = 2, Y = 2 };
            Position randomPosition3 = new Position { X = 2, Y = 1 };

            int[,] distBoard = new int[playerBoardSize.Y, playerBoardSize.X];
            GoalInfo[,] infoBoard = new GoalInfo[playerBoardSize.Y, playerBoardSize.X];
            for (int i = 0; i < playerBoardSize.Y; i++)
            {
                for (int j = 0; j < playerBoardSize.X; j++)
                {
                    distBoard[i, j] = randomDistance1;
                    infoBoard[i, j] = GoalInfo.IDK;
                }
            }
            distBoard[randomPosition2.Y, randomPosition2.X] = randomDistance2;
            infoBoard[randomPosition3.Y, randomPosition3.X] = GoalInfo.DiscoveredGoal;
            GiveInfoForwardedPayload payload = new GiveInfoForwardedPayload()
            {
                Distances = distBoard,
                RedTeamGoalAreaInformations = infoBoard,
                BlueTeamGoalAreaInformations = infoBoard,
            };
            GMMessage giveFwInfoMessage = new GMMessage(GMMessageId.GiveInfoForwarded, agentID, payload);

            // Act
            inputBuffer.Post(giveFwInfoMessage);
            await player.AcceptMessage(CancellationToken.None);

            // Assert
            GoalInfo actualGoalInfo = player.Board[randomPosition3.Y, randomPosition3.X].GoalInfo;
            Assert.Equal(GoalInfo.DiscoveredGoal, actualGoalInfo);

            int actualDist1 = player.Board[randomPosition1.Y, randomPosition1.X].DistToPiece;
            Assert.Equal(randomDistance1, actualDist1);

            int actualDist2 = player.Board[randomPosition2.Y, randomPosition2.X].DistToPiece;
            Assert.Equal(randomDistance2, actualDist2);
        }

        [Fact]
        public async Task TestAcceptMessagePutErrorShouldExtendPenaltyTime()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            GMMessage errorMessage = new GMMessage(GMMessageId.PutError, agentID, new PutErrorPayload());

            // Act
            inputBuffer.Post(errorMessage);
            await player.AcceptMessage(CancellationToken.None);

            // Assert
            int expectedPenaltyTime = player.PenaltiesTimes.PutPiece;
            int actualPenaltyTime = player.GetValue<Player.Models.Player, int>("penaltyTime");
            Assert.Equal(expectedPenaltyTime, actualPenaltyTime);
        }

        [Fact]
        public async Task TestAcceptMessagePickErrorShouldExtendPenaltyTime()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            GMMessage errorMessage = new GMMessage(GMMessageId.PickError, agentID, new PutErrorPayload());

            // Act
            inputBuffer.Post(errorMessage);
            await player.AcceptMessage(CancellationToken.None);

            // Assert
            int expectedPenaltyTime = player.PenaltiesTimes.PickupPiece;
            int actualPenaltyTime = player.GetValue<Player.Models.Player, int>("penaltyTime");
            Assert.Equal(expectedPenaltyTime, actualPenaltyTime);
        }

        public GMMessage CreateStartMessage()
        {
            StartGamePayload payloadStart = new StartGamePayload
            {
                AgentID = 1,
                AlliesIDs = new int[1] { 2 },
                LeaderId = 1,
                EnemiesIDs = new int[2] { 3, 4 },
                TeamID = Team.Red,
                BoardSize = playerBoardSize,
                GoalAreaSize = 1,
                NumberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 },
                NumberOfPieces = 2,
                NumberOfGoals = 2,
                Penalties = new Penalties(),
                ShamPieceProbability = 0.5f,
                Position = new Position { X = 1, Y = 1 },
            };

            return new GMMessage(GMMessageId.StartGame, agentID, payloadStart);
        }

        [Fact]
        public async Task TestJoinTheGameReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.JoinTheGame(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.JoinTheGame, lastSended.MessageID);
        }

        [Fact]
        public async Task TestMoveReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.Move(Direction.N, CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.Move, lastSended.MessageID);
        }

        [Fact]
        public async Task TestPutReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.Put(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.Put, lastSended.MessageID);
        }

        [Fact]
        public async Task TestBegForInfoReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.BegForInfo(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.BegForInfo, lastSended.MessageID);
        }

        [Fact]
        public async Task TestGiveInfoReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload()
            {
                AskingID = 2,
                Leader = false,
                TeamID = Team.Red,
            };
            GMMessage beg4Info = new GMMessage(GMMessageId.BegForInfoForwarded, agentID, payload);
            inputBuffer.Post(beg4Info);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.GiveInfo(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.GiveInfo, lastSended.MessageID);
        }

        [Fact]
        public async Task TestCheckPieceReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.CheckPiece(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.CheckPiece, lastSended.MessageID);
        }

        [Fact]
        public async Task TestDiscoverReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.Discover(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.Discover, lastSended.MessageID);
        }

        [Fact]
        public async Task TestDestroyPieceReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.DestroyPiece(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.PieceDestruction, lastSended.MessageID);
        }

        [Fact]
        public async Task TestPickReturnsAppropriateMessage()
        {
            // Arrange
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> inputBuffer = new BufferBlock<GMMessage>();
            ISocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, inputBuffer, client, logger);

            GMMessage messageStart = CreateStartMessage();
            inputBuffer.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            // Act
            await player.Pick(CancellationToken.None);

            // Assert
            Assert.Equal(PlayerMessageId.Pick, lastSended.MessageID);
        }
    }
}
