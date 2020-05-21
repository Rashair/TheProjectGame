using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Newtonsoft.Json;
using Player.Models.Strategies;
using Player.Models.Strategies.Utils;
using Serilog;
using Shared.Clients;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads;
using Shared.Payloads.GMPayloads;
using Shared.Payloads.PlayerPayloads;

namespace Player.Models
{
    public class Player
    {
        private readonly BufferBlock<Message> queue;
        private readonly ISocketClient<Message, Message> client;
        private readonly ILogger logger;

        private int id;
        private int penaltyTime;
        private readonly IStrategy strategy;
        private bool isWorking;
        private readonly PlayerConfiguration conf;
        private Team? winner;

        public Player(PlayerConfiguration conf, BufferBlock<Message> queue, ISocketClient<Message,
            Message> client, ILogger log)
        {
            this.conf = conf;
            this.strategy = StrategyFactory.Create((StrategyEnum)conf.Strategy);
            this.queue = queue;
            this.client = client;
            this.logger = log.ForContext<Player>();
            this.Team = conf.TeamID;
            this.Position = (-1, -1);
        }

        public int PreviousDistToPiece { get; private set; }

        public Penalties PenaltiesTimes { get; private set; }

        public int[] EnemiesIds { get; private set; }

        public NumberOfPlayers NumberOfPlayers { get; private set; }

        public int NumberOfPieces { get; private set; }

        public int NumberOfGoals { get; private set; }

        public float ShamPieceProbability { get; private set; }

        public Team Team { get; private set; }

        public bool IsLeader { get; private set; }

        public bool HasPiece { get; private set; }

        public bool? IsHeldPieceSham { get; private set; }

        public Field[,] Board { get; private set; }

        public (int y, int x) Position { get; private set; }

        public List<int> WaitingPlayers { get; private set; }

        public int[] TeamMatesIds { get; private set; }

        public int LeaderId { get; private set; }

        public (int y, int x) BoardSize { get; private set; }

        public int GoalAreaSize { get; private set; }

        internal async Task Start(CancellationToken cancellationToken)
        {
            isWorking = true;
            MessageID messageID = MessageID.Unknown;
            while (messageID != MessageID.JoinTheGameAnswer && isWorking &&
                !cancellationToken.IsCancellationRequested)
            {
                messageID = await AcceptMessage(cancellationToken);
            }
            while (messageID != MessageID.StartGame && isWorking && !cancellationToken.IsCancellationRequested)
            {
                messageID = await AcceptMessage(cancellationToken);
            }

            if (isWorking)
            {
                logger.Information("Player starting game\n" +
                    $"Team: {conf.TeamID}, strategy: {conf.Strategy}".PadLeft(12));

                await Work(cancellationToken);
            }

            await client.CloseAsync(cancellationToken);
        }

        public async Task JoinTheGame(CancellationToken cancellationToken)
        {
            JoinGamePayload payload = new JoinGamePayload()
            {
                TeamID = Team,
            };
            Message message = new Message()
            {
                MessageID = MessageID.JoinTheGame,
                Payload = payload,
            };
            await Communicate(message, cancellationToken);
            logger.Information("Sent JoinTheGame message");
        }

        internal async Task Work(CancellationToken cancellationToken)
        {
            while (isWorking && !cancellationToken.IsCancellationRequested)
            {
                await MakeDecisionFromStrategy(cancellationToken);
                await AcceptMessage(cancellationToken);
                await Penalty(cancellationToken);
            }
        }

        internal void StopWorking()
        {
            isWorking = false;
            logger.Warning($"Stopped player: {Team}");
        }

        public async Task Move(Direction direction, CancellationToken cancellationToken)
        {
            MovePayload payload = new MovePayload()
            {
                Direction = direction,
            };
            Message message = new Message()
            {
                MessageID = MessageID.Move,
                AgentID = id,
                Payload = payload,
            };
            await Communicate(message, cancellationToken);
        }

        public async Task Put(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            Message message = new Message()
            {
                MessageID = MessageID.Put,
                AgentID = id,
                Payload = payload,
            };
            await Communicate(message, cancellationToken);
        }

