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
using Shared.Senders;

namespace Player.Models
{
    public class Player
    {
        private readonly BufferBlock<GMMessage> queue;
        private readonly ISocketClient<GMMessage, PlayerMessage> client;
        private readonly ILogger logger;
        private long moveCounter = 0;

        private int id;
        private ISender sender;
        private int penaltyTime;
        private IStrategy strategy;
        private bool working;
        private readonly PlayerConfiguration conf;
        private Team? winner;
        private int discovered;

        public Player(PlayerConfiguration conf, BufferBlock<GMMessage> queue, ISocketClient<GMMessage, 
            PlayerMessage> client, ILogger logger)
        {
            this.conf = conf;
            this.strategy = StrategyFactory.Create((StrategyEnum)conf.Strategy);
            this.queue = queue;
            this.client = client;
            this.logger = logger.ForContext<Player>();
            this.Team = conf.TeamId == "red" ? Team.Red : Team.Blue;
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
            bool startGame = false;
            while (!cancellationToken.IsCancellationRequested && !startGame)
            {
                startGame = await AcceptMessage(cancellationToken);
            }

            logger.Information("Starting game");
            await Work(cancellationToken);
        }

        public async Task JoinTheGame(CancellationToken cancellationToken)
        {
            JoinGamePayload payload = new JoinGamePayload()
            {
                TeamId = Team,
            };
            PlayerMessage message = new PlayerMessage()
            {
                MessageId = PlayerMessageId.JoinTheGame,
                Payload = payload.Serialize(),
            };
            await Communicate(message, cancellationToken);
            logger.Information("Sent JoinTheGame message");
        }

        internal async Task Work(CancellationToken cancellationToken)
        {
            working = true;
            while (working && !cancellationToken.IsCancellationRequested)
            {
                await MakeDecisionFromStrategy(cancellationToken);
                await AcceptMessage(cancellationToken);
                await Penalty(cancellationToken);
            }

            await client.CloseAsync(cancellationToken);
        }

        internal void StopWorking()
        {
            working = false;
            logger.Warning($"Stopped player: {Team}");
        }

        public async Task Move(Direction direction, CancellationToken cancellationToken)
        {
            MovePayload payload = new MovePayload()
            {
                Direction = direction,
            };
            PlayerMessage message = new PlayerMessage()
            {
                MessageId = PlayerMessageId.Move,
                PlayerId = id,
                Payload = payload.Serialize(),
            };
            await Communicate(message, cancellationToken);
        }

        public async Task Put(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = new PlayerMessage()
            {
                MessageId = PlayerMessageId.Put,
                PlayerId = id,
                Payload = payload.Serialize(),
            };
            await Communicate(message, cancellationToken);
        }

        public async Task BegForInfo(CancellationToken cancellationToken)
        {
            PlayerMessage message = new PlayerMessage()
            {
                MessageId = PlayerMessageId.BegForInfo,
                PlayerId = id,
            };

            Random rnd = new Random();
            int index = rnd.Next(0, TeamMatesIds.Length - 1);
            if (TeamMatesIds[index] == id)
            {
                index = (index + 1) % TeamMatesIds.Length;
            }

            BegForInfoPayload payload = new BegForInfoPayload()
            {
                AskedPlayerId = TeamMatesIds[index],
            };

            message.Payload = payload.Serialize();

            await Communicate(message, cancellationToken);
        }

        public async Task GiveInfo(CancellationToken cancellationToken, bool toLeader = false)
        {
            if (WaitingPlayers.Count < 1 && !toLeader)
                return;

            PlayerMessage message = new PlayerMessage()
            {
                MessageId = PlayerMessageId.GiveInfo,
                PlayerId = id,
            };

            GiveInfoPayload response = new GiveInfoPayload();
            if (toLeader)
            {
                response.RespondToId = LeaderId;
            }
            else
            {
                response.RespondToId = WaitingPlayers[0];
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
                    response.BlueTeamGoalAreaInformations[row, col] = GoalInfo.IdK;
                }
                else
                {
                    response.BlueTeamGoalAreaInformations[row, col] = Board[row, col].GoalInfo;
                    response.RedTeamGoalAreaInformations[row, col] = GoalInfo.IdK;
                }
            }
            message.Payload = response.Serialize();
            await Communicate(message, cancellationToken);
            penaltyTime = int.Parse(PenaltiesTimes.InformationExchange);
        }

        public async Task RequestsResponse(CancellationToken cancellationToken, int respondToId, bool isFromLeader = false)
        {
            if (isFromLeader)
            {
                await GiveInfo(cancellationToken, isFromLeader);
            }
            else
            {
                WaitingPlayers.Add(respondToId);
            }
        }

        private PlayerMessage CreateMessage(PlayerMessageId type, Payload payload)
        {
            return new PlayerMessage()
            {
                MessageId = type,
                PlayerId = id,
                Payload = payload.Serialize(),
            };
        }

