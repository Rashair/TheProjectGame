using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Newtonsoft.Json;
using Player.Clients;
using Player.Models.Strategies;
using Shared.Enums;
using Shared.Messages;
using Shared.Models;
using Shared.Payloads;
using Shared.Senders;

namespace Player.Models
{
    public class Player
    {
        private BufferBlock<GMMessage> queue;
        private ISocketClient<GMMessage, PlayerMessage> client;

        private int id;
        private ISender sender;
        private int penaltyTime;
        private IStrategy strategy;
        private bool working;
        private readonly PlayerConfiguration conf;
        private Team winner;

        public int PreviousDistToPiece { get; private set; }

        public Penalties PenaltiesTimes { get; private set; }

        public int[] EnemiesIDs { get; private set; }

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

        public Player(PlayerConfiguration conf, IStrategy strategy, BufferBlock<GMMessage> queue, ISocketClient<GMMessage, PlayerMessage> client)
        {
            this.conf = conf;
            if (conf.TeamID == "red")
                Team = Team.Red;
            else
                Team = Team.Blue;
            this.strategy = strategy;
            this.queue = queue;
            this.client = client;
        }

        internal async Task Work(CancellationToken cancellationToken)
        {
            await JoinTheGame(cancellationToken);
            bool startGame = false;
            while (!cancellationToken.IsCancellationRequested && !startGame)
            {
                startGame = await AcceptMessage(cancellationToken);
            }
            if (startGame)
            {
                await Start(cancellationToken);
            }
        }

        internal async Task JoinTheGame(CancellationToken cancellationToken)
        {
            JoinGamePayload payload = new JoinGamePayload()
            {
                TeamID = Team,
            };
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = PlayerMessageID.JoinTheGame,
                Payload = payload.Serialize(),
            };
            await Communicate(message, cancellationToken);
        }

        internal async Task Start(CancellationToken cancellationToken)
        {
            working = true;
            while (working)
            {
                await AcceptMessage(cancellationToken);
                await MakeDecisionFromStrategy(cancellationToken);
                await Penalty();
            }
        }

        internal void Stop()
        {
            working = false;
        }

