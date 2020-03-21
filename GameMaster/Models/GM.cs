using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;

using GameMaster.Managers;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Newtonsoft.Json;
using Shared;
using Shared.Models.Enums;
using Shared.Models.Messages;
using Shared.Models.Payloads;
using Shared.Payloads;

namespace GameMaster.Models
{
    public class GM
    {
        private Configuration conf;
        private BufferBlock<PlayerMessage> queue;
        private WebSocketManager<GMMessage> manager;

        private readonly int[] legalKnowledgeReplies;
        private Dictionary<int, GMPlayer> players;
        private AbstractField[][] board;
        private int piecesOnBoard;

        private int redTeamPoints;
        private int blueTeamPoints;

        public bool WasGameStarted { get; set; }

        public GM(Configuration conf, BufferBlock<PlayerMessage> queue, WebSocketManager<GMMessage> manager)
        {
            this.conf = conf;
            this.queue = queue;
            this.manager = manager;
            legalKnowledgeReplies = new int[2];
        }

        public async Task AcceptMessage(PlayerMessage message, CancellationToken cancellationToken)
        {
            if (queue.TryReceive(null, out message))
            {
                switch (message.MessageID)
                {
                    case (int)MessageID.CheckPiece:
                        players[message.AgentID].CheckHolding();
                        break;
                    case (int)MessageID.PieceDestruction:
                        players[message.AgentID].DestroyHolding();
                        break;
                    case (int)MessageID.Discover:
                        players[message.AgentID].Discover(this);
                        break;
                    case (int)MessageID.GiveInfo:
                        ForwardKnowledgeReply(message);
                        break;
                    case (int)MessageID.BegForInfo:
                        ForwardKnowledgeQuestion(message);
                        break;
                    case (int)MessageID.JoinTheGame:
                        JoinGamePayload payload1 = JsonConvert.DeserializeObject<JoinGamePayload>(message.Payload);
                        int key = players.Count;
                        bool accepted = players.TryAdd(key, new GMPlayer(key, (Team)Enum.Parse(typeof(Team), payload1.TeamID)));
                        JoinAnswerPayload answer1Payload = new JoinAnswerPayload()
                        {
                            accepted = accepted,
                            agentID = key,
                        };
                        GMMessage answer1 = new GMMessage()
                        {
                            Id = 107,
                            Payload = JsonConvert.SerializeObject(answer1Payload),
                        };
                        await manager.SendMessageAsync(players[key].SocketID, answer1);
                        break;
                    case (int)MessageID.Move:
                        MovePayload payload2 = JsonConvert.DeserializeObject<MovePayload>(message.Payload);
                        AbstractField field = null;
                        int[] position1 = players[message.AgentID].GetPosition();
                        switch ((Directions)Enum.Parse(typeof(Directions), payload2.Direction))
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
                        players[message.AgentID].Move(field);
                        break;
                    case (int)MessageID.Pick:
                        int[] position2 = players[message.AgentID].GetPosition();
                        board[position2[0]][position2[1]].PickUp(players[message.AgentID]);
                        EmptyPayload answer2Payload = new EmptyPayload();
                        GMMessage answer2 = new GMMessage()
                        {
                            Id = 109,
                            Payload = JsonConvert.SerializeObject(answer2Payload),
                        };
                        await manager.SendMessageAsync(players[message.AgentID].SocketID, answer2);
                        break;
                    case (int)MessageID.Put:
                        bool point = players[message.AgentID].Put();
                        if (point)
                        {
                            if (players[message.AgentID].Team == Team.Red) redTeamPoints++;
                            else blueTeamPoints++;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        internal Dictionary<Direction, int> Discover(AbstractField field)
        {
            throw new NotImplementedException();
        }

        internal void StartGame()
        {
            InitializeBoard();

            // TODO : initialize rest
            players = new Dictionary<int, GMPlayer>();

            WasGameStarted = true;
        }

        private void InitializeBoard()
        {
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
        }

        private void FillBoardRow(int row, Func<int, int, AbstractField> getField)
        {
            for (int col = 0; col < board[row].Length; ++col)
            {
                board[row][col] = getField(row, col);
            }
        }

        public void GenerateGUI()
        {
            throw new NotImplementedException();
        }

        internal async Task Work(CancellationToken cancellationToken)
        {
            bool shouldGeneratePiece = true;
            var timer = PrepareGeneratePieceTimer((sender, e) =>
            {
                if (piecesOnBoard < conf.MaximumNumberOfPiecesOnBoard)
                {
                    shouldGeneratePiece = true;
                }
            });
            TimeSpan cancellationTimespan = TimeSpan.FromMilliseconds(50);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (queue.Count > 0)
                {
                    int maxMessagesToRead = Math.Min(conf.NumberOfPlayersPerTeam, queue.Count);
                    for (int i = 0; i < maxMessagesToRead; ++i)
                    {
                        var message = await queue.ReceiveAsync(cancellationTimespan, cancellationToken);
                        await AcceptMessage(message, cancellationToken);
                        if (conf.NumberOfGoals == blueTeamPoints || conf.NumberOfGoals == redTeamPoints)
                        {
                            EndGame();
                            break;
                        }
                    }
                }

                if (shouldGeneratePiece)
                {
                    timer.Stop();
                    GeneratePiece();
                    shouldGeneratePiece = false;
                    timer.Start();
                }
            }
        }

        private System.Timers.Timer PrepareGeneratePieceTimer(ElapsedEventHandler elapsed)
        {
            var timer = new System.Timers.Timer()
            {
                Interval = conf.GeneratePieceInterval,
                AutoReset = true,
            };
            timer.Elapsed += elapsed;

            return timer;
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
            piecesOnBoard += 1;
        }

        private async void ForwardKnowledgeQuestion(PlayerMessage agentMessage)
        {
            throw new NotImplementedException();
        }

        private async void ForwardKnowledgeReply(PlayerMessage agentMessage)
        {
            throw new NotImplementedException();
        }

        internal void EndGame()
        {
            throw new NotImplementedException();
        }
    }
}
