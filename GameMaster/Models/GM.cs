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
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Shared.Enums;
using Shared.Messages;
using Shared.Payloads;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly ILogger logger;
        private readonly IApplicationLifetime lifetime;
        private readonly GameConfiguration conf;
        private readonly BufferBlock<PlayerMessage> queue;
        private readonly ISocketManager<WebSocket, GMMessage> socketManager;

        private HashSet<(int recipient, int sender)> legalKnowledgeReplies;
        private Dictionary<int, GMPlayer> players;
        private AbstractField[][] board;

        private int redTeamPoints;
        private int blueTeamPoints;

        public bool WasGameInitialized { get; set; }

        public bool WasGameStarted { get; set; }

        public GM(IApplicationLifetime lifetime, GameConfiguration conf,
            BufferBlock<PlayerMessage> queue, WebSocketManager<GMMessage> socketManager)
        {
            this.logger = Log.ForContext<GM>();
            this.lifetime = lifetime;
            this.conf = conf;
            this.queue = queue;
            this.socketManager = socketManager;

            players = new Dictionary<int, GMPlayer>();
            legalKnowledgeReplies = new HashSet<(int, int)>();
        }

        public async Task AcceptMessage(PlayerMessage message, CancellationToken cancellationToken)
        {
            players.TryGetValue(message.PlayerID, out GMPlayer player);
            switch (message.MessageID)
            {
                case PlayerMessageID.CheckPiece:
                    await players[message.PlayerID].CheckHoldingAsync(cancellationToken);
                    break;
                case PlayerMessageID.PieceDestruction:
                    bool destroyed = await players[message.PlayerID].DestroyHoldingAsync(cancellationToken);
                    if (destroyed)
                    {
                        GeneratePiece();
                    }
                    break;
                case PlayerMessageID.Discover:
                    await players[message.PlayerID].DiscoverAsync(this, cancellationToken);
                    break;
                case PlayerMessageID.GiveInfo:
                    await ForwardKnowledgeReply(message, cancellationToken);
                    break;
                case PlayerMessageID.BegForInfo:
                    await ForwardKnowledgeQuestion(message, cancellationToken);
                    break;
                case PlayerMessageID.JoinTheGame:
                    {
                        JoinGamePayload payloadJoin = JsonConvert.DeserializeObject<JoinGamePayload>(message.Payload);
                        int key = players.Count;
                        bool accepted = TryToAddPlayer(key, payloadJoin.TeamID);
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

                        if (GetPlayersCount(Team.Red) == conf.NumberOfPlayersPerTeam &&
                           GetPlayersCount(Team.Blue) == conf.NumberOfPlayersPerTeam)
                        {
                            StartGame();
                            WasGameStarted = true;
                            logger.Information("Game was started.");
                        }
                        break;
                    }
                case PlayerMessageID.Move:
                    {
                        MovePayload payloadMove = JsonConvert.DeserializeObject<MovePayload>(message.Payload);
                        AbstractField field = null;
                        int[] pos = players[message.PlayerID].GetPosition();
                        switch (payloadMove.Direction)
                        {
                            case Direction.N:
                                if (pos[1] + 1 < board.GetLength(1))
                                {
                                    field = board[pos[0]][pos[1] + 1];
                                }
                                break;
                            case Direction.S:
                                if (pos[1] - 1 >= 0)
                                {
                                    field = board[pos[0]][pos[1] - 1];
                                }
                                break;
                            case Direction.E:
                                if (pos[0] + 1 < board.GetLength(0))
                                {
                                    field = board[pos[0] + 1][pos[1]];
                                }
                                break;
                            case Direction.W:
                                if (pos[0] - 1 >= 0)
                                {
                                    field = board[pos[0] - 1][pos[1]];
                                }
                                break;
                        }
                        if (!(field is null))
                        {
                            await players[message.PlayerID].MoveAsync(field, this, cancellationToken);
                        }
                        break;
                    }
                case PlayerMessageID.Pick:
                    await players[message.PlayerID].PickAsync(cancellationToken);
                    break;
                case PlayerMessageID.Put:
                    (bool point, bool removed) = await players[message.PlayerID].PutAsync(cancellationToken);
                    if (point)
                    {
                        if (players[message.PlayerID].Team == Team.Red)
                        {
                            redTeamPoints++;
                        }
                        else
                        {
                            blueTeamPoints++;
                        }
                    }
                    if (removed)
                    {
                        GeneratePiece();
                    }
                    break;
            }
        }

        private bool TryToAddPlayer(int key, Team team)
        {
            if (GetPlayersCount(team) == conf.NumberOfPlayersPerTeam)
            {
                return false;
            }

            return players.TryAdd(key, new GMPlayer(key, conf, socketManager, team));
        }

        private int GetPlayersCount(Team team)
        {
            return players.Where(pair => pair.Value.Team == team).Count();
        }

        internal void InitGame()
        {
            InitializeBoard();
            GenerateAllPieces();
            WasGameInitialized = true;
            logger.Information("Game was initialized.");
        }

        internal void StartGame()
        {
            // TODO: Send init message here
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

        private void GenerateAllPieces()
        {
            for (int i = 0; i < conf.NumberOfPiecesOnBoard; ++i)
            {
                GeneratePiece();
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
                    logger.Warning($"Message retrieve was cancelled: {e.Message}");
                }
            }
        }

        internal Dictionary<Direction, int> Discover(AbstractField field)
        {
            int[] center = field.GetPosition();
            var neighbourCoordinates = DirectionExtensions.GetCoordinatesAroundCenter(center);

            int[] distances = new int[neighbourCoordinates.Length];
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
                            int manhattanDistance = Math.Abs(neighbourCoordinates[k].y - i) + Math.Abs(neighbourCoordinates[k].x - j);
                            if (manhattanDistance < distances[k])
                                distances[k] = manhattanDistance;
                        }
                    }
                }
            }

            Dictionary<Direction, int> discoveryResult = new Dictionary<Direction, int>();
            for (int i = 0; i < distances.Length; i++)
            {
                var (dir, y, x) = neighbourCoordinates[i];
                if (y >= 0 && y < conf.Height && x >= 0 && x < conf.Width)
                {
                    discoveryResult.Add(dir, distances[i]);
                }
            }
            return discoveryResult;
        }

        private void GeneratePiece()
        {
            var rand = new Random();
            bool isSham = rand.Next(0, 101) < conf.ShamPieceProbability;
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

        private async Task ForwardKnowledgeQuestion(PlayerMessage playerMessage, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            BegForInfoPayload begPayload = JsonConvert.DeserializeObject<BegForInfoPayload>(playerMessage.Payload);
            BegForInfoForwardedPayload payload = new BegForInfoForwardedPayload()
            {
                AskingID = playerMessage.PlayerID,
                Leader = players[playerMessage.PlayerID].IsLeader,
                TeamId = players[playerMessage.PlayerID].Team,
            };
            GMMessage gmMessage = new GMMessage()
            {
                Id = GMMessageID.BegForInfoForwarded,
                Payload = payload.Serialize(),
            };

            legalKnowledgeReplies.Add((begPayload.AskedPlayerID, playerMessage.PlayerID));
            await socketManager.SendMessageAsync(
                players[begPayload.AskedPlayerID].SocketID,
                gmMessage, cancellationToken);
        }

        private async Task ForwardKnowledgeReply(PlayerMessage playerMessage, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            GiveInfoPayload payload = JsonConvert.DeserializeObject<GiveInfoPayload>(playerMessage.Payload);
            if (legalKnowledgeReplies.Contains((playerMessage.PlayerID, payload.RespondToID)))
            {
                legalKnowledgeReplies.Remove((playerMessage.PlayerID, payload.RespondToID));
                GiveInfoForwardedPayload answerPayload = new GiveInfoForwardedPayload()
                {
                    AnsweringID = playerMessage.PlayerID,
                    Distances = payload.Distances,
                    RedTeamGoalAreaInformations = payload.RedTeamGoalAreaInformations,
                    BlueTeamGoalAreaInformations = payload.BlueTeamGoalAreaInformations,
                };
                GMMessage answer = new GMMessage()
                {
                    Id = GMMessageID.GiveInfoForwarded,
                    Payload = answerPayload.Serialize(),
                };
                await socketManager.SendMessageAsync(payload.RespondToID.ToString(), answer, cancellationToken);
            }
        }

        internal void EndGame()
        {
            logger.Information("The winner is team {0}", redTeamPoints > blueTeamPoints ? Team.Red : Team.Blue);
            lifetime.StopApplication();
        }
    }
}
