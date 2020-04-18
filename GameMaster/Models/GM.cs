using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly ILogger log;
        private readonly ILogger logger;
        private readonly IApplicationLifetime lifetime;
        private readonly GameConfiguration conf;
        private readonly BufferBlock<PlayerMessage> queue;
        private readonly ISocketClient<PlayerMessage, GMMessage> socketClient;

        private HashSet<(int recipient, int sender)> legalKnowledgeReplies;
        private readonly Dictionary<int, GMPlayer> players;
        private AbstractField[][] board;

        private int redTeamPoints;
        private int blueTeamPoints;

        public bool WasGameInitialized { get; private set; }

        public bool WasGameStarted { get; private set; }

        public int TaskAreaEnd { get => conf.Height - conf.GoalAreaHeight; }

        public GM(IApplicationLifetime lifetime, GameConfiguration conf,
            BufferBlock<PlayerMessage> queue, ISocketClient<PlayerMessage, GMMessage> socketClient,
            ILogger log)
        {
            this.log = log;
            this.logger = log.ForContext<GM>();
            this.lifetime = lifetime;
            this.conf = conf;
            this.queue = queue;
            this.socketClient = socketClient;

            players = new Dictionary<int, GMPlayer>();
            legalKnowledgeReplies = new HashSet<(int, int)>();
        }

        public async Task AcceptMessage(PlayerMessage message, CancellationToken cancellationToken)
        {
            if (!WasGameInitialized)
            {
                // TODO: send error message
                logger.Warning("Game was not initialized yet: GM can't accept messages");
                return;
            }

            if (!WasGameStarted && message.MessageId != PlayerMessageId.JoinTheGame)
            {
                // TODO: send error message
                logger.Warning("Game was not started yet: GM can't accept messages other than JoinTheGame");
                return;
            }

            players.TryGetValue(message.PlayerId, out GMPlayer player);

            // logger.Information($"|{message.MessageId} | {message.Payload} | {player?.SocketId} | {player?.Team}");
            switch (message.MessageId)
            {
                case PlayerMessageId.CheckPiece:
                    await player.CheckHoldingAsync(cancellationToken);
                    break;
                case PlayerMessageId.PieceDestruction:
                    bool destroyed = await player.DestroyHoldingAsync(cancellationToken);
                    if (destroyed)
                    {
                        GeneratePiece();
                    }
                    break;
                case PlayerMessageId.Discover:
                    await player.DiscoverAsync(this, cancellationToken);

                    // TODO: send response here
                    break;
                case PlayerMessageId.GiveInfo:
                    await ForwardKnowledgeReply(message, cancellationToken);
                    break;
                case PlayerMessageId.BegForInfo:
                    await ForwardKnowledgeQuestion(message, cancellationToken);
                    break;
                case PlayerMessageId.JoinTheGame:
                {
                    JoinGamePayload payloadJoin = JsonConvert.DeserializeObject<JoinGamePayload>(message.Payload);
                    int key = message.PlayerId;
                    bool accepted = TryToAddPlayer(key, payloadJoin.TeamId);
                    JoinAnswerPayload answerJoinPayload = new JoinAnswerPayload()
                    {
                        Accepted = accepted,
                        PlayerId = key,
                    };
                    GMMessage answerJoin = new GMMessage()
                    {
                        Id = GMMessageId.JoinTheGameAnswer,
                        PlayerId = key,
                        Payload = JsonConvert.SerializeObject(answerJoinPayload),
                    };
                    await socketClient.SendAsync(answerJoin, cancellationToken);

                    if (GetPlayersCount(Team.Red) == conf.NumberOfPlayersPerTeam &&
                       GetPlayersCount(Team.Blue) == conf.NumberOfPlayersPerTeam)
                    {
                        await StartGame(cancellationToken);
                        WasGameStarted = true;
                        logger.Information("Game was started.");
                    }
                    break;
                }
                case PlayerMessageId.Move:
                {
                    MovePayload payloadMove = JsonConvert.DeserializeObject<MovePayload>(message.Payload);
                    AbstractField field = null;
                    int[] pos = player.GetPosition();
                    switch (payloadMove.Direction)
                    {
                        case Direction.N:
                            if (pos[0] + 1 < conf.Height && (player.Team == Team.Blue || pos[0] + 1 < TaskAreaEnd))
                            {
                                field = board[pos[0] + 1][pos[1]];
                            }
                            break;
                        case Direction.S:
                            if (pos[0] - 1 >= 0 && (player.Team == Team.Red || pos[0] - 1 >= conf.GoalAreaHeight))
                            {
                                field = board[pos[0] - 1][pos[1]];
                            }
                            break;
                        case Direction.E:
                            if (pos[1] + 1 < conf.Width)
                            {
                                field = board[pos[0]][pos[1] + 1];
                            }
                            break;
                        case Direction.W:
                            if (pos[1] - 1 >= 0)
                            {
                                field = board[pos[0]][pos[1] - 1];
                            }
                            break;
                    }
                    await player.MoveAsync(field, this, cancellationToken);

                    break;
                }
                case PlayerMessageId.Pick:
                    await player.PickAsync(cancellationToken);
                    break;
                case PlayerMessageId.Put:
                    (bool point, bool removed) = await player.PutAsync(cancellationToken);
                    if (point)
                    {
                        int y = player.GetPosition()[0];
                        if (y < conf.GoalAreaHeight)
                        {
                            logger.Information("RED TEAM POINT !!!");
                            redTeamPoints++;
                        }
                        else
                        {
                            logger.Information("BLUE TEAM POINT !!!");
                            blueTeamPoints++;
                        }
                        logger.Information($"by {player.Team}");
                        logger.Information($"RED: {redTeamPoints} | BLUE: {blueTeamPoints}");
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

            // TODO: isLeader flag!
            var player = new GMPlayer(key, conf, socketClient, team, log);
            return players.TryAdd(key, player);
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

        internal async Task StartGame(CancellationToken cancellationToken)
        {
            InitializePlayersPoisitions();

            int[] teamBlueIds = players.Where(p => p.Value.Team == Team.Blue).Select(p => p.Key).ToArray();
            int[] teamRedIds = players.Where(p => p.Value.Team == Team.Red).Select(p => p.Key).ToArray();
            List<Task> sendMessagesTasks = new List<Task>(players.Count);

            // Watch, you can't do anything async in foreach loop
            foreach (var p in players)
            {
                GMPlayer player = p.Value;
                StartGamePayload payload = null;
                if (player.Team == Team.Blue)
                {
                    payload = new StartGamePayload
                    {
                        AlliesIds = teamBlueIds,
                        LeaderId = teamBlueIds.First(),
                        EnemiesIds = teamRedIds,
                        TeamId = Team.Blue,
                        NumberOfPlayers = new NumberOfPlayers
                        {
                            Allies = teamBlueIds.Length,
                            Enemies = teamRedIds.Length,
                        },
                    };
                }
                else
                {
                    payload = new StartGamePayload
                    {
                        AlliesIds = teamRedIds,
                        LeaderId = teamRedIds.First(),
                        EnemiesIds = teamBlueIds,
                        TeamId = Team.Red,
                        NumberOfPlayers = new NumberOfPlayers
                        {
                            Allies = teamBlueIds.Length,
                            Enemies = teamRedIds.Length,
                        },
                    };
                }
                payload.PlayerId = p.Key;
                payload.BoardSize = new BoardSize
                {
                    Y = conf.Height,
                    X = conf.Width,
                };
                payload.GoalAreaSize = conf.GoalAreaHeight;
                payload.NumberOfPieces = conf.NumberOfPiecesOnBoard;
                payload.NumberOfGoals = conf.NumberOfGoals;
                payload.Penalties = new Penalties
                {
                    Move = conf.MovePenalty.ToString(),
                    InformationExchange = conf.AskPenalty.ToString(),
                    Discovery = conf.DiscoverPenalty.ToString(),
                    PutPiece = conf.PutPenalty.ToString(),
                    CheckForSham = conf.CheckPenalty.ToString(),
                    DestroyPiece = conf.DestroyPenalty.ToString(),
                };
                payload.ShamPieceProbability = conf.ShamPieceProbability;
                payload.Position = new Position
                {
                    Y = player[0],
                    X = player[1],
                };

                var message = new GMMessage
                {
                    Id = GMMessageId.StartGame,
                    PlayerId = p.Key,
                    Payload = payload.Serialize(),
                };

                sendMessagesTasks.Add(socketClient.SendAsync(message, cancellationToken));
            }

            await Task.WhenAll(sendMessagesTasks);
            WasGameStarted = true;
        }

        private void InitializePlayersPoisitions()
        {
            var rand = new Random();
            foreach (var p in players)
            {
                GMPlayer player = p.Value;
                (int y1, int y2) = GetBoundaries(player.Team);
                int y = rand.Next(y1, y2);
                int x = rand.Next(0, conf.Width);

                AbstractField pos = board[y][x];
                while (!pos.MoveHere(player))
                {
                    ++x;
                    if (x == conf.Width)
                    {
                        x = 0;
                        ++y;
                        if (y == y2)
                        {
                            y = y1;
                        }
                    }

                    pos = board[y][x];
                }
            }
        }

        private (int y1, int y2) GetBoundaries(Team team)
        {
            if (team == Team.Red)
            {
                return (0, TaskAreaEnd);
            }

            return (conf.GoalAreaHeight, conf.Height);
        }

        private void InitializeBoard()
        {
            board = new AbstractField[conf.Height][];
            for (int i = 0; i < board.Length; ++i)
            {
                board[i] = new AbstractField[conf.Width];
            }

            int goalFields = 0;
            AbstractField NonGoalOrGoalFieldGenerator(int y, int x)
            {
                if (goalFields < conf.NumberOfGoals)
                {
                    ++goalFields;
                    return new GoalField(y, x);
                }
                return new NonGoalField(y, x);
            }
            for (int rowIt = 0; rowIt < conf.GoalAreaHeight; ++rowIt)
            {
                FillBoardRow(rowIt, NonGoalOrGoalFieldGenerator);
            }

            Func<int, int, AbstractField> taskFieldGenerator = (int y, int x) => new TaskField(y, x);
            int secondGoalAreaStart = conf.Height - conf.GoalAreaHeight;
            for (int rowIt = conf.GoalAreaHeight; rowIt < secondGoalAreaStart; ++rowIt)
            {
                FillBoardRow(rowIt, taskFieldGenerator);
            }

            goalFields = 0;
            for (int rowIt = secondGoalAreaStart; rowIt < conf.Height; ++rowIt)
            {
                FillBoardRow(rowIt, NonGoalOrGoalFieldGenerator);
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
            TimeSpan cancellationTimespan = TimeSpan.FromMinutes(2);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PlayerMessage message = await queue.ReceiveAsync(cancellationTimespan, cancellationToken);
                    await AcceptMessage(message, cancellationToken);
                    if (conf.NumberOfGoals == blueTeamPoints || conf.NumberOfGoals == redTeamPoints)
                    {
                        await EndGame(cancellationToken);
                        break;
                    }
                }
                catch (TimeoutException e)
                {
                    logger.Warning($"Message retrieve was cancelled: {e.Message}");

                    // TODO: change it with switch to SocketClient
                    if (!socketClient.IsOpen)
                    {
                        logger.Error("No open connection. Exiting.");
                        lifetime.StopApplication();
                        break;
                    }
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
                var (dir, y, x) = neighbourCoordinates[i];
                if (y >= 0 && y < conf.Height && x >= 0 && x < conf.Width)
                    distances[i] = int.MaxValue;
                else
                    distances[i] = -1;
            }

            int secondGoalAreaStart = conf.Height - conf.GoalAreaHeight;
            for (int i = conf.GoalAreaHeight; i < secondGoalAreaStart; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    if (board[i][j].ContainsPieces() && board[i][j].CanPick())
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
                discoveryResult.Add(neighbourCoordinates[i].dir, distances[i]);
            }
            return discoveryResult;
        }

        internal int FindClosestPiece(AbstractField field)
        {
            int[] center = field.GetPosition();
            int distance = int.MaxValue;
            int secondGoalAreaStart = conf.Height - conf.GoalAreaHeight;
            for (int i = conf.GoalAreaHeight; i < secondGoalAreaStart; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    if (board[i][j].ContainsPieces() && board[i][j].CanPick())
                    {
                            int manhattanDistance = Math.Abs(center[0] - i) + Math.Abs(center[1] - j);
                            if (manhattanDistance < distance)
                                distance = manhattanDistance;
                    }
                }
            }
            return distance;
        }

        private void GeneratePiece()
        {
            var rand = new Random();
            bool isSham = rand.Next(0, 101) < conf.ShamPieceProbability * 100;
            AbstractPiece piece;
            if (isSham)
            {
                piece = new ShamPiece();
            }
            else
            {
                piece = new NormalPiece();
            }

            (int y, int x) = GenerateCoordinatesInTaskArea(rand);
            board[y][x].Put(piece);
        }

        private (int y, int x) GenerateCoordinatesInTaskArea(Random rand)
        {
            int taskAreaStart = conf.GoalAreaHeight;
            int yCoord = rand.Next(taskAreaStart, TaskAreaEnd);
            int xCoord = rand.Next(0, conf.Width);

            return (yCoord, xCoord);
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
                AskingId = playerMessage.PlayerId,
                Leader = players[playerMessage.PlayerId].IsLeader,
                TeamId = players[playerMessage.PlayerId].Team,
            };
            GMMessage gmMessage = new GMMessage()
            {
                Id = GMMessageId.BegForInfoForwarded,
                PlayerId = begPayload.AskedPlayerId,
                Payload = payload.Serialize(),
            };

            legalKnowledgeReplies.Add((begPayload.AskedPlayerId, playerMessage.PlayerId));
            await socketClient.SendAsync(gmMessage, cancellationToken);
        }

        private async Task ForwardKnowledgeReply(PlayerMessage playerMessage, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            GiveInfoPayload payload = JsonConvert.DeserializeObject<GiveInfoPayload>(playerMessage.Payload);
            if (legalKnowledgeReplies.Contains((playerMessage.PlayerId, payload.RespondToId)))
            {
                legalKnowledgeReplies.Remove((playerMessage.PlayerId, payload.RespondToId));
                GiveInfoForwardedPayload answerPayload = new GiveInfoForwardedPayload()
                {
                    AnsweringId = playerMessage.PlayerId,
                    Distances = payload.Distances,
                    RedTeamGoalAreaInformations = payload.RedTeamGoalAreaInformations,
                    BlueTeamGoalAreaInformations = payload.BlueTeamGoalAreaInformations,
                };
                GMMessage answer = new GMMessage()
                {
                    Id = GMMessageId.GiveInfoForwarded,
                    PlayerId = payload.RespondToId,
                    Payload = answerPayload.Serialize(),
                };
                await socketClient.SendAsync(answer, cancellationToken);
            }
        }

        internal async Task EndGame(CancellationToken cancellationToken)
        {
            var winner = redTeamPoints > blueTeamPoints ? Team.Red : Team.Blue;
            logger.Information($"The winner is team {winner}");
            var payload = new EndGamePayload()
            {
                Winner = winner,
            };
            
            List<GMMessage> messages = new List<GMMessage>();
            foreach (var p in players)
            {
               messages.Add(new GMMessage(GMMessageId.EndGame, p.Key, payload));
            }
            await socketClient.SendToAllAsync(messages, cancellationToken);
            logger.Information("Sent endGame to all.");

            await Task.Delay(4000);
            lifetime.StopApplication();
        }
    }
}
