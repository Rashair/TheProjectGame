using System;
using System.Collections.Generic;
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
using Shared.Models;
using Shared.Payloads.GMPayloads;
using TestsShared;
using Xunit;

namespace GameMaster.Tests
{
    public partial class GMPlayerTests
    {
        private const int DefaultId = 15;
        private const Team DefaultTeam = Team.Blue;
        private const bool DefaultIsLeader = false;

        private readonly ILogger logger = MockGenerator.Get<ILogger>();
        private Message prevSended;
        private Message lastSended;

        public GMPlayerTests()
        {
            lastSended = null;
            prevSended = null;
        }

        private ISocketClient<Message, Message> GenerateSocketClient()
        {
            var mock = new Mock<ISocketClient<Message, Message>>();
            mock.Setup(c => c.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>())).
                Callback<Message, CancellationToken>((m, c) =>
                {
                    prevSended = lastSended;
                    lastSended = m;
                });
            return mock.Object;
        }

        private GameConfiguration GenerateConfiguration()
        {
            return new MockGameConfiguration();
        }

        private BufferBlock<Message> GenerateBuffer()
        {
            return new BufferBlock<Message>();
        }

        private GMPlayer GenerateGMPlayer(GameConfiguration conf, ISocketClient<Message, Message> socketClient,
            int id = DefaultId, Team team = DefaultTeam, bool isLeader = DefaultIsLeader)
        {
            return new GMPlayer(id, conf, socketClient, team, logger, isLeader);
        }

        private GMPlayer GenerateGMPlayer(int id = DefaultId, Team team = DefaultTeam, bool isLeader = DefaultIsLeader)
        {
            return GenerateGMPlayer(GenerateConfiguration(), GenerateSocketClient(), id, team, isLeader);
        }

        private GM GenerateGM()
        {
            var conf = GenerateConfiguration();
            var queue = GenerateBuffer();
            var client = new TcpSocketClient<Message, Message>(logger);
            var lifetime = Mock.Of<IApplicationLifetime>();
            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            gameMaster.Invoke("InitGame");
            gameMaster.Invoke("GeneratePiece");
            return gameMaster;
        }

        public int? InvokeGetClosestPiece(GM gm, AbstractField pos)
        {
            return gm.Invoke<GM, int?>("FindClosestPiece", new object[] { pos });
        }

        public Dictionary<Direction, int?> InvokeDiscover(GM gm, AbstractField pos)
        {
            return gm.Invoke<GM, Dictionary<Direction, int?>>("Discover", new object[] { pos });
        }

        [Fact]
        public async Task TestLock()
        {
            // Arrange
            var token = CancellationToken.None;
            var player = GenerateGMPlayer();
            int delay = 100;

            // TODO: Change from conf
            int prematureRequestPenalty = 200;

            // Act
            player.Invoke("Lock", delay, DateTime.Now);

            // Assert
            bool gotLock = await player.TryGetLockAsync(token);
            Assert.False(gotLock);

            await Task.Delay((int)(delay * 1.5) + prematureRequestPenalty);

            gotLock = await player.TryGetLockAsync(token);
            Assert.True(gotLock);
        }

