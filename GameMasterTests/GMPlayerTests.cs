using System;
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
        private GMMessage lastSended;
    
        private ISocketClient<PlayerMessage, GMMessage> GenerateSocketClient()
        {
            var mock = new Mock<ISocketClient<PlayerMessage, GMMessage>>();
            mock.Setup(c => c.SendAsync(It.IsAny<GMMessage>(), It.IsAny<CancellationToken>())).
                Callback<GMMessage, CancellationToken>((m, c) => lastSended = m);
            return mock.Object;
        }

        private GameConfiguration GenerateConfiguration()
        {
            return new MockGameConfiguration();
        }

        private BufferBlock<PlayerMessage> GenerateBuffer()
        {
            return new BufferBlock<PlayerMessage>();
        }

        private GMPlayer GenerateGMPlayer(GameConfiguration conf, ISocketClient<PlayerMessage, GMMessage> socketClient,
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
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            var lifetime = Mock.Of<IApplicationLifetime>();
            var gameMaster = new GM(lifetime, conf, queue, client, logger);
            gameMaster.Invoke("InitGame");
            gameMaster.Invoke("GeneratePiece");
            return gameMaster;
        }

        [Fact]
        public async Task TestTryLock()
        {
            var player = GenerateGMPlayer();
            int delay = 100;
            var lockSpan = TimeSpan.FromMilliseconds(delay);
            Assert.True(player.TryLock(lockSpan));
            Assert.False(player.TryLock(lockSpan));
            await Task.Delay(delay * 2);
            Assert.True(player.TryLock(lockSpan));
        }

        [Fact]
        public async Task TestMoveAsyncToEmpty()
        {
            var gm = GenerateGM();
            var player = GenerateGMPlayer();
            var playerStartField = new TaskField(0, 0);
            var playerEndField = new TaskField(0, 1);
            Assert.True(playerStartField.MoveHere(player));

            lastSended = null;
            bool moved = await player.MoveAsync(playerEndField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(playerEndField, player.Position);
            Assert.Equal(GMMessageId.MoveAnswer, lastSended.MessageID);

            var payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(lastSended.Payload);
            Assert.True(payload.MadeMove);
            Position expectedPos = playerEndField.GetPosition();
            Assert.True(payload.CurrentPosition.Equals(expectedPos));
        }

        [Fact]
        public async Task TestMoveAsyncToFull()
        {
            var gm = GenerateGM();
            var firstPlayer = GenerateGMPlayer();
            var firstPlayerField = new TaskField(0, 0);
            Assert.True(firstPlayerField.MoveHere(firstPlayer));

            var secondPlayer = GenerateGMPlayer();
            var secondPlayerField = new TaskField(0, 1);
            Assert.True(secondPlayerField.MoveHere(secondPlayer));

            lastSended = null;
            bool moved = await firstPlayer.MoveAsync(secondPlayerField, gm, CancellationToken.None);
            Assert.False(moved);
            Assert.Equal(firstPlayerField, firstPlayer.Position);
            Assert.Equal(secondPlayerField, secondPlayer.Position);
            Assert.Equal(GMMessageId.MoveAnswer, lastSended.MessageID);

            var payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(lastSended.Payload);
            Assert.False(payload.MadeMove);
            Position expectedPos = firstPlayerField.GetPosition();
            Assert.True(payload.CurrentPosition.Equals(expectedPos));
        }

        [Fact]
        public async Task TestMoveAsyncToRelease()
        {
            var gm = GenerateGM();
            var firstPlayer = GenerateGMPlayer();
            var firstPlayerField = new TaskField(0, 0);
            Assert.True(firstPlayerField.MoveHere(firstPlayer));

            var secondPlayer = GenerateGMPlayer();
            var secondPlayerField = new TaskField(0, 1);
            var secondPlayerEndField = new TaskField(1, 1);
            Assert.True(secondPlayerField.MoveHere(secondPlayer));

            lastSended = null;
            bool moved = await secondPlayer.MoveAsync(secondPlayerEndField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(GMMessageId.MoveAnswer, lastSended.MessageID);
            var payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(lastSended.Payload);
            Assert.True(payload.MadeMove);

            lastSended = null;
            moved = await firstPlayer.MoveAsync(secondPlayerField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(GMMessageId.MoveAnswer, lastSended.MessageID);
            payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(lastSended.Payload);
            Assert.True(payload.MadeMove);
        }

        [Fact]
        public async Task TestMoveAsyncActivePenalty()
        {
            var gm = GenerateGM();
            var player = GenerateGMPlayer();
            var startField = new TaskField(0, 0);
            var firstField = new TaskField(0, 1);
            var secondField = new TaskField(1, 1);
            Assert.True(startField.MoveHere(player));

            lastSended = null;
            var moved = await player.MoveAsync(firstField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(GMMessageId.MoveAnswer, lastSended.MessageID);

            lastSended = null;
            moved = await player.MoveAsync(secondField, gm, CancellationToken.None);
            Assert.False(moved);
            Assert.Equal(GMMessageId.NotWaitedError, lastSended.MessageID);
        }

        [Fact]
        public async Task TestMoveAsyncAfterPenalty()
        {
            var gm = GenerateGM();
            var conf = GenerateConfiguration();
            var socketClient = GenerateSocketClient();
            var player = GenerateGMPlayer(conf, socketClient);
            var startField = new TaskField(0, 0);
            var firstField = new TaskField(0, 1);
            var secondField = new TaskField(1, 1);
            Assert.True(startField.MoveHere(player));

            lastSended = null;
            var moved = await player.MoveAsync(firstField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(GMMessageId.MoveAnswer, lastSended.MessageID);

            await Task.Delay(conf.MovePenalty * 2);
            lastSended = null;
            moved = await player.MoveAsync(secondField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.Equal(GMMessageId.MoveAnswer, lastSended.MessageID);
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

            lastSended = null;
            bool detroyed = await player.DestroyHoldingAsync(CancellationToken.None);
            Assert.True(detroyed);
            Assert.True(player.Holding is null);
            Assert.Equal(GMMessageId.DestructionAnswer, lastSended.MessageID);

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

            lastSended = null;
            await player.CheckHoldingAsync(CancellationToken.None);
            Assert.True(player.Holding is null);

            await Task.Delay(conf.CheckPenalty * 2);
            player.Holding = piece;
            lastSended = null;
            await player.CheckHoldingAsync(CancellationToken.None);
            Assert.Equal(GMMessageId.CheckAnswer, lastSended.MessageID);
        }

        [Fact]
        public async Task TestDiscoverAsync()
        {
            var gm = GenerateGM();
            var player = GenerateGMPlayer();
            var field = new TaskField(2, 2);
            Assert.True(field.MoveHere(player));

            lastSended = null;
            await player.DiscoverAsync(gm, CancellationToken.None);
            Assert.Equal(GMMessageId.DiscoverAnswer, lastSended.MessageID);
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

            lastSended = null;
            (bool? goal, bool removed) = await player.PutAsync(CancellationToken.None);
            Assert.True(goal == true && removed);
            Assert.True(player.Holding is null);
            Assert.Equal(GMMessageId.PutAnswer, lastSended.MessageID);

            await Task.Delay(conf.PutPenalty * 2);
            lastSended = null;
            (goal, removed) = await player.PutAsync(CancellationToken.None);
            Assert.False(goal == true || removed);
            Assert.Equal(GMMessageId.PutError, lastSended.MessageID);
            var payload = JsonConvert.DeserializeObject<PutErrorPayload>(lastSended.Payload);
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

            lastSended = null;
            bool picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.True(player.Holding is null);
            Assert.Equal(GMMessageId.PickError, lastSended.MessageID);
            var payload = JsonConvert.DeserializeObject<PickErrorPayload>(lastSended.Payload);
            Assert.Equal(PickError.NothingThere, payload.ErrorSubtype);

            // delay
            await Task.Delay(conf.PickUpPenalty * 2);
            field.Put(piece);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.True(picked);
            Assert.Equal(piece, player.Holding);
            Assert.Equal(GMMessageId.PickAnswer, lastSended.MessageID);

            // delay
            await Task.Delay(conf.PickUpPenalty * 2);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.Equal(piece, player.Holding);
            Assert.Equal(GMMessageId.PickError, lastSended.MessageID);
            payload = JsonConvert.DeserializeObject<PickErrorPayload>(lastSended.Payload);
            Assert.Equal(PickError.Other, payload.ErrorSubtype);

            // delay
            await Task.Delay(conf.PickUpPenalty * 2);
            var secondPiece = new NormalPiece();
            field.Put(secondPiece);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.Equal(piece, player.Holding);
            Assert.Equal(GMMessageId.PickError, lastSended.MessageID);
            payload = JsonConvert.DeserializeObject<PickErrorPayload>(lastSended.Payload);
            Assert.Equal(PickError.Other, payload.ErrorSubtype);
        }
    }
}
