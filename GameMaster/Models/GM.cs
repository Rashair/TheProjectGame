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
        private readonly ILogger logger;
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
        private bool forceEndGame;

        public bool WasGameInitialized { get; private set; }

        public bool WasGameStarted { get; private set; }

        public bool WasGameFinished { get; private set; }

        public int SecondGoalAreaStart { get => conf.Height - conf.GoalAreaHeight; }

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
                        AlliesIDs = teamBlueIds,
                        LeaderId = teamBlueIds.First(),
                        EnemiesIDs = teamRedIds,
                        TeamID = Team.Blue,
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
                        AlliesIDs = teamRedIds,
                        LeaderId = teamRedIds.First(),
                        EnemiesIDs = teamBlueIds,
                        TeamID = Team.Red,
                        NumberOfPlayers = new NumberOfPlayers
                        {
                            Allies = teamBlueIds.Length,
                            Enemies = teamRedIds.Length,
                        },
                    };
                }
                payload.AgentID = p.Key;
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
                    PickupPiece = conf.PickupPenalty,
                    CheckPiece = conf.CheckPenalty,
                    DestroyPiece = conf.DestroyPenalty,
                    PutPiece = conf.PutPenalty,
                    PrematureRequest = conf.PrematureRequestPenalty
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
                    Payload = payload,
                };
                logger.Verbose(MessageLogger.Sent(message));
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
                    logger.Verbose(MessageLogger.Received(message));
                    await AcceptMessage(message, cancellationToken);
                    if (conf.NumberOfGoals == blueTeamPoints || conf.NumberOfGoals == redTeamPoints)
                    {
                        await EndGame(cancellationToken);
                        break;
                    }
                    if (forceEndGame)
                    {
                        logger.Warning("CS disconnected, exiting.");
                        lifetime.StopApplication();
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

        private async Task SendInformationExchangeMessage(GMMessageId messageId, int agentID, bool wasSent, CancellationToken cancellationToken)
        {
            GMMessage confirmationMessage = new GMMessage(messageId,
                    agentID, new InformationExchangePayload() { WasSent = wasSent });
            logger.Verbose("Sent message." + MessageLogger.Get(confirmationMessage));
            await socketClient.SendAsync(confirmationMessage, cancellationToken);
        }

        public async Task AcceptMessage(PlayerMessage message, CancellationToken cancellationToken)
        {
            if (!WasGameInitialized)
            {
                // TODO: send error message
                logger.Warning("Game was not initialized yet: GM can't accept messages");
                return;
            }

            if (!WasGameStarted && message.MessageID != PlayerMessageId.JoinTheGame &&
                message.MessageID != PlayerMessageId.Disconnected &&
                message.MessageID != PlayerMessageId.CSDisconnected)
            {
                // TODO: send error message
                logger.Warning("Game was not started yet: GM can't accept messages other than JoinTheGame," +
                    "Disconnected or CSDisconnected");
                return;
            }

            players.TryGetValue(message.AgentID, out GMPlayer player);
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
                    if (player == null)
                    {
                        await SendInformationExchangeMessage(GMMessageId.InformationExchangeResponse, message.AgentID, false, cancellationToken);
                    }
                    else
                    {
                        (int agentID, bool? res) forwardReply = await player.ForwardKnowledgeReply(message, cancellationToken, legalKnowledgeReplies);
                        if (forwardReply.res != null)
                        {
                            await SendInformationExchangeMessage(GMMessageId.InformationExchangeResponse, forwardReply.agentID, (bool)forwardReply.res, cancellationToken);
                        }
                    }
                    break;
                case PlayerMessageId.BegForInfo:
                    if (player == null)
                    {
                        await SendInformationExchangeMessage(GMMessageId.InformationExchangeRequest, message.AgentID, false, cancellationToken);
                    }
                    else
                    {
                        (int agentID, bool? res) forwardQuestion = await player.ForwardKnowledgeQuestion(message, cancellationToken, players, legalKnowledgeReplies);
                        if (forwardQuestion.res != null)
                        {
                            await SendInformationExchangeMessage(GMMessageId.InformationExchangeRequest, forwardQuestion.agentID, (bool)forwardQuestion.res, cancellationToken);
                        }
                    }
                    break;
                case PlayerMessageId.Disconnected:
                {
                    int key = message.AgentID;
                    players.Remove(key);
                    logger.Verbose($"Player {key} disconnected");
                    if (WasGameStarted)
                    {
                        if (player.Team == Team.Blue)
                        {
                            redTeamPoints = conf.NumberOfGoals;
                        }
                        else
                        {
                            blueTeamPoints = conf.NumberOfGoals;
                        }
                    }
                    break;
                }
                case PlayerMessageId.CSDisconnected:
                    forceEndGame = true;
                    break;
                case PlayerMessageId.JoinTheGame:
                {
                    JoinGamePayload payloadJoin = message.DeserializePayload<JoinGamePayload>();
                    int key = message.AgentID;
                    bool accepted = TryToAddPlayer(key, payloadJoin.TeamId);
                    JoinAnswerPayload answerJoinPayload = new JoinAnswerPayload()
                    {
                        Accepted = accepted,
                        AgentID = key,
                    };
                    GMMessage answerJoin = new GMMessage()
                    {
                        MessageID = GMMessageId.JoinTheGameAnswer,
                        AgentID = key,
                        Payload = answerJoinPayload,
                    };

                    await socketClient.SendAsync(answerJoin, cancellationToken);
                    logger.Verbose(MessageLogger.Sent(answerJoin));

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
                    MovePayload payloadMove = message.DeserializePayload<MovePayload>();
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
                    (PutEvent putEvent, bool wasPieceRemoved) = await player.PutAsync(cancellationToken);
                    if (putEvent == PutEvent.NormalOnGoalField)
                    {
                        int y = player.GetPosition()[0];
                        string teamStr = "";
                        if (y < conf.GoalAreaHeight)
                        {
                            teamStr = "RED";
                            redTeamPoints++;
                        }
                        else if (y >= SecondGoalAreaStart)
                        {
                            teamStr = "BLUE";
                            blueTeamPoints++;
                        }
                        else
                        {
                            logger.Error("Critical error, goal on task field.");
                        }
                        logger.Information($"{teamStr} TEAM POINT !!!\n" +
                            $"    by {player.Team}");
                        logger.Information($"RED: {redTeamPoints} | BLUE: {blueTeamPoints}");
                    }

                    if (wasPieceRemoved)
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

        internal Dictionary<Direction, int?> Discover(AbstractField field)
        {
            int[] center = field.GetPosition();
            var neighbourCoordinates = DirectionExtensions.GetCoordinatesAroundCenter(center);

            int?[] distances = new int?[neighbourCoordinates.Length];
            for (int i = 0; i < distances.Length; i++)
            {
                var (dir, y, x) = neighbourCoordinates[i];
                if (y >= 0 && y < conf.Height && x >= 0 && x < conf.Width)
                    distances[i] = int.MaxValue;
                else
                    distances[i] = null;
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

            Dictionary<Direction, int?> discoveryResult = new Dictionary<Direction, int?>();
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
            {
                logger.Verbose(MessageLogger.Sent(messages[i]));
            }
            logger.Information("Sent endGame to all.");

            await Task.Delay(4000);
            WasGameFinished = true;

            lifetime.StopApplication();
        }
    }
}
