using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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
        private readonly int playerId = 1;
        private PlayerMessage lastSended;

        private class MockSocketClient<R, S> : ISocketClient<R, S>
        {
            private readonly Send send;

            public delegate void Send(S message);

            public MockSocketClient(Send send)
            {
                this.send = send;
            }

            public bool IsOpen => throw new NotImplementedException();

            public object GetSocket() => throw new NotImplementedException();

            public Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public Task CloseAsync(CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public Task<(bool, R)> ReceiveAsync(CancellationToken cancellationToken)
                 => throw new NotImplementedException();

            public async Task SendAsync(S message, CancellationToken cancellationToken)
            {
                send(message);
                await Task.CompletedTask;
            }

            public Task SendToAllAsync(List<S> messages, CancellationToken cancellationToken)
                => throw new NotImplementedException();
        }

        private MockSocketClient<GMMessage, PlayerMessage> GenerateSocketClient()
        {
            return new MockSocketClient<GMMessage, PlayerMessage>((m) => { lastSended = m; });
        }

        private PlayerConfiguration GenerateSampleConfiguration()
        {
            return new PlayerConfiguration() { CsIP = "192.168.0.0", CsPort = 3729, TeamId = "red", Strategy = 3 };
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
            GMMessage messageDiscover = new GMMessage(GMMessageId.DiscoverAnswer, playerId, payloadDiscover);
            GMMessage messageStart = CreateStartMessage();

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post(messageStart);
            input.Post(messageDiscover);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, input, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);

            Assert.Equal(0, player.Board[0, 0].DistToPiece);
        }

        [Fact]
        public async Task TestAcceptMessageBegForInfoAccept()
        {
            BegForInfoForwardedPayload payloadBeg = new BegForInfoForwardedPayload()
            {
                AskingId = 2,
                Leader = false,
                TeamId = Team.Red,
            };
            GMMessage messageBeg = new GMMessage(GMMessageId.BegForInfoForwarded, playerId, payloadBeg);
            GMMessage messageStart = CreateStartMessage();

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post(messageStart);
            input.Post(messageBeg);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, input, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

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
                Id = GMMessageId.CheckAnswer,
                Payload = JsonConvert.SerializeObject(payloadCheck),
            };
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post(startMessage);
            input.Post(messageCheck);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Models.Player(configuration, input,
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
            GMMessage destructionMessage = new GMMessage(GMMessageId.DestructionAnswer, playerId, destructionPayload);

            EmptyAnswerPayload pickPayload = new EmptyAnswerPayload();
            GMMessage pickMessage = new GMMessage(GMMessageId.PickAnswer, playerId, pickPayload);
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post(startMessage);
            input.Post(pickMessage);
            input.Post(destructionMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, input, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

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
            int playerId = 1;
            int leaderId = 1;
            Team teamId = Team.Red;
            int[] alliesId = new int[1] { 2 };
            int[] enemiesId = new int[2] { 3, 4 };
            BoardSize boardSize = new BoardSize { X = 3, Y = 3 };
            int goalAreaSize = 1;
            NumberOfPlayers numberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 };
            int numberOfPieces = 2;
            int numberOfGoals = 2;
            Penalties penalties = new Penalties();
            float shanProbability = 0.5f;
            Position position = new Position { X = 1, Y = 1 };

            // Arrange
            StartGamePayload startGamePayload = new StartGamePayload
            {
                PlayerId = playerId,
                AlliesIds = alliesId,
                LeaderId = leaderId,
                EnemiesIds = enemiesId,
                TeamId = teamId,
                BoardSize = boardSize,
                GoalAreaSize = goalAreaSize,
                NumberOfPlayers = numberOfPlayers,
                NumberOfPieces = numberOfPieces,
                NumberOfGoals = numberOfGoals,
                Penalties = penalties,
                ShamPieceProbability = shanProbability,
                Position = position,
            };
            GMMessage startMessage = new GMMessage(GMMessageId.StartGame, playerId, startGamePayload);

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post(startMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, input, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            // Act
            bool expectedisLeader = true;
            (int, int) expectedBoardSize = (boardSize.X, boardSize.Y);
            (int, int) expectedPosition = (position.X, position.Y);

            await player.AcceptMessage(CancellationToken.None);

            var playerIdResult = player.GetValue<Player.Models.Player, int>("id");
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
            Assert.Equal(playerId, playerIdResult);
            Assert.Equal(leaderId, leaderIdResult);
            Assert.Equal(alliesId, teamMatesResult);
            Assert.Equal(expectedisLeader, isLeaderResult);
            Assert.Equal(teamId, teamResult);
            Assert.Equal(expectedBoardSize, boardSizeResult);
            Assert.True(penalties.AreAllPropertiesTheSame(penaltiesResult));
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
            GMMessage moveAnswerMessage = new GMMessage(GMMessageId.MoveAnswer, playerId, moveAnswerPayload);
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post<GMMessage>(startMessage);
            input.Post<GMMessage>(moveAnswerMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Player.Models.Player(configuration, input, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

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
            GMMessage pickAnswerMessage = new GMMessage(GMMessageId.PickAnswer, playerId, pickPayload);
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post(startMessage);
            input.Post(pickAnswerMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Models.Player(configuration, input, new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

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
            var endGamePayload = new EndGamePayload()
            {
                Winner = Team.Red,
            };
            GMMessage endGameMessage = new GMMessage(GMMessageId.EndGame, playerId, endGamePayload);
            GMMessage startMessage = CreateStartMessage();

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post<GMMessage>(startMessage);
            input.Post<GMMessage>(endGameMessage);

            PlayerConfiguration configuration = GenerateSampleConfiguration();
            var player = new Models.Player(configuration, input,
                new TcpSocketClient<GMMessage, PlayerMessage>(logger), logger);

            // Act
            Team expectedWinner = Team.Red;

            await player.AcceptMessage(CancellationToken.None);
            await player.AcceptMessage(CancellationToken.None);
            var realWinner = player.GetValue<Player.Models.Player, Team>("winner");

            // Assert
            Assert.Equal(expectedWinner, realWinner);
        }

        [Fact]
        public async Task TestAcceptMessageBegForInfoForwardedShouldSendInfo()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload()
            {
                AskingId = 2,
                Leader = true,
                TeamId = Team.Red,
            };
            GMMessage beg4Info = new GMMessage(GMMessageId.BegForInfoForwarded, 1, payload);
            input.Post(beg4Info);
            await player.AcceptMessage(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.GiveInfo);
        }

        [Fact]
        public async Task TestAcceptMessageJoinTheGameShouldEndGame()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            JoinAnswerPayload payload = new JoinAnswerPayload()
            {
                Accepted = false,
                PlayerId = 1,
            };
            GMMessage messageStart = new GMMessage(GMMessageId.JoinTheGameAnswer, 1, payload);
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            bool expected = false;
            bool actual = player.GetValue<Player.Models.Player, bool>("working");
            Assert.Equal(expected, actual);
        }

        public GMMessage CreateStartMessage()
        {
            StartGamePayload payloadStart = new StartGamePayload
            {
                PlayerId = 1,
                AlliesIds = new int[1] { 2 },
                LeaderId = 1,
                EnemiesIds = new int[2] { 3, 4 },
                TeamId = Team.Red,
                BoardSize = new BoardSize { X = 3, Y = 3 },
                GoalAreaSize = 1,
                NumberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 },
                NumberOfPieces = 2,
                NumberOfGoals = 2,
                Penalties = new Penalties(),
                ShamPieceProbability = 0.5f,
                Position = new Position { X = 1, Y = 1 },
            };

            return new GMMessage(GMMessageId.StartGame, playerId, payloadStart);
        }

        [Fact]
        public async Task TestJoinTheGameReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            await player.JoinTheGame(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.JoinTheGame);
        }

        [Fact]
        public async Task TestMoveReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            await player.Move(Direction.N, CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.Move);
        }

        [Fact]
        public async Task TestPutReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            await player.Put(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.Put);
        }

        [Fact]
        public async Task TestBegForInfoReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            await player.BegForInfo(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.BegForInfo);
        }

        [Fact]
        public async Task TestGiveInfoReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload()
            {
                AskingId = 2,
                Leader = false,
                TeamId = Team.Red,
            };
            GMMessage beg4Info = new GMMessage(GMMessageId.BegForInfoForwarded, 1, payload);
            input.Post(beg4Info);
            await player.AcceptMessage(CancellationToken.None);
            await player.GiveInfo(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.GiveInfo);
        }

        [Fact]
        public async Task TestCheckPieceReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            await player.CheckPiece(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.CheckPiece);
        }

        [Fact]
        public async Task TestDiscoverReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            await player.Discover(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.Discover);
        }

        [Fact]
        public async Task TestDestroyPieceReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            await player.DestroyPiece(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.PieceDestruction);
        }

        [Fact]
        public async Task TestPickReturnsAppropriateMessage()
        {
            PlayerConfiguration configuration = GenerateSampleConfiguration();
            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            MockSocketClient<GMMessage, PlayerMessage> client = GenerateSocketClient();
            var player = new Models.Player(configuration, input, client, logger);

            GMMessage messageStart = CreateStartMessage();
            input.Post(messageStart);
            await player.AcceptMessage(CancellationToken.None);

            await player.Pick(CancellationToken.None);
            Assert.True(lastSended.MessageId == PlayerMessageId.Pick);
        }
    }
}
