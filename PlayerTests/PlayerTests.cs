using Shared;
using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Shared.Models.Messages;
using Shared.Models.Payloads;
using Newtonsoft.Json;
using Player.Models;
using System;
using System.Collections.Generic;
using Player.Clients;

namespace Player.Tests
{
    public class PlayerTests
    {
        [Fact]
        public void TestAcceptMessageMoveAccept()
        {
            MoveAnswerPayload payload = new MoveAnswerPayload();
            payload.madeMove = true;
            payload.currentPosition = new Position();
            payload.currentPosition.x = 0;
            payload.currentPosition.y = 0;
            payload.closestPiece = 0;
            string payloadstring = JsonConvert.SerializeObject(payload);

            GMMessage message = new GMMessage();
            message.payload = payloadstring;
            message.id = (int)MessageID.MoveAnswer;

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
            DiscoveryAnswerPayload payload = new DiscoveryAnswerPayload();
            payload.distanceFromCurrent = 0;
            payload.distanceE = 0;
            payload.distanceW = 0;
            payload.distanceS = 0;
            payload.distanceN = 0;
            payload.distanceNE = 0;
            payload.distanceSE = 0;
            payload.distanceNW = 0;
            payload.distanceNE = 0;
            string payloadstring = JsonConvert.SerializeObject(payload);

            GMMessage message = new GMMessage();
            message.payload = payloadstring;
            message.id = (int)MessageID.MoveAnswer;

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
            BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload();
            payload.askingID = 1;
            payload.leader = false;
            payload.teamId = "Red";
            string payloadstring = JsonConvert.SerializeObject(payload);

            GMMessage message = new GMMessage();
            message.payload = payloadstring;
            message.id = (int)MessageID.BegForInfoForwarded;

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