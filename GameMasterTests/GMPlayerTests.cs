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
using Newtonsoft.Json;
using Serilog;
using Shared.Clients;
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

        private readonly ILogger logger = MockGenerator.Get<ILogger>();
        private GMMessage lastSended;

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

        private MockSocketClient<PlayerMessage, GMMessage> GenerateSocketClient()
        {
            return new MockSocketClient<PlayerMessage, GMMessage>((m) => { lastSended = m; });
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
            var conf = new MockGameConfiguration();
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
            Assert.True(player.Position == playerEndField);
            Assert.True(lastSended.Id == GMMessageId.MoveAnswer);

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
            Assert.True(lastSended.Id == GMMessageId.MoveAnswer);

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
            Assert.True(lastSended.Id == GMMessageId.MoveAnswer);
            var payload = JsonConvert.DeserializeObject<MoveAnswerPayload>(lastSended.Payload);
            Assert.True(payload.MadeMove);

            lastSended = null;
            moved = await firstPlayer.MoveAsync(secondPlayerField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.True(lastSended.Id == GMMessageId.MoveAnswer);
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
            Assert.True(lastSended.Id == GMMessageId.MoveAnswer);

            lastSended = null;
            moved = await player.MoveAsync(secondField, gm, CancellationToken.None);
            Assert.False(moved);
            Assert.True(lastSended.Id == GMMessageId.NotWaitedError);
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
            Assert.True(lastSended.Id == GMMessageId.MoveAnswer);

            await Task.Delay(conf.MovePenalty * 2);
            lastSended = null;
            moved = await player.MoveAsync(secondField, gm, CancellationToken.None);
            Assert.True(moved);
            Assert.True(lastSended.Id == GMMessageId.MoveAnswer);
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
            Assert.True(lastSended.Id == GMMessageId.DestructionAnswer);

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
            Assert.True(lastSended.Id == GMMessageId.CheckAnswer);
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
            Assert.True(lastSended.Id == GMMessageId.DiscoverAnswer);
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
            (bool goal, bool removed) = await player.PutAsync(CancellationToken.None);
            Assert.True(goal && removed);
            Assert.True(player.Holding is null);
            Assert.True(lastSended.Id == GMMessageId.PutAnswer);

            await Task.Delay(conf.PutPenalty * 2);
            lastSended = null;
            (goal, removed) = await player.PutAsync(CancellationToken.None);
            Assert.False(goal || removed);
            Assert.True(lastSended.Id == GMMessageId.PutError);
            var payload = JsonConvert.DeserializeObject<PutErrorPayload>(lastSended.Payload);
            Assert.True(payload.ErrorSubtype == PutError.AgentNotHolding);
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
            Assert.True(lastSended.Id == GMMessageId.PickError);
            var payload = JsonConvert.DeserializeObject<PickErrorPayload>(lastSended.Payload);
            Assert.True(payload.ErrorSubtype == PickError.NothingThere);

            // delay
            await Task.Delay(conf.PickPenalty * 2);
            field.Put(piece);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.True(picked);
            Assert.True(player.Holding == piece);
            Assert.True(lastSended.Id == GMMessageId.PickAnswer);

            // delay
            await Task.Delay(conf.PickPenalty * 2);
            lastSended = null;
            picked = await player.PickAsync(CancellationToken.None);
            Assert.False(picked);
            Assert.True(player.Holding == piece);
            Assert.True(lastSended.Id == GMMessageId.PickError);
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
            Assert.True(lastSended.Id == GMMessageId.PickError);
            payload = JsonConvert.DeserializeObject<PickErrorPayload>(lastSended.Payload);
            Assert.True(payload.ErrorSubtype == PickError.Other);
        }
    }
}
