using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using GameMaster.Models;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using GameMaster.Tests.Mocks;
using Microsoft.Extensions.Hosting;
using Moq;
using Newtonsoft.Json;

using Shared.Enums;
using Shared.Messages;
using Shared.Payloads;
using TestsShared;
using Xunit;

namespace GameMaster.Tests
{
    public class GMPlayerTests
    {
        private const int DefaultId = 15;
        private const Team DefaultTeam = Team.Blue;
        private const bool DefaultIsLeader = false;

        private GMMessage lastSended;

        private class MockSocketManager : ISocketManager<TcpClient, GMMessage>
        {
            private readonly Send send;

            public delegate void Send(GMMessage message);

            public MockSocketManager(Send send)
            {
                this.send = send;
            }

            public bool AddSocket(TcpClient socket) => throw new NotImplementedException();

            public int GetId(TcpClient socket) => throw new NotImplementedException();

            public TcpClient GetSocketById(int id) => throw new NotImplementedException();

            public Task<bool> RemoveSocketAsync(int id, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public async Task SendMessageAsync(int id, GMMessage message, CancellationToken cancellationToken)
            {
                send(message);
                await Task.CompletedTask;
            }

            public Task SendMessageToAllAsync(GMMessage message, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public bool IsAnyOpen()
            {
                return true;
            }
        }

        private ISocketManager<TcpClient, GMMessage> GenerateSocketManager()
        {
            return new MockSocketManager((m) => { lastSended = m; });
        }

        private GameConfiguration GenerateConfiguration()
        {
            return new GameConfiguration
            {
                AskPenalty = 100,
                CheckPenalty = 100,
                DiscoverPenalty = 100,
                ResponsePenalty = 100,
                PutPenalty = 100,
                MovePenalty = 100,
            };
        }

        private BufferBlock<PlayerMessage> GenerateBuffer()
        {
            return new BufferBlock<PlayerMessage>();
        }

        private GMPlayer GenerateGMPlayer(GameConfiguration conf, ISocketManager<TcpClient, GMMessage> socketManager,
            int id = DefaultId, Team team = DefaultTeam, bool isLeader = DefaultIsLeader)
        {
            return new GMPlayer(id, conf, socketManager, team, isLeader);
        }

        private GMPlayer GenerateGMPlayer(int id = DefaultId, Team team = DefaultTeam, bool isLeader = DefaultIsLeader)
        {
            return GenerateGMPlayer(GenerateConfiguration(), GenerateSocketManager(), id, team, isLeader);
        }

        private GM GenerateGM()
        {
            var conf = new MockGameConfiguration();
            var queue = GenerateBuffer();
            var manager = new TcpSocketManager<GMMessage>();
            var lifetime = Mock.Of<IApplicationLifetime>();
            var gameMaster = new GM(lifetime, conf, queue, manager);
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
            Assert.True(player.Position == playerEndField);
            Assert.True(lastSended.Id == GMMessageID.MoveAnswer);

            var payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(lastSended.Payload);
            Assert.True(payload.MadeMove);
            Assert.True(payload.CurrentPosition.X == playerEndField.GetPositionObject().X);
            Assert.True(payload.CurrentPosition.Y == playerEndField.GetPositionObject().Y);
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
            Assert.True(firstPlayer.Position == firstPlayerField);
            Assert.True(secondPlayer.Position == secondPlayerField);
            Assert.True(lastSended.Id == GMMessageID.MoveAnswer);

            var payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(lastSended.Payload);
            Assert.False(payload.MadeMove);
            Assert.True(payload.CurrentPosition.X == firstPlayerField.GetPositionObject().X);
            Assert.True(payload.CurrentPosition.Y == firstPlayerField.GetPositionObject().Y);
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
            Assert.True(lastSended.Id == GMMessageID.MoveAnswer);
            var payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(lastSended.Payload);
            Assert.True(payload.MadeMove);

            lastSended = null;
            moved = await firstPlayer.MoveAsync(secondPlayerField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.True(lastSended.Id == GMMessageID.MoveAnswer);
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
            Assert.True(lastSended.Id == GMMessageID.MoveAnswer);

            lastSended = null;
            moved = await player.MoveAsync(secondField, gm, CancellationToken.None);
            Assert.False(moved);
            Assert.True(lastSended.Id == GMMessageID.NotWaitedError);
        }

        [Fact]
        public async Task TestMoveAsyncAfterPenalty()
        {
            var gm = GenerateGM();
            var conf = GenerateConfiguration();
            var socketManager = GenerateSocketManager();
            var player = GenerateGMPlayer(conf, socketManager);
            var startField = new TaskField(0, 0);
            var firstField = new TaskField(0, 1);
            var secondField = new TaskField(1, 1);
            Assert.True(startField.MoveHere(player));

            lastSended = null;
            var moved = await player.MoveAsync(firstField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.True(lastSended.Id == GMMessageID.MoveAnswer);

            await Task.Delay(conf.MovePenalty * 2);
            lastSended = null;
            moved = await player.MoveAsync(secondField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.True(lastSended.Id == GMMessageID.MoveAnswer);
        }

        [Fact]
        public async Task TestDestroyAsync()
        {
            var conf = GenerateConfiguration();
            var socketManager = GenerateSocketManager();
            var player = GenerateGMPlayer(conf, socketManager);
            var piece = new ShamPiece();
            player.Holding = piece;
            var field = new TaskField(0, 0);
            Assert.True(field.MoveHere(player));

            lastSended = null;
            bool detroyed = await player.DestroyHoldingAsync(CancellationToken.None);
            Assert.True(detroyed);
            Assert.True(player.Holding is null);
            Assert.True(lastSended.Id == GMMessageID.DestructionAnswer);

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
            var socketManager = GenerateSocketManager();
            var player = GenerateGMPlayer(conf, socketManager);
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
            Assert.True(lastSended.Id == GMMessageID.CheckAnswer);
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
            Assert.True(lastSended.Id == GMMessageID.DiscoverAnswer);
        }

        [Fact]
        public async Task TestPutAsync()
        {
            var conf = GenerateConfiguration();
            var socketManager = GenerateSocketManager();
            var player = GenerateGMPlayer(conf, socketManager);
            var piece = new NormalPiece();
            var field = new GoalField(0, 0);
            player.Holding = piece;
            Assert.True(field.MoveHere(player));

            lastSended = null;
            (bool goal, bool removed) = await player.PutAsync(CancellationToken.None);
            Assert.True(goal && removed);
            Assert.True(player.Holding is null);
            Assert.True(lastSended.Id == GMMessageID.PutAnswer);

            await Task.Delay(conf.PutPenalty * 2);
            lastSended = null;
            (goal, removed) = await player.PutAsync(CancellationToken.None);
            Assert.False(goal || removed);
            Assert.True(lastSended.Id == GMMessageID.PutError);
            var payload = JsonConvert.DeserializeObject<PutErrorPayload>(lastSended.Payload);
            Assert.True(payload.ErrorSubtype == PutError.AgentNotHolding);
        }

        [Fact]
        public async Task TestPickAsync()
        {
            var conf = GenerateConfiguration();
            var socketManager = GenerateSocketManager();
            var player = GenerateGMPlayer(conf, socketManager);
            var piece = new ShamPiece();
            var field = new TaskField(0, 0);
            Assert.True(field.MoveHere(player));
            Assert.True(player.Holding is null);

            lastSended = null;
            bool picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.True(player.Holding is null);
            Assert.True(lastSended.Id == GMMessageID.PickError);
            var payload = JsonConvert.DeserializeObject<PickErrorPayload>(lastSended.Payload);
            Assert.True(payload.ErrorSubtype == PickError.NothingThere);

            // delay
            await Task.Delay(conf.PickPenalty * 2);
            field.Put(piece);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.True(picked);
            Assert.True(player.Holding == piece);
            Assert.True(lastSended.Id == GMMessageID.PickAnswer);

            // delay
            await Task.Delay(conf.PickPenalty * 2);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.True(player.Holding == piece);
            Assert.True(lastSended.Id == GMMessageID.PickError);
            payload = JsonConvert.DeserializeObject<PickErrorPayload>(lastSended.Payload);
            Assert.True(payload.ErrorSubtype == PickError.Other);

            // delay
            await Task.Delay(conf.PickPenalty * 2);
            var secondPiece = new NormalPiece();
            field.Put(secondPiece);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.True(player.Holding == piece);
            Assert.True(lastSended.Id == GMMessageID.PickError);
            payload = JsonConvert.DeserializeObject<PickErrorPayload>(lastSended.Payload);
            Assert.True(payload.ErrorSubtype == PickError.Other);
        }
    }
}
