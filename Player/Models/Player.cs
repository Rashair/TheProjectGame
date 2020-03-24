using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Newtonsoft.Json;
using Player.Clients;
using Player.Models.Strategies;
using Shared;
using Shared.Enums;
using Shared.Models.Messages;
using Shared.Models.Payloads;
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
        private Penalties penaltiesTimes;
        private Team winner;
        private int[] enemiesIDs;
        private int goalAreaSize;
        private NumberOfPlayers numberOfPlayers;
        private int numberOfPieces;
        private int numberOfGoals;
        private float shamPieceProbability;
        private Team team;

        public bool IsLeader { get; private set; }

        public bool HavePiece { get; private set; }

        public Field[,] Board { get; private set; }

        public Tuple<int, int> Position { get; private set; }

        public List<int> WaitingPlayers { get; private set; }

        public int[] TeamMatesIds { get; private set; }

        public int LeaderId { get; private set; }

        public (int x, int y) BoardSize { get; private set; }

        public Player(Team team, BufferBlock<GMMessage> queue, WebSocketClient<GMMessage, PlayerMessage> client)
        {
            this.team = team;
            this.queue = queue;
            this.client = client;
        }

        // JoinTheGame before
        internal void JoinTheGame()
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
            Communicate(message);
        }

        // Start before
        internal async Task Start()
        {
            working = true;
            while (working)
            {
                await Task.Run(() => AcceptMessage());
                await Task.Run(() => MakeDecisionFromStrategy());
                await Task.Run(() => Penalty());
            }
        }

        // Stop before
        internal void Stop()
        {
            working = false;
        }

        public void Move(Directions direction)
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
            Communicate(message);
        }

        public void Put()
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = PlayerMessageID.Put,
                PlayerID = id,
                Payload = payload.Serialize(),
            };
            Communicate(message);
        }

        public void BegForInfo()
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

            Communicate(message);
        }

        public void GiveInfo(bool toLeader = false)
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

            WaitingPlayers.RemoveAt(0);

            message.Payload = response.Serialize();

            Communicate(message);
        }

        public void RequestsResponse(int respondToID, bool isFromLeader = false)
        {
            if (isFromLeader)
            {
                GiveInfo(isFromLeader);
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

        public void CheckPiece()
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = CreateMessage(PlayerMessageID.CheckPiece, payload);
            Communicate(message);
        }

        public void Discover()
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = CreateMessage(PlayerMessageID.Discover, payload);
            Communicate(message);
        }

        public void AcceptMessage()
        {
            GMMessage message;
            if (queue.TryReceive(null, out message))
            {
                switch (message.Id)
                {
                    case GMMessageID.CheckAnswer:
                        CheckAnswerPayload payloadCheck = JsonConvert.DeserializeObject<CheckAnswerPayload>(message.Payload);
                        if (payloadCheck.Sham) HavePiece = false;
                        break;
                    case GMMessageID.DestructionAnswer:
                        HavePiece = false;
                        break;
                    case GMMessageID.DiscoverAnswer:
                        DiscoveryAnswerPayload payloadDiscover = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.Payload);
                        Board[Position.Item1, Position.Item2].DistToPiece = payloadDiscover.DistanceFromCurrent;
                        Board[Position.Item1 + 1, Position.Item2].DistToPiece = payloadDiscover.DistanceE;
                        Board[Position.Item1 - 1, Position.Item2].DistToPiece = payloadDiscover.DistanceW;
                        Board[Position.Item1, Position.Item2 + 1].DistToPiece = payloadDiscover.DistanceS;
                        Board[Position.Item1, Position.Item2 - 1].DistToPiece = payloadDiscover.DistanceN;
                        Board[Position.Item1 + 1, Position.Item2 + 1].DistToPiece = payloadDiscover.DistanceSE;
                        Board[Position.Item1 - 1, Position.Item2 - 1].DistToPiece = payloadDiscover.DistanceNW;
                        Board[Position.Item1 + 1, Position.Item2 - 1].DistToPiece = payloadDiscover.DistanceNE;
                        Board[Position.Item1 - 1, Position.Item2 + 1].DistToPiece = payloadDiscover.DistanceSW;
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
                        if (id == payloadStart.LeaderID) IsLeader = true;
                        else IsLeader = false;
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
                        break;
                    case GMMessageID.BegForInfoForwarded:
                        BegForInfoForwardedPayload payloadBeg = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.Payload);
                        if (team == payloadBeg.TeamId)
                        {
                            if (payloadBeg.Leader)
                            {
                                GiveInfo(true);
                            }
                            else
                            {
                                WaitingPlayers.Add(payloadBeg.AskingID);
                            }
                        }
                        break;
                    case GMMessageID.JoinTheGameAnswer:
                        JoinAnswerPayload payloadJoin = JsonConvert.DeserializeObject<JoinAnswerPayload>(message.Payload);
                        if (id != payloadJoin.PlayerID) id = payloadJoin.PlayerID;
                        if (!payloadJoin.Accepted) Stop();
                        break;
                    case GMMessageID.MoveAnswer:
                        MoveAnswerPayload payloadMove = JsonConvert.DeserializeObject<MoveAnswerPayload>(message.Payload);
                        if (payloadMove.MadeMove)
                        {
                            Position = new Tuple<int, int>(payloadMove.CurrentPosition.X, payloadMove.CurrentPosition.Y);
                            Board[Position.Item1, Position.Item2].DistToPiece = payloadMove.ClosestPiece;
                            EmptyPayload answerPickPayload = new EmptyPayload();
                            PlayerMessage messageStart = new PlayerMessage()
                            {
                                MessageID = PlayerMessageID.Pick,
                                PlayerID = id,
                                Payload = JsonConvert.SerializeObject(answerPickPayload),
                            };
                            Communicate(messageStart);
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
                                if (payloadGive.Distances[i, j] != -1) Board[i, j].DistToPiece = payloadGive.Distances[i, j];
                                if (team == Team.Red)
                                {
                                    if (payloadGive.RedTeamGoalAreaInformations[i, j] != GoalInfo.IDK) Board[i, j].GoalInfo = payloadGive.RedTeamGoalAreaInformations[i, j];
                                }
                                else
                                {
                                    if (payloadGive.BlueTeamGoalAreaInformations[i, j] != GoalInfo.IDK) Board[i, j].GoalInfo = payloadGive.BlueTeamGoalAreaInformations[i, j];
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public void MakeDecisionFromStrategy()
        {
            strategy.MakeDecision(this);
        }

        private async void Communicate(PlayerMessage message)
        {
            CancellationToken ct = CancellationToken.None;
            await client.SendAsync(message, ct);
        }

        private void Penalty()
        {
            Thread.Sleep(penaltyTime);
            penaltyTime = 0;
        }
    }
}