        public async Task CheckPiece(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = CreateMessage(PlayerMessageId.CheckPiece, payload);
            await Communicate(message, cancellationToken);
        }

        public async Task Discover(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = CreateMessage(PlayerMessageId.Discover, payload);

            await Communicate(message, cancellationToken);
        }

        /// <summary>
        /// Returns true if StartGameMessage was accepted
        /// </summary>
        public async Task<bool> AcceptMessage(CancellationToken cancellationToken)
        {
            var cancellationTimespan = TimeSpan.FromMinutes(2);
            GMMessage message = await queue.ReceiveAsync(cancellationTimespan, cancellationToken);

            // logger.Information($"|{message.Id} | {message.Payload} | HasPiece: {HasPiece} | {discovered} ");
            switch (message.Id)
            {
                case GMMessageId.CheckAnswer:
                    CheckAnswerPayload payloadCheck = JsonConvert.DeserializeObject<CheckAnswerPayload>(message.Payload);
                    IsHeldPieceSham = payloadCheck.Sham;
                    penaltyTime = int.Parse(PenaltiesTimes.CheckForSham);
                    break;
                case GMMessageId.DestructionAnswer:
                    HasPiece = false;
                    IsHeldPieceSham = null;

                    // TODO: switch to conf
                    penaltyTime = 100;
                    break;
                case GMMessageId.DiscoverAnswer:
                    DiscoveryAnswerPayload payloadDiscover = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.Payload);
                    Board[Position.y, Position.x].DistToPiece = payloadDiscover.DistanceFromCurrent;
                    if (Position.y + 1 < BoardSize.y) 
                        Board[Position.y + 1, Position.x].DistToPiece = payloadDiscover.DistanceN;
                    if (Position.y > 0) 
                        Board[Position.y - 1, Position.x].DistToPiece = payloadDiscover.DistanceS;
                    if (Position.x + 1 < BoardSize.x) 
                        Board[Position.y, Position.x + 1].DistToPiece = payloadDiscover.DistanceE;
                    if (Position.x > 0)
                        Board[Position.y, Position.x - 1].DistToPiece = payloadDiscover.DistanceW;
                    if (Position.y + 1 < BoardSize.y && Position.x > 0)
                        Board[Position.y + 1, Position.x - 1].DistToPiece = payloadDiscover.DistanceNW;
                    if (Position.y > 0 && Position.x + 1 < BoardSize.x)
                        Board[Position.y - 1, Position.x + 1].DistToPiece = payloadDiscover.DistanceSE;
                    if (Position.y + 1 < BoardSize.y && Position.x + 1 < BoardSize.x) 
                        Board[Position.y + 1, Position.x + 1].DistToPiece = payloadDiscover.DistanceNE;
                    if (Position.y > 0 && Position.x > 0)
                        Board[Position.y - 1, Position.x - 1].DistToPiece = payloadDiscover.DistanceSW;
                    penaltyTime = int.Parse(PenaltiesTimes.Discovery);
                    break;
                case GMMessageId.EndGame:
                    EndGamePayload payloadEnd = JsonConvert.DeserializeObject<EndGamePayload>(message.Payload);
                    winner = payloadEnd.Winner;
                    StopWorking();
                    break;
                case GMMessageId.StartGame:
                    StartGamePayload payloadStart = JsonConvert.DeserializeObject<StartGamePayload>(message.Payload);
                    id = payloadStart.PlayerId;
                    TeamMatesIds = payloadStart.AlliesIds;
                    if (id == payloadStart.LeaderId)
                    {
                        IsLeader = true;
                    }
                    else
                    {
                        IsLeader = false;
                    }
                    LeaderId = payloadStart.LeaderId;
                    Team = payloadStart.TeamId;
                    BoardSize = (payloadStart.BoardSize.Y, payloadStart.BoardSize.X);
                    Board = new Field[payloadStart.BoardSize.Y, payloadStart.BoardSize.X];
                    for (int i = 0; i < payloadStart.BoardSize.Y; i++)
                    {
                        for (int j = 0; j < payloadStart.BoardSize.X; j++)
                        {
                            Board[i, j] = new Field
                            {
                                DistToPiece = int.MaxValue,
                                GoalInfo = GoalInfo.IdK,
                            };
                        }
                    }
                    PenaltiesTimes = payloadStart.Penalties;
                    Position = (payloadStart.Position.Y, payloadStart.Position.X);
                    EnemiesIds = payloadStart.EnemiesIds;
                    GoalAreaSize = payloadStart.GoalAreaSize;
                    NumberOfPlayers = payloadStart.NumberOfPlayers;
                    NumberOfPieces = payloadStart.NumberOfPieces;
                    NumberOfGoals = payloadStart.NumberOfGoals;
                    ShamPieceProbability = payloadStart.ShamPieceProbability;
                    WaitingPlayers = new List<int>();
                    return true;
                case GMMessageId.BegForInfoForwarded:
                    BegForInfoForwardedPayload payloadBeg = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.Payload);
                    if (Team == payloadBeg.TeamId)
                    {
                        await RequestsResponse(cancellationToken, payloadBeg.AskingId, payloadBeg.Leader);
                    }
                    break;
                case GMMessageId.JoinTheGameAnswer:
                    JoinAnswerPayload payloadJoin = JsonConvert.DeserializeObject<JoinAnswerPayload>(message.Payload);
                    id = payloadJoin.PlayerId;
                    if (!payloadJoin.Accepted)
                    {
                        StopWorking();
                    }
                    break;
                case GMMessageId.MoveAnswer:
                    MoveAnswerPayload payloadMove = JsonConvert.DeserializeObject<MoveAnswerPayload>(message.Payload);
                    if (payloadMove.MadeMove)
                    {
                        Position = (payloadMove.CurrentPosition.Y, payloadMove.CurrentPosition.X);
                        Board[Position.y, Position.x].DistToPiece = payloadMove.ClosestPiece;
                    }
                    penaltyTime = int.Parse(PenaltiesTimes.Move);
                    break;
                case GMMessageId.PickAnswer:
                    HasPiece = true;

