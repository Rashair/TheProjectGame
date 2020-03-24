using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Newtonsoft.Json;
using Shared;
using Shared.Enums;
using Shared.Models.Messages;
using Shared.Models.Payloads;

namespace GameMaster.Models
{
    public class GM
    {
        private static int[] legalKnowledgeReplies = new int[2]; // unique from documentation considered as static
        private readonly Dictionary<int, GMPlayer> players;
        private readonly AbstractField[][] board;
        private Configuration conf;

        internal int RedTeamPoints { get; set; }

        internal int BlueTeamPoints { get; set; }

        private BufferBlock<PlayerMessage> queue;
        private SocketManager<WebSocket, GMMessage> manager;

        public GM(Configuration conf, BufferBlock<PlayerMessage> queue, WebSocketManager<GMMessage> manager)
        {
            this.conf = conf;
            board = new AbstractField[conf.Height][];
            for (int i = 0; i < board.Length; ++i)
            {
                board[i] = new AbstractField[conf.Width];
            }

            Func<int, int, AbstractField> nonGoalFieldGenerator = (int x, int y) => new NonGoalField(x, y);
            for (int rowIt = 0; rowIt < conf.GoalAreaHeight; ++rowIt)
            {
                FillBoardRow(rowIt, nonGoalFieldGenerator);
            }

            Func<int, int, AbstractField> taskFieldGenerator = (int x, int y) => new TaskField(x, y);
            int secondGoalAreaStart = conf.Height - conf.GoalAreaHeight;
            for (int rowIt = conf.GoalAreaHeight; rowIt < secondGoalAreaStart; ++rowIt)
            {
                FillBoardRow(rowIt, taskFieldGenerator);
            }

            for (int rowIt = secondGoalAreaStart; rowIt < conf.Height; ++rowIt)
            {
                FillBoardRow(rowIt, nonGoalFieldGenerator);
            }

            this.queue = queue;
            this.manager = manager;

            // TODO : initialize rest
        }

        private void FillBoardRow(int row, Func<int, int, AbstractField> getField)
        {
            for (int col = 0; col < board[row].Length; ++col)
            {
                board[row][col] = getField(row, col);
            }
        }

        public async Task AcceptMessage()
        {
            PlayerMessage message;
            if (queue.TryReceive(null, out message))
            {
                switch (message.MessageID)
                {
                    case PlayerMessageID.CheckPiece:
                        players[message.PlayerID].CheckHolding();
                        break;
                    case PlayerMessageID.PieceDestruction:
                        players[message.PlayerID].DestroyHolding();
                        break;
                    case PlayerMessageID.Discover:
                        players[message.PlayerID].Discover(this);
                        break;
                    case PlayerMessageID.GiveInfo:
                        ForwardKnowledgeReply(message);
                        break;
                    case PlayerMessageID.BegForInfo:
                        ForwardKnowledgeQuestion(message);
                        break;
                    case PlayerMessageID.JoinTheGame:
                        JoinGamePayload payloadJoin = JsonConvert.DeserializeObject<JoinGamePayload>(message.Payload);
                        int key = players.Count;
                        bool accepted = players.TryAdd(key, new GMPlayer(key, payloadJoin.TeamID));
                        JoinAnswerPayload answerJoinPayload = new JoinAnswerPayload()
                        {
                            Accepted = accepted,
                            PlayerID = key,
                        };
                        GMMessage answerJoin = new GMMessage()
                        {
                            Id = GMMessageID.JoinTheGameAnswer,
                            Payload = JsonConvert.SerializeObject(answerJoinPayload),
                        };
                        await manager.SendMessageAsync(players[key].SocketID, answerJoin);
                        break;
                    case PlayerMessageID.Move:
                        MovePayload payloadMove = JsonConvert.DeserializeObject<MovePayload>(message.Payload);
                        AbstractField field = null;
                        int[] position1 = players[message.PlayerID].GetPosition();
                        switch (payloadMove.Direction)
                        {
                            case Directions.N:
                                if (position1[1] + 1 < board.GetLength(1)) field = board[position1[0]][position1[1] + 1];
                                break;
                            case Directions.S:
                                if (position1[1] - 1 >= 0) field = board[position1[0]][position1[1] - 1];
                                break;
                            case Directions.E:
                                if (position1[0] + 1 < board.GetLength(0)) field = board[position1[0] + 1][position1[1]];
                                break;
                            case Directions.W:
                                if (position1[0] - 1 >= 0) field = board[position1[0] - 1][position1[1]];
                                break;
                        }
                        players[message.PlayerID].Move(field);
                        break;
                    case PlayerMessageID.Pick:
                        int[] position2 = players[message.PlayerID].GetPosition();
                        board[position2[0]][position2[1]].PickUp(players[message.PlayerID]);
                        EmptyPayload answerPickPayload = new EmptyPayload();
                        GMMessage answerPick = new GMMessage()
                        {
                            Id = GMMessageID.PickAnswer,
                            Payload = JsonConvert.SerializeObject(answerPickPayload),
                        };
                        await manager.SendMessageAsync(players[message.PlayerID].SocketID, answerPick);
                        break;
                    case PlayerMessageID.Put:
                        bool point = players[message.PlayerID].Put();
                        if (point)
                        {
                            if (players[message.PlayerID].Team == Team.Red) RedTeamPoints++;
                            else BlueTeamPoints++;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public void GenerateGUI()
        {
            throw new NotImplementedException();
        }

        internal Dictionary<Direction, int> Discover(AbstractField field)
        {
            throw new NotImplementedException();
        }

        internal void EndGame()
        {
            throw new NotImplementedException();
        }

        private void GeneratePiece()
        {
            var rand = new Random();
            bool isSham = rand.Next(0, 100) <= conf.ShamPieceProbability;
            AbstractPiece piece;
            if (isSham)
            {
                piece = new ShamPiece();
            }
            else
            {
                piece = new NormalPiece();
            }

            int taskAreaStart = conf.GoalAreaHeight;
            int taskAreaEnd = conf.Height - conf.GoalAreaHeight;
            int xCoord = rand.Next(taskAreaStart, taskAreaEnd);
            int yCoord = rand.Next(0, conf.Width);

            board[xCoord][yCoord].Put(piece);
        }

        private async Task ForwardKnowledgeQuestion(PlayerMessage playerMessage)
        {
            throw new NotImplementedException();
        }

        private async Task ForwardKnowledgeReply(PlayerMessage playerMessage)
        {
            throw new NotImplementedException();
        }
    }
}