        public async Task Move(Direction direction, CancellationToken cancellationToken)
        {
            MovePayload payload = new MovePayload()
            {
                Direction = direction,
            };
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = PlayerMessageID.Move,
                PlayerID = id,
                Payload = payload.Serialize(),
            };
            await Communicate(message, cancellationToken);
        }

        public async Task Put(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = PlayerMessageID.Put,
                PlayerID = id,
                Payload = payload.Serialize(),
            };
            await Communicate(message, cancellationToken);
        }

        public async Task BegForInfo(CancellationToken cancellationToken)
        {
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = PlayerMessageID.BegForInfo,
                PlayerID = id,
            };

            Random rnd = new Random();
            int index = rnd.Next(0, TeamMatesIds.Length - 1);
            if (TeamMatesIds[index] == id)
            {
                index = (index + 1) % TeamMatesIds.Length;
            }

            BegForInfoPayload payload = new BegForInfoPayload()
            {
                AskedPlayerID = TeamMatesIds[index],
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
                MessageID = PlayerMessageID.GiveInfo,
                PlayerID = id,
            };

            GiveInfoPayload response = new GiveInfoPayload();
            if (toLeader)
            {
                response.RespondToID = LeaderId;
            }
            else
            {
                response.RespondToID = WaitingPlayers[0];
                WaitingPlayers.RemoveAt(0);
            }

            response.Distances = new int[BoardSize.x, BoardSize.y];
            response.RedTeamGoalAreaInformations = new GoalInfo[BoardSize.x, BoardSize.y];
            response.BlueTeamGoalAreaInformations = new GoalInfo[BoardSize.x, BoardSize.y];

            for (int i = 0; i < Board.Length; ++i)
            {
                int row = i / BoardSize.y;
                int col = i % BoardSize.y;
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
            message.Payload = response.Serialize();
            await Communicate(message, cancellationToken);
            penaltyTime = int.Parse(PenaltiesTimes.InformationExchange);
        }

        public async Task RequestsResponse(CancellationToken cancellationToken, int respondToID, bool isFromLeader = false)
        {
            if (isFromLeader)
            {
                await GiveInfo(cancellationToken, isFromLeader);
            }
            else
            {
                WaitingPlayers.Add(respondToID);
            }
        }

        private PlayerMessage CreateMessage(PlayerMessageID type, Payload payload)
        {
            return new PlayerMessage()
            {
                MessageID = type,
                PlayerID = id,
                Payload = payload.Serialize(),
            };
        }

        public async Task CheckPiece(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = CreateMessage(PlayerMessageID.CheckPiece, payload);
            await Communicate(message, cancellationToken);
        }

        public async Task Discover(CancellationToken cancellationToken)
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = CreateMessage(PlayerMessageID.Discover, payload);

            await Communicate(message, cancellationToken);
        }

        public async Task<bool> AcceptMessage(CancellationToken cancellationToken) // returns true if StartGameMessage was accepted
        {
            GMMessage message;
            if (queue.TryReceive(null, out message))
            {
                switch (message.Id)
                {
                    case GMMessageID.CheckAnswer:
                        CheckAnswerPayload payloadCheck = JsonConvert.DeserializeObject<CheckAnswerPayload>(message.Payload);
                        IsHeldPieceSham = payloadCheck.Sham;
                        penaltyTime = int.Parse(PenaltiesTimes.CheckForSham);
                        break;
                    case GMMessageID.DestructionAnswer:
                        HasPiece = false;
                        IsHeldPieceSham = null;
                        penaltyTime = int.Parse(PenaltiesTimes.DestroyPiece);
                        break;
                    case GMMessageID.DiscoverAnswer:
                        DiscoveryAnswerPayload payloadDiscover = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.Payload);
                        Board[Position.y, Position.x].DistToPiece = payloadDiscover.DistanceFromCurrent;
                        Board[Position.y + 1, Position.x].DistToPiece = payloadDiscover.DistanceE;
                        Board[Position.y - 1, Position.x].DistToPiece = payloadDiscover.DistanceW;
                        Board[Position.y, Position.x + 1].DistToPiece = payloadDiscover.DistanceN;
                        Board[Position.y, Position.x - 1].DistToPiece = payloadDiscover.DistanceS;
                        Board[Position.y + 1, Position.x - 1].DistToPiece = payloadDiscover.DistanceSE;
                        Board[Position.y - 1, Position.x + 1].DistToPiece = payloadDiscover.DistanceNW;
                        Board[Position.y + 1, Position.x + 1].DistToPiece = payloadDiscover.DistanceNE;
                        Board[Position.y - 1, Position.x - 1].DistToPiece = payloadDiscover.DistanceSW;
                        penaltyTime = int.Parse(PenaltiesTimes.Discovery);
                        break;
                    case GMMessageID.EndGame:
                        EndGamePayload payloadEnd = JsonConvert.DeserializeObject<EndGamePayload>(message.Payload);
                        winner = payloadEnd.Winner;
                        Stop();
                        break;
                    case GMMessageID.StartGame:
                        StartGamePayload payloadStart = JsonConvert.DeserializeObject<StartGamePayload>(message.Payload);
                        id = payloadStart.PlayerID;
                        TeamMatesIds = payloadStart.AlliesIDs;
                        if (id == payloadStart.LeaderID)
                        {
                            IsLeader = true;
                        }
                        else
                        {
                            IsLeader = false;
                        }
                        Team = payloadStart.TeamId;
                        Board = new Field[payloadStart.BoardSize.X, payloadStart.BoardSize.Y];
                        for (int i = 0; i < payloadStart.BoardSize.X; i++)
                        {
                            for (int j = 0; j < payloadStart.BoardSize.Y; j++)
                            {
                                Board[i, j] = new Field
                                {
                                    DistToPiece = int.MaxValue,
                                    GoalInfo = GoalInfo.IDK,
                                };
                            }
                        }
                        PenaltiesTimes = payloadStart.Penalties;
                        Position = (payloadStart.Position.X, payloadStart.Position.Y);
                        EnemiesIDs = payloadStart.EnemiesIDs;
                        GoalAreaSize = payloadStart.GoalAreaSize;
                        NumberOfPlayers = payloadStart.NumberOfPlayers;
                        NumberOfPieces = payloadStart.NumberOfPieces;
                        NumberOfGoals = payloadStart.NumberOfGoals;
                        ShamPieceProbability = payloadStart.ShamPieceProbability;
                        WaitingPlayers = new List<int>();
                        return true;
                    case GMMessageID.BegForInfoForwarded:
                        BegForInfoForwardedPayload payloadBeg = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.Payload);
                        if (Team == payloadBeg.TeamId)
                        {
                            await RequestsResponse(cancellationToken, payloadBeg.AskingID, payloadBeg.Leader);
                        }
                        break;
                    case GMMessageID.JoinTheGameAnswer:
                        JoinAnswerPayload payloadJoin = JsonConvert.DeserializeObject<JoinAnswerPayload>(message.Payload);
                        id = payloadJoin.PlayerID;
                        if (!payloadJoin.Accepted)
                        {
                            Stop();
                        }
                        break;
                    case GMMessageID.MoveAnswer:
                        MoveAnswerPayload payloadMove = JsonConvert.DeserializeObject<MoveAnswerPayload>(message.Payload);
                        if (payloadMove.MadeMove)
                        {
                            Position = (payloadMove.CurrentPosition.X, payloadMove.CurrentPosition.Y);
                            Board[Position.y, Position.x].DistToPiece = payloadMove.ClosestPiece;
                        }
                        penaltyTime = int.Parse(PenaltiesTimes.Move);
                        break;
                    case GMMessageID.PickAnswer:
                        HasPiece = true;

                        // TODO: Add if this value will be in configuration
                        penaltyTime = 0;
                        break;
                    case GMMessageID.PutAnswer:
                        HasPiece = false;
                        penaltyTime = int.Parse(PenaltiesTimes.PutPiece);
                        break;
                    case GMMessageID.GiveInfoForwarded:
                        GiveInfoForwardedPayload payloadGive = JsonConvert.DeserializeObject<GiveInfoForwardedPayload>(message.Payload);
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
                    default:
                        break;
                }
            }
            return false;
        }

        public async Task DestroyPiece(CancellationToken cancellationToken)
        {
            EmptyPayload messagePickPayload = new EmptyPayload();
            PlayerMessage messagePick = new PlayerMessage()
            {
                MessageID = PlayerMessageID.PieceDestruction,
                PlayerID = id,
                Payload = JsonConvert.SerializeObject(messagePickPayload),
            };
            await Communicate(messagePick, cancellationToken);
        }

        public async Task Pick(CancellationToken cancellationToken)
        {
            EmptyPayload messagePickPayload = new EmptyPayload();
            PlayerMessage messagePick = new PlayerMessage()
            {
                MessageID = PlayerMessageID.Pick,
                PlayerID = id,
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

        private async Task Penalty()
        {
            await Task.Delay(penaltyTime);
            penaltyTime = 0;
        }
    }
}