                    // TODO: Add if this value will be in configuration
                    Board[Position.y, Position.x].DistToPiece = int.MaxValue;
                    penaltyTime = 100;
                    break;
                case GMMessageId.PutAnswer:
                    HasPiece = false;
                    IsHeldPieceSham = null;

                    // TODO: info about discovered goal !!!
                    var payload = JsonConvert.DeserializeObject<StartGamePayload>(message.Payload);
                    if (Board[Position.y, Position.x].GoalInfo == GoalInfo.IdK)
                    {
                        ++discovered;
                    }
                    Board[Position.y, Position.x].GoalInfo = GoalInfo.DiscoveredNotGoal;
                    penaltyTime = int.Parse(PenaltiesTimes.PutPiece);
                    break;
                case GMMessageId.GiveInfoForwarded:
                    GiveInfoForwardedPayload payloadGive = JsonConvert.DeserializeObject<GiveInfoForwardedPayload>(message.Payload);
                    for (int i = 0; i < payloadGive.Distances.GetLength(0); i++)
                    {
                        for (int j = 0; j < payloadGive.Distances.GetLength(1); j++)
                        {
                            if (payloadGive.Distances[i, j] != int.MaxValue)
                            {
                                Board[i, j].DistToPiece = payloadGive.Distances[i, j];
                            }
                            if (payloadGive.RedTeamGoalAreaInformations[i, j] != GoalInfo.IdK)
                            {
                                Board[i, j].GoalInfo = payloadGive.RedTeamGoalAreaInformations[i, j];
                            }
                            else if (payloadGive.BlueTeamGoalAreaInformations[i, j] != GoalInfo.IdK)
                            {
                                Board[i, j].GoalInfo = payloadGive.BlueTeamGoalAreaInformations[i, j];
                            }
                        }
                    }
                    break;
                case GMMessageId.NotWaitedError:
                    NotWaitedErrorPayload errorPayload = JsonConvert.DeserializeObject<NotWaitedErrorPayload>(message.Payload);
                    int toWait = (int)(DateTime.Now - errorPayload.WaitUntil).TotalMilliseconds;
                    if (toWait >= 0)
                    {
                        penaltyTime = toWait;
                    }
                    break;
                case GMMessageId.PickError:
                    // TODO: from config
                    penaltyTime = 100;
                    break;
                case GMMessageId.PutError:
                    penaltyTime = int.Parse(PenaltiesTimes.PutPiece);
                    break;
                case GMMessageId.UnknownError:
                    penaltyTime = 50;
                    break;
            }

            await Task.Delay(20);
            return false;
        }

        public async Task DestroyPiece(CancellationToken cancellationToken)
        {
            EmptyPayload messagePickPayload = new EmptyPayload();
            PlayerMessage messagePick = new PlayerMessage()
            {
                MessageId = PlayerMessageId.PieceDestruction,
                PlayerId = id,
                Payload = JsonConvert.SerializeObject(messagePickPayload),
            };
            await Communicate(messagePick, cancellationToken);
        }

        public async Task Pick(CancellationToken cancellationToken)
        {
            EmptyPayload messagePickPayload = new EmptyPayload();
            PlayerMessage messagePick = new PlayerMessage()
            {
                MessageId = PlayerMessageId.Pick,
                PlayerId = id,
                Payload = JsonConvert.SerializeObject(messagePickPayload),
            };
            await Communicate(messagePick, cancellationToken);
        }

        public async Task MakeDecisionFromStrategy(CancellationToken cancellationToken)
        {
            await strategy.MakeDecision(this, cancellationToken);
        }

        private async Task Communicate(PlayerMessage message, CancellationToken cancellationToken)
        {
            await client.SendAsync(message, cancellationToken);
        }

        private async Task Penalty(CancellationToken cancellationToken)
        {
            await Task.Delay(penaltyTime, cancellationToken);
            penaltyTime = 0;
        }
    }
}
