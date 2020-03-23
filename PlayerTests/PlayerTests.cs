using Newtonsoft.Json;
using Player.Clients;
using Player.Models;
using Shared;
using Shared.Models.Messages;
using Shared.Models.Payloads;
using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace Player.Tests
{
    public class PlayerTests
    {
        [Fact]
        public void TestAcceptMessageMoveAccept()
        {
            MoveAnswerPayload payload = new MoveAnswerPayload()
            {
                madeMove = true,
                currentPosition = new Position()
                {
                    x = 0,
                    y = 0
                },
                closestPiece = 0
            };
            GMMessage message = new GMMessage()
            {
                id = (int)MessageID.MoveAnswer,
                payload = JsonConvert.SerializeObject(payload)
            };

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post<GMMessage>(message);

            Team team = Team.Red;
            var player = new Player.Models.Player(team, input, new WebSocketClient<GMMessage, AgentMessage>());

            player.board = new Field[1, 1];
            player.board[0, 0] = new Field();

            player.AcceptMessage();

            Assert.Equal(0, player.board[0, 0].distToPiece);
        }

        [Fact]
        public void TestAcceptMessageDiscoverAccept()
        {
            DiscoveryAnswerPayload payload = new DiscoveryAnswerPayload()
            {
                distanceFromCurrent = 0,
                distanceE = 0,
                distanceW = 0,
                distanceS = 0,
                distanceN = 0,
                distanceNE = 0,
                distanceSE = 0,
                distanceNW = 0,
                distanceSW = 0
            };
            GMMessage message = new GMMessage()
            {
                id = (int)MessageID.MoveAnswer,
                payload = JsonConvert.SerializeObject(payload)
            };

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post<GMMessage>(message);

            Team team = Team.Red;
            var player = new Player.Models.Player(team, input, new WebSocketClient<GMMessage, AgentMessage>());

            player.board = new Field[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    player.board[i, j] = new Field();
                }
            }
            player.position = new Tuple<int, int>(1, 1);

            player.AcceptMessage();

            Assert.Equal(0, player.board[0, 0].distToPiece);
        }

        [Fact]
        public void TestAcceptMessageBegForInfoAccept()
        {
            BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload()
            {
                askingID = 1,
                leader = false,
                teamId = "Red"
            };
            GMMessage message = new GMMessage()
            {
                id = (int)MessageID.BegForInfoForwarded,
                payload = JsonConvert.SerializeObject(payload)
            };

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post<GMMessage>(message);

            Team team = Team.Red;
            var player = new Player.Models.Player(team, input, new WebSocketClient<GMMessage, AgentMessage>());

            player.waitingPlayers = new List<int>();

            player.AcceptMessage();

            Assert.Single(player.waitingPlayers);
        }
    }
}
