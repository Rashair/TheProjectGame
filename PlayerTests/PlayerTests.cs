using System.Threading.Tasks.Dataflow;

using Newtonsoft.Json;
using Player.Clients;
using Player.Models.Strategies;
using Shared;
using Shared.Models;
using Shared.Models.Messages;
using Shared.Models.Payloads;
using Xunit;

namespace Player.Tests
{
    public class PlayerTests
    {
        [Fact]
        public void TestAcceptMessageMoveAccept()
        {
            MoveAnswerPayload payloadMove = new MoveAnswerPayload()
            {
                MadeMove = true,
                CurrentPosition = new Position()
                {
                    X = 0,
                    Y = 0,
                },
                ClosestPiece = 0,
            };
            GMMessage messageMove = new GMMessage()
            {
                Id = (int)MessageID.MoveAnswer,
                Payload = JsonConvert.SerializeObject(payloadMove),
            };

            StartGamePayload payloadStart = new StartGamePayload
            {
                AgentID = 1,
                AlliesIDs = new int[1] { 2 },
                LeaderID = 1,
                EnemiesIDs = new int[2] { 3, 4 },
                TeamId = "Red",
                BoardSize = new BoardSize { X = 3,  Y = 3 },
                GoalAreaSize = 1,
                NumberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 },
                NumberOfPieces = 2,
                NumberOfGoals = 2,
                Penalties = new Penalties { Move = "0", CheckForSham = "0", Discovery = "0", DestroyPiece = "0", PutPiece = "0", InformationExchange = "0" },
                ShamPieceProbability = 0.5f,
                Position = new Position { X = 1, Y = 1 },
            };
            GMMessage messageStart = new GMMessage()
            {
                Id = (int)MessageID.StartGame,
                Payload = JsonConvert.SerializeObject(payloadStart),
            };

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post<GMMessage>(messageStart);
            input.Post<GMMessage>(messageMove);

            Team team = Team.Red;
            var player = new Player.Models.Player(team, new Strategy(), input, new WebSocketClient<GMMessage, PlayerMessage>());

            player.AcceptMessage();
            player.AcceptMessage();

            Assert.Equal(0, player.Board[0, 0].DistToPiece);
        }

        [Fact]
        public void TestAcceptMessageDiscoverAccept()
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
            GMMessage messageDiscover = new GMMessage()
            {
                Id = (int)MessageID.MoveAnswer,
                Payload = JsonConvert.SerializeObject(payloadDiscover),
            };

            StartGamePayload payloadStart = new StartGamePayload
            {
                AgentID = 1,
                AlliesIDs = new int[1] { 2 },
                LeaderID = 1,
                EnemiesIDs = new int[2] { 3, 4 },
                TeamId = "Red",
                BoardSize = new BoardSize { X = 3, Y = 3 },
                GoalAreaSize = 1,
                NumberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 },
                NumberOfPieces = 2,
                NumberOfGoals = 2,
                Penalties = new Penalties { Move = "0", CheckForSham = "0", Discovery = "0", DestroyPiece = "0", PutPiece = "0", InformationExchange = "0" },
                ShamPieceProbability = 0.5f,
                Position = new Position { X = 1, Y = 1 },
            };
            GMMessage messageStart = new GMMessage()
            {
                Id = (int)MessageID.StartGame,
                Payload = JsonConvert.SerializeObject(payloadStart),
            };

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post<GMMessage>(messageStart);
            input.Post<GMMessage>(messageDiscover);

            Team team = Team.Red;
            var player = new Player.Models.Player(team, new Strategy(), input, new WebSocketClient<GMMessage, PlayerMessage>());

            player.AcceptMessage();
            player.AcceptMessage();

            Assert.Equal(0, player.Board[0, 0].DistToPiece);
        }

        [Fact]
        public void TestAcceptMessageBegForInfoAccept()
        {
            BegForInfoForwardedPayload payloadBeg = new BegForInfoForwardedPayload()
            {
                AskingID = 2,
                Leader = false,
                TeamId = "Red",
            };
            GMMessage messageBeg = new GMMessage()
            {
                Id = (int)MessageID.BegForInfoForwarded,
                Payload = JsonConvert.SerializeObject(payloadBeg),
            };

            StartGamePayload payloadStart = new StartGamePayload
            {
                AgentID = 1,
                AlliesIDs = new int[1] { 2 },
                LeaderID = 1,
                EnemiesIDs = new int[2] { 3, 4 },
                TeamId = "Red",
                BoardSize = new BoardSize { X = 3, Y = 3 },
                GoalAreaSize = 1,
                NumberOfPlayers = new NumberOfPlayers { Allies = 2, Enemies = 2 },
                NumberOfPieces = 2,
                NumberOfGoals = 2,
                Penalties = new Penalties { Move = "0", CheckForSham = "0", Discovery = "0", DestroyPiece = "0", PutPiece = "0", InformationExchange = "0" },
                ShamPieceProbability = 0.5f,
                Position = new Position { X = 1, Y = 1 },
            };
            GMMessage messageStart = new GMMessage()
            {
                Id = (int)MessageID.StartGame,
                Payload = JsonConvert.SerializeObject(payloadStart),
            };

            BufferBlock<GMMessage> input = new BufferBlock<GMMessage>();
            input.Post<GMMessage>(messageStart);
            input.Post<GMMessage>(messageBeg);

            Team team = Team.Red;
            var player = new Player.Models.Player(team, new Strategy(), input, new WebSocketClient<GMMessage, PlayerMessage>());

            player.AcceptMessage();
            player.AcceptMessage();

            Assert.Single(player.WaitingPlayers);
        }
    }
}
