using GameMaster.Managers;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Newtonsoft.Json;
using Shared;
using Shared.Models.Messages;
using Shared.Models.Payloads;
using Shared.Payloads;
using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly Dictionary<int, GMPlayer> players;
        private readonly AbstractField[][] board;
        private static int[] legalKnowledgeReplies = new int[2]; // unique from documentation considered as static
        private Configuration conf;
        internal int redTeamPoints;
        internal int blueTeamPoints;

        private BufferBlock<PlayerMessage> queue;
        private WebSocketManager<GMMessage> manager;

        public GM(Configuration conf, BufferBlock<PlayerMessage> _queue, WebSocketManager<GMMessage> _manager)
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

            queue = _queue;
            manager = _manager;

            // TODO : initialize rest
        }

        private void FillBoardRow(int row, Func<int, int, AbstractField> getField)
        {
            for (int col = 0; col < board[row].Length; ++col)
            {
                board[row][col] = getField(row, col);
            }
        }

        public async void AcceptMessage()
        {
            PlayerMessage message;
            if (queue.TryReceive(null, out message))
            {
                switch (message.messageID)
                {
                    case (int)MessageID.CheckPiece:
                        players[message.agentID].CheckHolding();
                        break;
                    case (int)MessageID.PieceDestruction:
                        players[message.agentID].DestroyHolding();
                        break;
                    case (int)MessageID.Discover:
                        players[message.agentID].Discover(this);
                        break;
                    case (int)MessageID.GiveInfo:
                        ForwardKnowledgeReply(message);
                        break;
                    case (int)MessageID.BegForInfo:
                        ForwardKnowledgeQuestion(message);
                        break;
                    case (int)MessageID.JoinTheGame:
                        JoinGamePayload payload1 = JsonConvert.DeserializeObject<JoinGamePayload>(message.payload);
                        int key = players.Count;
                        bool accepted = players.TryAdd(key, new GMPlayer(key, (Team)Enum.Parse(typeof(Team), payload1.teamID)));
                        JoinAnswerPayload answer1Payload = new JoinAnswerPayload()
                        {
                            accepted = accepted,
                            agentID = key
                        };
                        GMMessage answer1 = new GMMessage()
                        {
                            id = 107,
                            payload = JsonConvert.SerializeObject(answer1Payload)
                        };
                        await manager.SendMessageAsync(players[key].SocketID, answer1);
                        break;
                    case (int)MessageID.Move:
                        MovePayload payload2 = JsonConvert.DeserializeObject<MovePayload>(message.payload);
                        AbstractField field = null;
                        int[] position1 = players[message.agentID].GetPosition();
                        switch ((Directions)Enum.Parse(typeof(Directions), payload2.direction))
                        {
                            case Directions.N:
                                if (position1[1] + 1 < board.GetLength(1)) field = board[position1[0]][position1[1] + 1];
                                break;
                            case Directions.S:
                                if (position1[1] - 1 >= 0) field = board[position1[0]][position1[1] - 1];
                                break;
                            case Directions.W:
                                if (position1[0] + 1 < board.GetLength(0)) field = board[position1[0] + 1][position1[1]];
                                break;
                            case Directions.E:
                                if (position1[0] - 1 >= 0) field = board[position1[0] - 1][position1[1]];
                                break;
                        }
                        players[message.agentID].Move(field);
                        break;
                    case (int)MessageID.Pick:
                        int[] position2 = players[message.agentID].GetPosition();
                        board[position2[0]][position2[1]].PickUp(players[message.agentID]);
                        EmptyPayload answer2Payload = new EmptyPayload();
                        GMMessage answer2 = new GMMessage()
                        {
                            id = 109,
                            payload = JsonConvert.SerializeObject(answer2Payload)
                        };
                        await manager.SendMessageAsync(players[message.agentID].SocketID, answer2);
                        break;
                    case (int)MessageID.Put:
                        bool point = players[message.agentID].Put();
                        if (point)
                        {
                            if (players[message.agentID].team == Team.Red) redTeamPoints++;
                            else blueTeamPoints++;
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

        private async void ForwardKnowledgeQuestion(PlayerMessage agentMessage)
        {
            throw new NotImplementedException();
        }

        private async void ForwardKnowledgeReply(PlayerMessage agentMessage)
        {
            throw new NotImplementedException();
        }
    }
}