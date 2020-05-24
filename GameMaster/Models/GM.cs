using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using GameMaster.Models.Fields;
using GameMaster.Models.Messages;
using GameMaster.Models.Payloads;
using GameMaster.Models.Pieces;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads.CommunicationServerPayloads;
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly ILogger log;
        private readonly ILogger logger;
        private readonly IApplicationLifetime lifetime;
        private readonly BufferBlock<Message> queue;
        private readonly ISocketClient<Message, Message> socketClient;
        private readonly HashSet<(int recipient, int sender)> legalKnowledgeReplies;
        private readonly Dictionary<int, GMPlayer> players;
        private readonly GameConfiguration conf;
        private readonly Random rand;
        private AbstractField[][] board;
        private readonly WebSocketManager<ClientMessage> guiManager;

        private int redTeamPoints;
        private int blueTeamPoints;
        private bool forceEndGame;

        public bool WasGameInitialized { get; private set; }

        public bool WasGameStarted { get; private set; }

        public bool WasGameFinished { get; private set; }

        public int SecondGoalAreaStart { get => conf.Height - conf.GoalAreaHeight; }

        public GM(IApplicationLifetime lifetime, GameConfiguration conf,
            BufferBlock<Message> queue, ISocketClient<Message, Message> socketClient,
            ILogger log, WebSocketManager<ClientMessage> guiManager = null)
        {
            this.log = log;
            this.logger = log.ForContext<GM>();
            this.lifetime = lifetime;
            this.conf = conf;
            this.queue = queue;
            this.socketClient = socketClient;
            this.guiManager = guiManager;

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

        private async Task SendPieceGUI(int x, int y, CancellationToken cancellationToken)
        {
            if (!(guiManager is null) && board[y][x].ContainsPieces())
            {
                ClientMessage clientMessage = new ClientMessage
                {
                    Type = "Piece",
                    Payload = new PieceClientPayload
                    {
                        X = x,
                        Y = y
                    }
                };
                await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);
            }
        }

        private async Task SendInitGUI(CancellationToken cancellationToken)
        {
            if (!(guiManager is null))
            {
                List<int[]> goalFields = new List<int[]>();
                for (int y = 0; y < conf.Height; ++y)
                {
                    for (int x = 0; x < conf.Width; ++x)
                    {
                        if (board[y][x] is GoalField)
                        {
                            goalFields.Add(new int[] { x, y });
                        }
                    }
                }
                ClientMessage clientMessage = new ClientMessage
                {
                    Type = "Init",
                    Info = "Game started",
                    Payload = new InitClientPayload
                    {
                        Width = conf.Width,
                        Height = conf.Height,
                        FirstGoalLevel = conf.GoalAreaHeight - 1,
                        SecondGoalLevel = SecondGoalAreaStart,
                        Goals = goalFields
                    },
                };
                await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);

                for (int y = conf.GoalAreaHeight; y < SecondGoalAreaStart; ++y)
                {
                    for (int x = 0; x < conf.Width; ++x)
                    {
                        await SendPieceGUI(x, y, cancellationToken);
                    }
                }

                foreach (var p in players)
                {
                    GMPlayer player = p.Value;
                    clientMessage = new ClientMessage
                    {
                        Type = "Player",
                        Payload = new PlayerClientPayload
                        {
                            Id = p.Key,
                            Team = (player.Team == Team.Blue) ? 1 : 2,
                            X = player[1],
                            Y = player[0],
                            IsLeader = player.IsLeader
                        }
                    };
                    await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);
                }
            }
        }

        internal async Task StartGame(CancellationToken cancellationToken)
        {
            var gmInitializer = new GMInitializer(conf, board);
            gmInitializer.InitializePlayersPoisitions(players);

            await SendInitGUI(cancellationToken);

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
                        LeaderID = teamBlueIds.First(),
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
                        LeaderID = teamRedIds.First(),
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
                    Discovery = conf.DiscoveryPenalty,
                    Pickup = conf.PickupPenalty,
                    CheckForSham = conf.CheckForShamPenalty,
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

                var message = new Message
                {
                    MessageID = MessageID.StartGame,
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
                    Message message = await queue.ReceiveAsync(cancellationTimespan, cancellationToken);
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

        private async Task SendInformationExchangeMessage(MessageID messageId, int agentID, bool wasSent, CancellationToken cancellationToken)
        {
            Message confirmationMessage = new Message(messageId,
                    agentID, new InformationExchangePayload() { Succeeded = wasSent });
            logger.Verbose("Sent message." + confirmationMessage.GetDescription());
            await socketClient.SendAsync(confirmationMessage, cancellationToken);
        }

        public async Task AcceptMessage(Message message, CancellationToken cancellationToken)
        {
            if (!WasGameInitialized)
            {
                // TODO: send error message
                logger.Warning("Game was not initialized yet: GM can't accept messages");
                return;
            }

            if (!WasGameStarted && message.MessageID != MessageID.JoinTheGame &&
                message.MessageID != MessageID.PlayerDisconnected &&
                message.MessageID != MessageID.CSDisconnected)
            {
                // TODO: send error message
                logger.Warning("Game was not started yet: GM can't accept messages other than JoinTheGame," +
                    "Disconnected or CSDisconnected");
                return;
            }

            int agentID = message.AgentID.Value;
            players.TryGetValue(agentID, out GMPlayer player);
            switch (message.MessageID)
            {
                case MessageID.CheckPiece:
                    await player.CheckHoldingAsync(cancellationToken);
                    break;
                case MessageID.PieceDestruction:
                    bool destroyed = await player.DestroyHoldingAsync(cancellationToken);
                    if (destroyed)
                    {
                        (int y, int x) = GeneratePieceInside();
                        await SendPieceGUI(x, y, cancellationToken);
                    }
                    break;
                case MessageID.Discover:
                    await player.DiscoverAsync(Discover, cancellationToken);
                    break;
                case MessageID.GiveInfo:
                    if (player == null)
                    {
                        await SendInformationExchangeMessage(MessageID.InformationExchangeResponse, agentID, false, cancellationToken);
                    }
                    else
                    {
                        (int agentID, bool? res) forwardReply = await player.ForwardKnowledgeReply(message, cancellationToken, legalKnowledgeReplies);
                        if (forwardReply.res != null)
                        {
                            await SendInformationExchangeMessage(MessageID.InformationExchangeResponse, forwardReply.agentID, (bool)forwardReply.res, cancellationToken);
                        }
                    }
                    break;
                case MessageID.BegForInfo:
                    if (player == null)
                    {
                        await SendInformationExchangeMessage(MessageID.InformationExchangeRequest, agentID, false, cancellationToken);
                    }
                    else
                    {
                        (int agentID, bool? res) forwardQuestion = await player.ForwardKnowledgeQuestion(message, cancellationToken, players, legalKnowledgeReplies);
                        if (forwardQuestion.res != null)
                        {
                            await SendInformationExchangeMessage(MessageID.InformationExchangeRequest, forwardQuestion.agentID, (bool)forwardQuestion.res, cancellationToken);
                        }
                    }
                    break;
                case MessageID.PlayerDisconnected:
                {
                    DisconnectPayload payloadDisconnect = (DisconnectPayload)message.Payload;
                    int key = payloadDisconnect.AgentID;
                    players.TryGetValue(key, out player);
                    players.Remove(key);
                    logger.Information($"Player {key} disconnected");

                    if (!(guiManager is null))
                    {
                        ClientMessage clientMessage = new ClientMessage("Disconnected", $"Player {key} disconnected, team {player.Team}");
                        await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);
                    }

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
                case MessageID.CSDisconnected:
                    forceEndGame = true;
                    break;
                case MessageID.JoinTheGame:
                {
                    if (!WasGameStarted)
                    {
                        JoinGamePayload payloadJoin = (JoinGamePayload)message.Payload;
                        int key = agentID;
                        bool accepted = TryToAddPlayer(key, payloadJoin.TeamID);
                        JoinAnswerPayload answerJoinPayload = new JoinAnswerPayload()
                        {
                            Accepted = accepted,
                            AgentID = key,
                        };
                        Message answerJoin = new Message()
                        {
                            MessageID = MessageID.JoinTheGameAnswer,
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
                    }
                    break;
                }
                case MessageID.Move:
                {
                    MovePayload payloadMove = (MovePayload)message.Payload;
                    AbstractField field = null;
                    int[] pos = player.GetPosition();
                    switch (payloadMove.Direction)
                    {
                        case Direction.N:
                            if (pos[0] + 1 < conf.Height && (player.Team == Team.Red || pos[0] + 1 < SecondGoalAreaStart))
                            {
                                field = board[pos[0] + 1][pos[1]];
                            }
                            break;
                        case Direction.S:
                            if (pos[0] - 1 >= 0 && (player.Team == Team.Blue || pos[0] - 1 >= conf.GoalAreaHeight))
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
                    await player.MoveAsync(field, FindClosestPiece, cancellationToken);
                    break;
                }
                case MessageID.Pick:
                    await player.PickAsync(cancellationToken);
                    break;
                case MessageID.Put:
                    (PutEvent putEvent, bool wasPieceRemoved) = await player.PutAsync(cancellationToken);
                    if (putEvent == PutEvent.NormalOnGoalField)
                    {
                        int y = player.GetPosition()[0];
                        string teamStr = "";
                        if (y < conf.GoalAreaHeight)
                        {
                            teamStr = "BLUE";
                            ++blueTeamPoints;
                        }
                        else if (y >= SecondGoalAreaStart)
                        {
                            teamStr = "RED";
                            ++redTeamPoints;
                        }
                        else
                        {
                            logger.Error("Critical error, goal on task field.");
                        }
                        logger.Information($"{teamStr} TEAM POINT !!!\n" +
                            $"    by {player.Team}");
                        logger.Information($"BLUE: {blueTeamPoints} | RED: {redTeamPoints}");
                    }

                    if (wasPieceRemoved)
                    {
                        (int y, int x) = GeneratePieceInside();
                        await SendPieceGUI(x, y, cancellationToken);
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
            var player = new GMPlayer(key, conf, socketClient, team, log, guiManager: guiManager);
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

        internal int? FindClosestPiece(AbstractField field)
        {
            Position center = field.GetPosition();
            int distance = int.MaxValue;
            for (int i = conf.GoalAreaHeight; i < SecondGoalAreaStart; i++)
            {
                for (int j = 0; j < conf.Width; j++)
                {
                    if (board[i][j].ContainsPieces() && board[i][j].CanPick())
                    {
                        int manhattanDistance = Math.Abs(center.Y - i) + Math.Abs(center.X - j);
                        if (distance > manhattanDistance)
                        {
                            distance = manhattanDistance;
                        }
                    }
                }
            }

            return distance;
        }

        private (int, int) GeneratePieceInside()
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
            return (y, x);
        }

        private void GeneratePiece()
        {
            GeneratePieceInside();
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

            if (!(guiManager is null))
            {
                ClientMessage clientMessage = new ClientMessage("End", $"The end, the winner is team {winner}");
                await guiManager.SendMessageToAllAsync(clientMessage, cancellationToken);
            }

            var payload = new EndGamePayload()
            {
                Winner = winner,
            };

            List<Message> messages = new List<Message>();
            foreach (var p in players)
            {
                var msg = new Message(MessageID.EndGame, p.Key, payload);
                messages.Add(msg);
                logger.Verbose(MessageLogger.Sent(msg));
            }
            await socketClient.SendToAllAsync(messages, cancellationToken);
            logger.Information("Sent endGame to all.");

            await Task.Delay(4000);
            WasGameFinished = true;

            lifetime.StopApplication();
        }
    }
}