        public async Task BegForInfo(CancellationToken cancellationToken)
        {
            Message message = new Message()
            {
                MessageID = MessageID.BegForInfo,
                AgentID = id,
            };

            Random rnd = new Random();
            int index = rnd.Next(0, TeamMatesIds.Length - 1);
            if (TeamMatesIds[index] == id)
            {
                index = (index + 1) % TeamMatesIds.Length;
            }

            BegForInfoPayload payload = new BegForInfoPayload()
            {
                AskedAgentID = TeamMatesIds[index],
            };

            message.Payload = payload;

            await Communicate(message, cancellationToken);
        }

        public async Task GiveInfo(CancellationToken cancellationToken, bool toLeader = false)
        {
            if (WaitingPlayers.Count < 1 && !toLeader)
                return;

            Message message = new Message()
            {
                MessageID = MessageID.GiveInfo,
                AgentID = id,
            };

            GiveInfoPayload response = new GiveInfoPayload();
            if (toLeader)
            {
                response.RespondToID = LeaderId;
            }
            else
            {
                // TODO: different algorithm - whole list is shifted, maybe from end?
                response.RespondToID = WaitingPlayers[0];
                WaitingPlayers.RemoveAt(0);
            }

            response.Distances = new int[BoardSize.y, BoardSize.x];
            response.RedTeamGoalAreaInformations = new GoalInfo[BoardSize.y, BoardSize.x];
            response.BlueTeamGoalAreaInformations = new GoalInfo[BoardSize.y, BoardSize.x];

            for (int i = 0; i < Board.Length; ++i)
            {
                int row = i / BoardSize.x;
                int col = i % BoardSize.x;
                response.Distances[row, col] = Board[row, col].DistToPiece;
                if (Team == Team.Red)
                {
                    response.RedTeamGoalAreaInformations[row, col] = Board[row, col].GoalInfo;
                    response.BlueTeamGoalAreaInformations[row, col] = GoalInfo.IDK;
                }
                else
                {
                    response.BlueTeamGoalAreaInformations[row, col] = Board[row, col].GoalInfo;
                    response.RedTeamGoalAreaInformations[row, col] = GoalInfo.IDK;
                }
            }
            message.Payload = response;
            await Communicate(message, cancellationToken);
        }

        public async Task RequestsResponse(CancellationToken cancellationToken, int respondToId, bool isFromLeader = false)
        {
            if (isFromLeader)
            {
                await GiveInfo(cancellationToken, isFromLeader);
            }
            else
            {
                // TODO: They are always in waiting, fix this!
                WaitingPlayers.Add(respondToId);
            }
        }

        private Message CreateMessage(MessageID type, Payload payload)
        {
            return new Message()
            {
                MessageID = type,
                AgentID = id,
                Payload = payload,
            };
        }

        public async Task CheckPiece(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            Message message = CreateMessage(MessageID.CheckPiece, payload);
            await Communicate(message, cancellationToken);
        }

        public async Task Discover(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            Message message = CreateMessage(MessageID.Discover, payload);

            await Communicate(message, cancellationToken);
        }

