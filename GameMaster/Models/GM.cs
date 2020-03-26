using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Enums;
using Shared.Models.Messages;
using Shared.Models.Payloads;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly ILogger<GM> logger;
        private readonly Configuration conf;
        private readonly BufferBlock<PlayerMessage> queue;
        private ISocketManager<WebSocket, GMMessage> socketManager;

        private readonly int[] legalKnowledgeReplies;
        private Dictionary<int, GMPlayer> players;
        private AbstractField[][] board;
        private int piecesOnBoard;

        private int redTeamPoints;
        private int blueTeamPoints;

        public bool WasGameStarted { get; set; }

        public GM(Configuration conf, BufferBlock<PlayerMessage> queue, ILogger<GM> logger,
            WebSocketManager<GMMessage> socketManager)
        {
            this.logger = logger;
            this.conf = conf;
            this.queue = queue;
            this.socketManager = socketManager;
            legalKnowledgeReplies = new int[2];
        }

        public async Task AcceptMessage(PlayerMessage message, CancellationToken cancellationToken)
        {
            switch (message.MessageID)
            {
                case PlayerMessageID.CheckPiece:
                    players[message.PlayerID].CheckHolding();
                    break;
                case PlayerMessageID.PieceDestruction:
                    players[message.PlayerID].DestroyHolding();
                    piecesOnBoard--;
                    GeneratePiece();
                    break;
                case PlayerMessageID.Discover:
                    players[message.PlayerID].Discover(this);
                    break;
                case PlayerMessageID.GiveInfo:
                    await ForwardKnowledgeReply(message, cancellationToken);
                    break;
                case PlayerMessageID.BegForInfo:
                    await ForwardKnowledgeQuestion(message, cancellationToken);
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
                    await socketManager.SendMessageAsync(players[key].SocketID, answerJoin, cancellationToken);
                    break;
                case PlayerMessageID.Move:
                    MovePayload payloadMove = JsonConvert.DeserializeObject<MovePayload>(message.Payload);
                    AbstractField field = null;
                    int[] position1 = players[message.PlayerID].GetPosition();
                    switch (payloadMove.Direction)
                    {
                        case Directions.N:
                            if (position1[1] + 1 < board.GetLength(1))
                            {
                                field = board[position1[0]][position1[1] + 1];
                            }
                            break;
                        case Directions.S:
                            if (position1[1] - 1 >= 0)
                            {
                                field = board[position1[0]][position1[1] - 1];
                            }
                            break;
                        case Directions.E:
                            if (position1[0] + 1 < board.GetLength(0))
                            {
                                field = board[position1[0] + 1][position1[1]];
                            }
                            break;
                        case Directions.W:
                            if (position1[0] - 1 >= 0)
                            {
                                field = board[position1[0] - 1][position1[1]];
                            }
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
                    await socketManager.SendMessageAsync(players[message.PlayerID].SocketID, answerPick,
                        cancellationToken);
                    break;
                case PlayerMessageID.Put:
                    bool point = players[message.PlayerID].Put();
                    if (point)
                    {
                        piecesOnBoard--;
                        if (players[message.PlayerID].Team == Team.Red)
                        {
                            redTeamPoints++;
                        }
                        else
                        {
                            blueTeamPoints++;
                        }
                    }
                    GeneratePiece();
                    break;
                default:
                    break;
            }
        }

        internal Dictionary<Direction, int> Discover(AbstractField field)
        {
            int[] center = field.GetPosition();
            int[,] neighbourCoordinates = new int[9, 2]
            {
                // up row
                { center[0] - 1, center[1] - 1 },
                { center[0] - 1, center[1] },
                { center[0] - 1, center[1] + 1 },

                // middle row
                { center[0], center[1] - 1 },
                { center[0], center[1] },
                { center[0], center[1] + 1 },

                // down row
                { center[0] + 1, center[1] - 1 },
                { center[0] + 1, center[1] },
                { center[0] + 1, center[1] + 1 },
            };

            int[] distances = new int[9];
            for (int i = 0; i < distances.Length; i++)
            {
                distances[i] = int.MaxValue;
            }

            int secondGoalAreaStart = conf.Height - conf.GoalAreaHeight;
            for (int i = conf.GoalAreaHeight; i < secondGoalAreaStart; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    if (board[i][j].ContainsPieces())
                    {
                        for (int k = 0; k < distances.Length; k++)
                        {
                            int manhattanDistance = Math.Abs(neighbourCoordinates[k, 0] - i) + Math.Abs(neighbourCoordinates[k, 1] - j);
                            if (manhattanDistance < distances[k])
                                distances[k] = manhattanDistance;
                        }
                    }
                }
            }

            Direction[] direction = (Direction[])Enum.GetValues(typeof(Direction));
            Array.Sort(direction);
            Dictionary<Direction, int> discoveryResult = new Dictionary<Direction, int>();
            for (int i = 0; i < distances.Length; i++)
            {
                int x = neighbourCoordinates[i, 0];
                int y = neighbourCoordinates[i, 1];

                if (distances[i] >= 0 && x >= conf.GoalAreaHeight && x <= secondGoalAreaStart && y >= 0 && y < conf.Width)
                {
                    discoveryResult.Add(direction[i], distances[i]);
                }
            }
            return discoveryResult;
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

        internal async Task Work(CancellationToken cancellationToken)
        {
            TimeSpan cancellationTimespan = TimeSpan.FromMilliseconds(100);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PlayerMessage message = await queue.ReceiveAsync(cancellationTimespan, cancellationToken);
                    await AcceptMessage(message, cancellationToken);
                    if (conf.NumberOfGoals == blueTeamPoints || conf.NumberOfGoals == redTeamPoints)
                    {
                        EndGame();
                        break;
                    }
                }
                catch (OperationCanceledException e)
                {
                    logger.LogWarning($"Message retrieve was cancelled: {e.Message}");
                }
            }
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

        private async Task ForwardKnowledgeQuestion(PlayerMessage playerMessage, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task ForwardKnowledgeReply(PlayerMessage playerMessage, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal void EndGame()
        {
            throw new NotImplementedException();
        }
    }
}
