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
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly ILogger log;
        private ILogger logger;
        private readonly IApplicationLifetime lifetime;
        private readonly BufferBlock<PlayerMessage> queue;
        private readonly ISocketClient<PlayerMessage, GMMessage> socketClient;
        private readonly HashSet<(int recipient, int sender)> legalKnowledgeReplies;
        private readonly Dictionary<int, GMPlayer> players;
        private readonly GameConfiguration conf;
        private readonly Random rand;
        private AbstractField[][] board;

        private int redTeamPoints;
        private int blueTeamPoints;
    
        public bool WasGameInitialized { get; private set; }

        public bool WasGameStarted { get; private set; }

        public bool WasGameFinished { get; private set; }

        public int SecondGoalAreaStart { get => conf.Height - conf.GoalAreaHeight; }

        public bool Verbose { get; set; }

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
            rand = new Random();
            Verbose = conf.Verbose;
        }

        internal bool InitGame()
        {
            (bool valid, string msg) = conf.IsValid();
            if (!valid)
            {
                logger.Error(msg);
                return false;
            }
            board = new AbstractField[conf.Height][];
            var gmInitializer = new GMInitializer(conf, board);
            gmInitializer.InitializeBoard();
            gmInitializer.GenerateAllPieces(GeneratePiece);
            WasGameInitialized = true;
            logger.Information("Game was initialized.");
            return true;
        }

        internal async Task StartGame(CancellationToken cancellationToken)
        {
            var gmInitializer = new GMInitializer(conf, board);
            gmInitializer.InitializePlayersPoisitions(players);

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
                    Move = conf.MovePenalty,
                    Ask = conf.AskPenalty,
                    Response = conf.ResponsePenalty,
                    Discover = conf.DiscoverPenalty,
                    PickPiece = conf.PickPenalty,
                    CheckPiece = conf.CheckPenalty,
                    DestroyPiece = conf.DestroyPenalty,
                    PutPiece = conf.PutPenalty,
                };
                payload.ShamPieceProbability = conf.ShamPieceProbability;
                payload.Position = new Position
                {
                    Y = player[0],
                    X = player[1],
                };

                var message = new GMMessage
                {
                    MessageID = GMMessageId.StartGame,
                    AgentID = p.Key,
                    Payload = payload.Serialize(),
                };
                logger.Verbose("Sent message." + MessageLogger.Get(message));
                sendMessagesTasks.Add(socketClient.SendAsync(message, cancellationToken));
            }

            await Task.WhenAll(sendMessagesTasks);
            WasGameStarted = true;
        }

        internal async Task Work(CancellationToken cancellationToken)
        {
            TimeSpan cancellationTimespan = TimeSpan.FromMinutes(2);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PlayerMessage message = await queue.ReceiveAsync(cancellationTimespan, cancellationToken);
                    logger.Verbose("Received message. " + MessageLogger.Get(message));
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

                    if (!socketClient.IsOpen)
                    {
                        logger.Error("No open connection. Exiting.");
                        lifetime.StopApplication();
                        break;
                    }
                }
            }
        }

        public async Task AcceptMessage(PlayerMessage message, CancellationToken cancellationToken)
        {
            if (!WasGameInitialized)
            {
                // TODO: send error message
                logger.Warning("Game was not initialized yet: GM can't accept messages");
                return;
            }

            if (!WasGameStarted && message.MessageID != PlayerMessageId.JoinTheGame)
            {
                // TODO: send error message
                logger.Warning("Game was not started yet: GM can't accept messages other than JoinTheGame");
                return;
            }

            players.TryGetValue(message.AgentID, out GMPlayer player);
           
            // logger.Information($"|{message.MessageId} | {message.Payload} | | {player?.Team}");
            switch (message.MessageID)
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
                    int key = message.AgentID;
                    bool accepted = TryToAddPlayer(key, payloadJoin.TeamId);
                    JoinAnswerPayload answerJoinPayload = new JoinAnswerPayload()
                    {
                        Accepted = accepted,
                        PlayerId = key,
                    };
                    GMMessage answerJoin = new GMMessage()
                    {
                        MessageID = GMMessageId.JoinTheGameAnswer,
                        AgentID = key,
                        Payload = JsonConvert.SerializeObject(answerJoinPayload),
                    };
                    logger.Verbose("Sent message." + MessageLogger.Get(answerJoin));
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
                            if (pos[0] + 1 < conf.Height && (player.Team == Team.Blue || pos[0] + 1 < SecondGoalAreaStart))
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
                    (bool? point, bool removed) = await player.PutAsync(cancellationToken);
                    if (point == true)
                    {
                        int y = player.GetPosition()[0];
                        string teamStr;
                        if (y < conf.GoalAreaHeight)
                        {
                            teamStr = "RED";
                            redTeamPoints++;
                        }
                        else
                        {
                            teamStr = "BLUE";
                            blueTeamPoints++;
                        }
                        logger.Information($"{teamStr} TEAM POINT !!!\n" +
                            $"    by {player.Team}");
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

            for (int i = conf.GoalAreaHeight; i < SecondGoalAreaStart; i++)
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
            for (int i = conf.GoalAreaHeight; i < SecondGoalAreaStart; i++)
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

            (int y, int x) = GenerateCoordinatesInTaskArea();
            board[y][x].Put(piece);
        }

        private (int y, int x) GenerateCoordinatesInTaskArea()
        {
            int taskAreaStart = conf.GoalAreaHeight;
            int yCoord = rand.Next(taskAreaStart, SecondGoalAreaStart);
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
                AskingId = playerMessage.AgentID,
                Leader = players[playerMessage.AgentID].IsLeader,
                TeamId = players[playerMessage.AgentID].Team,
            };
            GMMessage gmMessage = new GMMessage()
            {
                MessageID = GMMessageId.BegForInfoForwarded,
                AgentID = begPayload.AskedPlayerId,
                Payload = payload.Serialize(),
            };

            legalKnowledgeReplies.Add((begPayload.AskedPlayerId, playerMessage.AgentID));
            logger.Verbose("Sent message." + MessageLogger.Get(gmMessage));
            await socketClient.SendAsync(gmMessage, cancellationToken);
        }

        private async Task ForwardKnowledgeReply(PlayerMessage playerMessage, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            GiveInfoPayload payload = JsonConvert.DeserializeObject<GiveInfoPayload>(playerMessage.Payload);
            if (legalKnowledgeReplies.Contains((playerMessage.AgentID, payload.RespondToId)))
            {
                legalKnowledgeReplies.Remove((playerMessage.AgentID, payload.RespondToId));
                GiveInfoForwardedPayload answerPayload = new GiveInfoForwardedPayload()
                {
                    AnsweringId = playerMessage.AgentID,
                    Distances = payload.Distances,
                    RedTeamGoalAreaInformations = payload.RedTeamGoalAreaInformations,
                    BlueTeamGoalAreaInformations = payload.BlueTeamGoalAreaInformations,
                };
                GMMessage answer = new GMMessage()
                {
                    MessageID = GMMessageId.GiveInfoForwarded,
                    AgentID = payload.RespondToId,
                    Payload = answerPayload.Serialize(),
                };
                logger.Verbose("Sent message." + MessageLogger.Get(answer));
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
            for (int i = 0; i < messages.Count; i++)
                logger.Verbose("Sent message." + MessageLogger.Get(messages[i]));
            logger.Information("Sent endGame to all.");
            WasGameFinished = true;

            await Task.Delay(4000);
            lifetime.StopApplication();
        }
    }
}