        /// <summary>
        /// Returns true if StartGameMessage was accepted
        /// </summary>
        public async Task<MessageID> AcceptMessage(CancellationToken cancellationToken)
        {
            var cancellationTimespan = TimeSpan.FromMinutes(2);
            Message message = await queue.ReceiveAsync(cancellationTimespan, cancellationToken);
            logger.Verbose("Received message. " + message.GetDescription());
            switch (message.MessageID)
            {
                case MessageID.CheckAnswer:
                    CheckAnswerPayload payloadCheck = (CheckAnswerPayload)message.Payload;
                    IsHeldPieceSham = payloadCheck.Sham;
                    penaltyTime = PenaltiesTimes.CheckPiece;
                    break;
                case MessageID.DestructionAnswer:
                    HasPiece = false;
                    IsHeldPieceSham = null;
                    penaltyTime = PenaltiesTimes.DestroyPiece;
                    break;
                case MessageID.DiscoverAnswer:
                    DiscoveryAnswerPayload payloadDiscover = (DiscoveryAnswerPayload)message.Payload;
                    Board[Position.y, Position.x].DistToPiece = payloadDiscover.DistanceFromCurrent.Value;
                    if (Position.y + 1 < BoardSize.y)
                        Board[Position.y + 1, Position.x].DistToPiece = payloadDiscover.DistanceN.Value;
                    if (Position.y > 0)
                        Board[Position.y - 1, Position.x].DistToPiece = payloadDiscover.DistanceS.Value;
                    if (Position.x + 1 < BoardSize.x)
                        Board[Position.y, Position.x + 1].DistToPiece = payloadDiscover.DistanceE.Value;
                    if (Position.x > 0)
                        Board[Position.y, Position.x - 1].DistToPiece = payloadDiscover.DistanceW.Value;
                    if (Position.y + 1 < BoardSize.y && Position.x > 0)
                        Board[Position.y + 1, Position.x - 1].DistToPiece = payloadDiscover.DistanceNW.Value;
                    if (Position.y > 0 && Position.x + 1 < BoardSize.x)
                        Board[Position.y - 1, Position.x + 1].DistToPiece = payloadDiscover.DistanceSE.Value;
                    if (Position.y + 1 < BoardSize.y && Position.x + 1 < BoardSize.x)
                        Board[Position.y + 1, Position.x + 1].DistToPiece = payloadDiscover.DistanceNE.Value;
                    if (Position.y > 0 && Position.x > 0)
                        Board[Position.y - 1, Position.x - 1].DistToPiece = payloadDiscover.DistanceSW.Value;
                    penaltyTime = PenaltiesTimes.Discover;
                    break;
                case MessageID.EndGame:
                    EndGamePayload payloadEnd = (EndGamePayload)message.Payload;
                    winner = payloadEnd.Winner;
                    StopWorking();
                    break;
                case MessageID.CSDisconnected:
                    winner = Team == Team.Blue ? Team.Red : Team.Blue;
                    logger.Warning("CS disconnected");
                    StopWorking();
                    break;
                case MessageID.StartGame:
                    StartGamePayload payloadStart = (StartGamePayload)message.Payload;
                    id = payloadStart.AgentID;
                    TeamMatesIds = payloadStart.AlliesIDs;
                    if (id == payloadStart.LeaderID)
                    {
                        IsLeader = true;
                    }
                    else
                    {
                        IsLeader = false;
                    }
                    LeaderId = payloadStart.LeaderID;
                    Team = payloadStart.TeamID;
                    BoardSize = (payloadStart.BoardSize.Y, payloadStart.BoardSize.X);
                    Board = new Field[payloadStart.BoardSize.Y, payloadStart.BoardSize.X];
                    for (int i = 0; i < payloadStart.BoardSize.Y; i++)
                    {
                        for (int j = 0; j < payloadStart.BoardSize.X; j++)
                        {
                            Board[i, j] = new Field
                            {
                                DistToPiece = int.MaxValue,
                                GoalInfo = GoalInfo.IDK,
                            };
                        }
                    }
                    PenaltiesTimes = payloadStart.Penalties;
                    Position = (payloadStart.Position.Y, payloadStart.Position.X);
                    EnemiesIds = payloadStart.EnemiesIDs;

                    GoalAreaSize = payloadStart.GoalAreaSize;
                    NumberOfPlayers = payloadStart.NumberOfPlayers;
                    NumberOfPieces = payloadStart.NumberOfPieces;
                    NumberOfGoals = payloadStart.NumberOfGoals;
                    ShamPieceProbability = payloadStart.ShamPieceProbability;
                    WaitingPlayers = new List<int>();
                    break;
                case MessageID.BegForInfoForwarded:
                    BegForInfoForwardedPayload payloadBeg = (BegForInfoForwardedPayload)message.Payload;
                    if (Team == payloadBeg.TeamID)
                    {
                        await RequestsResponse(cancellationToken, payloadBeg.AskingID, payloadBeg.Leader);
                    }
                    break;
                case MessageID.JoinTheGameAnswer:
                    JoinAnswerPayload payloadJoin = (JoinAnswerPayload)message.Payload;
                    id = payloadJoin.AgentID;
                    if (!payloadJoin.Accepted)
                    {
                        StopWorking();
                    }
                    break;
                case MessageID.MoveAnswer:
                    MoveAnswerPayload payloadMove = (MoveAnswerPayload)message.Payload;
                    if (payloadMove.MadeMove)
                    {
                        Position = (payloadMove.CurrentPosition.Y, payloadMove.CurrentPosition.X);

                        if (payloadMove.ClosestPiece != null)
                            Board[Position.y, Position.x].DistToPiece = payloadMove.ClosestPiece.Value;
                        else
                            Board[Position.y, Position.x].DistToPiece = int.MaxValue;
                    }
                    penaltyTime = PenaltiesTimes.Move;
                    break;
                case MessageID.PickAnswer:
                    HasPiece = true;
                    Board[Position.y, Position.x].DistToPiece = int.MaxValue;
                    penaltyTime = PenaltiesTimes.PickupPiece;
                    break;
                case MessageID.PutAnswer:
                    HasPiece = false;
                    IsHeldPieceSham = null;

                    PutAnswerPayload payload = (PutAnswerPayload)message.Payload;
                    if (payload.PutEvent == PutEvent.NormalOnGoalField)
                    {
                        Board[Position.y, Position.x].GoalInfo = GoalInfo.DiscoveredGoal;
                        logger.Information($"GOT GOAL at ({Position.y}, {Position.x}) !!!");
                    }
                    else if (payload.PutEvent == PutEvent.NormalOnNonGoalField)
                    {
                        Board[Position.y, Position.x].GoalInfo = GoalInfo.DiscoveredNotGoal;
                    }

                    penaltyTime = PenaltiesTimes.PutPiece;
                    break;
                case MessageID.GiveInfoForwarded:
                    GiveInfoForwardedPayload payloadGive = (GiveInfoForwardedPayload)message.Payload;
                    for (int i = 0; i < payloadGive.Distances.GetLength(0); i++)
                    {
                        for (int j = 0; j < payloadGive.Distances.GetLength(1); j++)
                        {
                            if (payloadGive.Distances[i, j] != int.MaxValue)
                            {
                                Board[i, j].DistToPiece = payloadGive.Distances[i, j];
                            }
                            if (payloadGive.RedTeamGoalAreaInformations[i, j] != GoalInfo.IDK)
                            {
                                Board[i, j].GoalInfo = payloadGive.RedTeamGoalAreaInformations[i, j];
                            }
                            else if (payloadGive.BlueTeamGoalAreaInformations[i, j] != GoalInfo.IDK)
                            {
                                Board[i, j].GoalInfo = payloadGive.BlueTeamGoalAreaInformations[i, j];
                            }
                        }
                    }
                    break;
                case MessageID.InformationExchangeResponse:
                    penaltyTime = PenaltiesTimes.Response;
                    break;
                case MessageID.InformationExchangeRequest:
                    penaltyTime = PenaltiesTimes.Ask;
                    break;
                case MessageID.NotWaitedError:
                    NotWaitedErrorPayload errorPayload = (NotWaitedErrorPayload)message.Payload;
                    int toWait = errorPayload.WaitFor;
                    if (toWait >= 0)
                    {
                        penaltyTime = toWait;
                    }
                    break;
                case MessageID.PickError:
                    penaltyTime = PenaltiesTimes.PickupPiece;
                    break;
                case MessageID.PutError:
                    penaltyTime = PenaltiesTimes.PutPiece;
                    break;
                case MessageID.UnknownError:
                    UnknownErrorPayload unknownErrorPayload = (UnknownErrorPayload)message.Payload;
                    HasPiece = unknownErrorPayload.HoldingPiece;
                    penaltyTime = 50;
                    break;
                default:
                    return MessageID.Unknown;
            }

            return message.MessageID;
        }

        public async Task DestroyPiece(CancellationToken cancellationToken)
        {
            EmptyPayload messagePickPayload = new EmptyPayload();
            Message messagePick = new Message()
            {
                MessageID = MessageID.PieceDestruction,
                AgentID = id,
                Payload = messagePickPayload,
            };
            await Communicate(messagePick, cancellationToken);
        }

        public async Task Pick(CancellationToken cancellationToken)
        {
            EmptyPayload messagePickPayload = new EmptyPayload();
            Message messagePick = new Message()
            {
                MessageID = MessageID.Pick,
                AgentID = id,
                Payload = messagePickPayload
            };
            await Communicate(messagePick, cancellationToken);
        }

        public async Task MakeDecisionFromStrategy(CancellationToken cancellationToken)
        {
            await strategy.MakeDecision(this, cancellationToken);
        }

        private async Task Communicate(Message message, CancellationToken cancellationToken)
        {
            await client.SendAsync(message, cancellationToken);
            logger.Verbose(MessageLogger.Sent(message));
        }

        private async Task Penalty(CancellationToken cancellationToken)
        {
            await Task.Delay(penaltyTime, cancellationToken);
            penaltyTime = 0;
        }
    }
}
