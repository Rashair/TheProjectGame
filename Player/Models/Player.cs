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
        private WebSocketClient<GMMessage, PlayerMessage> client;

        private int id;
        private ISender sender;
        private int penaltyTime;
        private IStrategy strategy;
        private bool working;
        private Team team;

        private Penalties penaltiesTimes;
        private Team winner;
        private int[] enemiesIDs;
        private int goalAreaSize;
        private NumberOfPlayers numberOfPlayers;
        private int numberOfPieces;
        private int numberOfGoals;
        private float shamPieceProbability;

        private readonly PlayerConfiguration conf;

        public bool IsLeader { get; private set; }

        public bool HavePiece { get; private set; }

        public Field[,] Board { get; private set; }

        public Tuple<int, int> Position { get; private set; }

        public List<int> WaitingPlayers { get; private set; }

        public int[] TeamMatesIds { get; private set; }

        public int LeaderId { get; private set; }

        public (int x, int y) BoardSize { get; private set; }

        public Player(PlayerConfiguration conf, IStrategy strategy, BufferBlock<GMMessage> queue, WebSocketClient<GMMessage, PlayerMessage> client)
        {
            this.conf = conf;
            if (conf.TeamID == "red")
                team = Team.Red;
            else
                team = Team.Blue;
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
                TeamID = team,
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
                await Task.Run(() =>
                {
                    MakeDecisionFromStrategy();
                    Penalty();
                }, cancellationToken);
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
                if (team == Team.Red)
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
                        if (payloadCheck.Sham)
                        {
                            HavePiece = false;
                        }
                        break;
                    case GMMessageID.DestructionAnswer:
                        HavePiece = false;
                        break;
                    case GMMessageID.DiscoverAnswer:
                        DiscoveryAnswerPayload payloadDiscover = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.Payload);
                        Board[Position.Item1, Position.Item2].DistToPiece = payloadDiscover.DistanceFromCurrent;
                        Board[Position.Item1 + 1, Position.Item2].DistToPiece = payloadDiscover.DistanceE;
                        Board[Position.Item1 - 1, Position.Item2].DistToPiece = payloadDiscover.DistanceW;
                        Board[Position.Item1, Position.Item2 + 1].DistToPiece = payloadDiscover.DistanceN;
                        Board[Position.Item1, Position.Item2 - 1].DistToPiece = payloadDiscover.DistanceS;
                        Board[Position.Item1 + 1, Position.Item2 - 1].DistToPiece = payloadDiscover.DistanceSE;
                        Board[Position.Item1 - 1, Position.Item2 + 1].DistToPiece = payloadDiscover.DistanceNW;
                        Board[Position.Item1 + 1, Position.Item2 + 1].DistToPiece = payloadDiscover.DistanceNE;
                        Board[Position.Item1 - 1, Position.Item2 - 1].DistToPiece = payloadDiscover.DistanceSW;
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
                        team = payloadStart.TeamId;
                        Board = new Field[payloadStart.BoardSize.X, payloadStart.BoardSize.Y];
                        for (int i = 0; i < payloadStart.BoardSize.X; i++)
                        {
                            for (int j = 0; j < payloadStart.BoardSize.Y; j++)
                            {
                                Board[i, j] = new Field();
                                Board[i, j].DistToPiece = -1;
                                Board[i, j].GoalInfo = GoalInfo.IDK;
                            }
                        }
                        penaltiesTimes = payloadStart.Penalties;
                        Position = new Tuple<int, int>(payloadStart.Position.X, payloadStart.Position.Y);
                        enemiesIDs = payloadStart.EnemiesIDs;
                        goalAreaSize = payloadStart.GoalAreaSize;
                        numberOfPlayers = payloadStart.NumberOfPlayers;
                        numberOfPieces = payloadStart.NumberOfPieces;
                        numberOfGoals = payloadStart.NumberOfGoals;
                        shamPieceProbability = payloadStart.ShamPieceProbability;
                        WaitingPlayers = new List<int>();
                        return true;
                    case GMMessageID.BegForInfoForwarded:
                        BegForInfoForwardedPayload payloadBeg = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.Payload);
                        if (team == payloadBeg.TeamId)
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
                            Position = new Tuple<int, int>(payloadMove.CurrentPosition.X, payloadMove.CurrentPosition.Y);
                            Board[Position.Item1, Position.Item2].DistToPiece = payloadMove.ClosestPiece;
                            if (Board[Position.Item1, Position.Item2].DistToPiece == 0)
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
                        }
                        break;
                    case GMMessageID.PickAnswer:
                        HavePiece = true;
                        break;
                    case GMMessageID.PutAnswer:
                        HavePiece = false;
                        break;
                    case GMMessageID.GiveInfoForwarded:
                        GiveInfoForwardedPayload payloadGive = JsonConvert.DeserializeObject<GiveInfoForwardedPayload>(message.Payload);
                        for (int i = 0; i < payloadGive.Distances.GetLength(0); i++)
                        {
                            for (int j = 0; j < payloadGive.Distances.GetLength(1); j++)
                            {
                                if (payloadGive.Distances[i, j] != -1)
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

        public void MakeDecisionFromStrategy()
        {
            strategy.MakeDecision(this);
        }

        private async Task Communicate(PlayerMessage message, CancellationToken cancellationToken)
        {
            await client.SendAsync(message, cancellationToken);
        }

        private void Penalty()
        {
            Thread.Sleep(penaltyTime);
            penaltyTime = 0;
        }
    }
}