        [Fact]
        public async Task TestMoveAsyncToEmpty()
        {
            var gm = GenerateGM();
            Func<AbstractField, int?> getClosestPiece =
    (AbstractField pos) => gm.Invoke<GM, int?>("FindClosestPiece", new object[] { pos });

            var player = GenerateGMPlayer();
            var playerStartField = new TaskField(0, 0);
            var playerEndField = new TaskField(0, 1);
            Assert.True(playerStartField.MoveHere(player));

            bool moved = await player.MoveAsync(playerEndField, getClosestPiece, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(playerEndField, player.Position);
            Assert.Equal(MessageID.MoveAnswer, lastSended.MessageID);
            Assert.Null(prevSended);

            var payload = (MoveAnswerPayload)lastSended.Payload;
            Assert.True(payload.MadeMove);
            Position expectedPos = playerEndField.GetPosition();
            Assert.True(payload.CurrentPosition.Equals(expectedPos));
        }

        [Fact]
        public async Task TestMoveAsyncToFull()
        {
            var gm = GenerateGM();
            Func<AbstractField, int?> getClosestPiece = (AbstractField pos) => InvokeGetClosestPiece(gm, pos);
            var firstPlayer = GenerateGMPlayer();
            var firstPlayerField = new TaskField(0, 0);
            Assert.True(firstPlayerField.MoveHere(firstPlayer));

            var secondPlayer = GenerateGMPlayer();
            var secondPlayerField = new TaskField(0, 1);
            Assert.True(secondPlayerField.MoveHere(secondPlayer));

            // Act
            bool moved = await firstPlayer.MoveAsync(secondPlayerField, getClosestPiece, CancellationToken.None);
            Assert.False(moved);
            Assert.Equal(firstPlayerField, firstPlayer.Position);
            Assert.Equal(secondPlayerField, secondPlayer.Position);
            Assert.Equal(MessageID.MoveAnswer, lastSended.MessageID);
            Assert.Null(prevSended);

            var payload = (MoveAnswerPayload)lastSended.Payload;
            Assert.False(payload.MadeMove);
            Position expectedPos = firstPlayerField.GetPosition();
            Assert.True(payload.CurrentPosition.Equals(expectedPos));
        }

        [Fact]
        public async Task TestMoveAsyncToRelease()
        {
            var gm = GenerateGM();
            Func<AbstractField, int?> getClosestPiece = (AbstractField pos) => InvokeGetClosestPiece(gm, pos);
            var firstPlayer = GenerateGMPlayer();
            var firstPlayerField = new TaskField(0, 0);
            Assert.True(firstPlayerField.MoveHere(firstPlayer));

            var secondPlayer = GenerateGMPlayer();
            var secondPlayerField = new TaskField(0, 1);
            var secondPlayerEndField = new TaskField(1, 1);
            Assert.True(secondPlayerField.MoveHere(secondPlayer));

            // Act
            bool moved = await secondPlayer.MoveAsync(secondPlayerEndField, getClosestPiece, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(MessageID.MoveAnswer, lastSended.MessageID);
            var payload = (MoveAnswerPayload)lastSended.Payload;
            Assert.True(payload.MadeMove);
            Assert.Null(prevSended);

            lastSended = null;
            moved = await firstPlayer.MoveAsync(secondPlayerField, getClosestPiece, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(MessageID.MoveAnswer, lastSended.MessageID);
            payload = (MoveAnswerPayload)lastSended.Payload;
            Assert.True(payload.MadeMove);
        }

        [Fact]
        public async Task TestMoveAsyncActivePenalty()
        {
            var gm = GenerateGM();
            Func<AbstractField, int?> getClosestPiece = (AbstractField pos) => InvokeGetClosestPiece(gm, pos);
            var player = GenerateGMPlayer();
            var startField = new TaskField(0, 0);
            var firstField = new TaskField(0, 1);
            var secondField = new TaskField(1, 1);
            Assert.True(startField.MoveHere(player));

            // Act
            var moved = await player.MoveAsync(firstField, getClosestPiece, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(MessageID.MoveAnswer, lastSended.MessageID);
            Assert.Null(prevSended);

            lastSended = null;
            moved = await player.MoveAsync(secondField, getClosestPiece, CancellationToken.None);
            Assert.False(moved);
            Assert.Equal(MessageID.NotWaitedError, lastSended.MessageID);
        }

        [Fact]
        public async Task TestMoveAsyncAfterPenalty()
        {
            var gm = GenerateGM();
            Func<AbstractField, int?> getClosestPiece = (AbstractField pos) => InvokeGetClosestPiece(gm, pos);
            var conf = GenerateConfiguration();
            var socketClient = GenerateSocketClient();
            var player = GenerateGMPlayer(conf, socketClient);
            var startField = new TaskField(0, 0);
            var firstField = new TaskField(0, 1);
            var secondField = new TaskField(1, 1);
            Assert.True(startField.MoveHere(player));

            // Act
            var moved = await player.MoveAsync(firstField, getClosestPiece, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(MessageID.MoveAnswer, lastSended.MessageID);
            Assert.Null(prevSended);

            await Task.Delay(conf.MovePenalty * 2);
            lastSended = null;
            moved = await player.MoveAsync(secondField, getClosestPiece, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(MessageID.MoveAnswer, lastSended.MessageID);
        }

        [Fact]
        public async Task TestDestroyAsync()
        {
            var conf = GenerateConfiguration();
            var socketClient = GenerateSocketClient();
            var player = GenerateGMPlayer(conf, socketClient);
            var piece = new ShamPiece();
            player.Holding = piece;
            var field = new TaskField(0, 0);
            Assert.True(field.MoveHere(player));

            // Act
            bool detroyed = await player.DestroyHoldingAsync(CancellationToken.None);
            Assert.True(detroyed);
            Assert.True(player.Holding is null);
            Assert.Equal(MessageID.DestructionAnswer, lastSended.MessageID);
            Assert.Null(prevSended);

            // delay
            await Task.Delay(conf.DestroyPenalty * 2);
            lastSended = null;
            detroyed = await player.DestroyHoldingAsync(CancellationToken.None);
            Assert.False(detroyed);
        }

        [Fact]
        public async Task TestCheckHoldingAsync()
        {
            var conf = GenerateConfiguration();
            var socketClient = GenerateSocketClient();
            var player = GenerateGMPlayer(conf, socketClient);
            var piece = new ShamPiece();
            var field = new TaskField(0, 0);
            Assert.True(field.MoveHere(player));
            Assert.True(player.Holding is null);

            // Act
            await player.CheckHoldingAsync(CancellationToken.None);
            Assert.True(player.Holding is null);
            Assert.Null(prevSended);

            await Task.Delay(conf.CheckForShamPenalty * 2);
            player.Holding = piece;
            lastSended = null;
            await player.CheckHoldingAsync(CancellationToken.None);
            Assert.Equal(MessageID.CheckAnswer, lastSended.MessageID);
        }

        [Fact]
        public async Task TestDiscoverAsync()
        {
            var gm = GenerateGM();
            Func<AbstractField, Dictionary<Direction, int?>> discover = (AbstractField pos) => InvokeDiscover(gm, pos);
            var player = GenerateGMPlayer();
            var field = new TaskField(2, 2);
            Assert.True(field.MoveHere(player));

            // Act
            await player.DiscoverAsync(discover, CancellationToken.None);
            Assert.Equal(MessageID.DiscoverAnswer, lastSended.MessageID);
            Assert.Null(prevSended);
        }

        [Fact]
        public async Task TestPutAsync()
        {
            var conf = GenerateConfiguration();
            var socketClient = GenerateSocketClient();
            var player = GenerateGMPlayer(conf, socketClient);
            var piece = new NormalPiece();
            var field = new GoalField(0, 0);
            player.Holding = piece;
            Assert.True(field.MoveHere(player));

            // Act
            (PutEvent putEvent, bool removed) = await player.PutAsync(CancellationToken.None);
            Assert.True(putEvent == PutEvent.NormalOnGoalField && removed);
            Assert.True(player.Holding is null);
            Assert.Equal(MessageID.PutAnswer, lastSended.MessageID);
            Assert.Null(prevSended);

            await Task.Delay(conf.PutPenalty * 2);
            lastSended = null;
            (putEvent, removed) = await player.PutAsync(CancellationToken.None);
            Assert.False(putEvent == PutEvent.NormalOnGoalField || removed);
            Assert.Equal(MessageID.PutError, lastSended.MessageID);
            var payload = (PutErrorPayload)lastSended.Payload;
            Assert.Equal(PutError.AgentNotHolding, payload.ErrorSubtype);
        }

        [Fact]
        public async Task TestPickAsync()
        {
            var conf = GenerateConfiguration();
            var socketClient = GenerateSocketClient();
            var player = GenerateGMPlayer(conf, socketClient);
            var piece = new ShamPiece();
            var field = new TaskField(0, 0);
            Assert.True(field.MoveHere(player));
            Assert.True(player.Holding is null);

            // Act
            bool picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.True(player.Holding is null);
            Assert.Equal(MessageID.PickError, lastSended.MessageID);
            var payload = (PickErrorPayload)lastSended.Payload;
            Assert.Equal(PickError.NothingThere, payload.ErrorSubtype);
            Assert.Null(prevSended);

            // delay
            await Task.Delay(conf.PickupPenalty * 2);
            field.Put(piece);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.True(picked);
            Assert.Equal(piece, player.Holding);
            Assert.Equal(MessageID.PickAnswer, lastSended.MessageID);

            // delay
            await Task.Delay(conf.PickupPenalty * 2);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.Equal(piece, player.Holding);
            Assert.Equal(MessageID.PickError, lastSended.MessageID);
            payload = (PickErrorPayload)lastSended.Payload;
            Assert.Equal(PickError.Other, payload.ErrorSubtype);

            // delay
            await Task.Delay(conf.PickupPenalty * 2);
            var secondPiece = new NormalPiece();
            field.Put(secondPiece);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.Equal(piece, player.Holding);
            Assert.Equal(MessageID.PickError, lastSended.MessageID);
            payload = (PickErrorPayload)lastSended.Payload;
            Assert.Equal(PickError.Other, payload.ErrorSubtype);
        }
    }
}
