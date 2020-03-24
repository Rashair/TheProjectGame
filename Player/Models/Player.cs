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
using Shared.Messages;
using Shared.Models.Payloads;
using Shared.Payloads;
using Shared.Senders;

namespace Player.Models
{
    public class Player
    {
        private int id;
        private ISender sender;
        private int penaltyTime;
        private IStrategy strategy;
        private bool working;
        private Team team;

        public bool IsLeader { get; private set; }

        public bool HavePiece { get; private set; }

        public Field[,] Board { get; private set; }

        public Tuple<int, int> Position { get; private set; }

        public List<int> WaitingPlayers { get; private set; }

        public int[] TeamMatesIds { get; private set; }

        public int LeaderId { get; private set; }

        public (int x, int y) BoardSize { get; private set; }

        private Penalties penaltiesTimes;
        private string winner;
        private int[] enemiesIDs;
        private int goalAreaSize;
        private NumberOfPlayers numberOfPlayers;
        private int numberOfPieces;
        private int numberOfGoals;
        private float shamPieceProbability;
        private BufferBlock<GMMessage> queue;
        private WebSocketClient<GMMessage, PlayerMessage> client;

        public Player(Team team, BufferBlock<GMMessage> queue, WebSocketClient<GMMessage, PlayerMessage> client)
        {
            team = team;
            queue = queue;
            client = client;
        }

        // JoinTheGame before
        internal void JoinTheGame()
        {
            JoinGamePayload payload = new JoinGamePayload()
            {
                TeamID = team.ToString(),
            };
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = (int)MessageType.JoinTheGame,
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

        public void Move(Direction direction)
        {
            MovePayload payload = new MovePayload()
            {
                Direction = direction.ToString(),
            };
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = (int)MessageType.Move,
                AgentID = id,
                Payload = payload.Serialize(),
            };
            Communicate(message);
        }

        public void Put()
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = (int)MessageType.Put,
                AgentID = id,
                Payload = payload.Serialize(),
            };
            Communicate(message);
        }

        public void BegForInfo()
        {
            PlayerMessage message = new PlayerMessage()
            {
                MessageID = (int)MessageType.BegForInfo,
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

            message.Payload = payload.Serialize();

            Communicate(message);
        }

        public void GiveInfo(bool toLeader = false)
        {
            if (WaitingPlayers.Count < 1 && !toLeader)
                return;

            PlayerMessage message = new PlayerMessage()
            {
                MessageID = (int)MessageType.GiveInfo,
                AgentID = id,
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

        private PlayerMessage CreateMessage(MessageType type, Payload payload)
        {
            return new PlayerMessage()
            {
                MessageID = (int)type,
                AgentID = id,
                Payload = payload.Serialize(),
            };
        }

        public void CheckPiece()
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = CreateMessage(MessageType.CheckPiece, payload);
            Communicate(message);
        }

        public void Discover()
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = CreateMessage(MessageType.Discover, payload);
            Communicate(message);
        }

        public void AcceptMessage()
        {
            GMMessage message;
            if (queue.TryReceive(null, out message))
            {
                switch (message.Id)
                {
                    case (int)MessageID.CheckAnswer:
                        CheckAnswerPayload payloadCheck = JsonConvert.DeserializeObject<CheckAnswerPayload>(message.Payload);
                        if (payloadCheck.sham) HavePiece = false;
                        break;
                    case (int)MessageID.DestructionAnswer:
                        HavePiece = false;
                        break;
                    case (int)MessageID.DiscoverAnswer:
                        DiscoveryAnswerPayload payloadDiscover = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.Payload);
                        Board[Position.Item1, Position.Item2].DistToPiece = payloadDiscover.distanceFromCurrent;
                        Board[Position.Item1 + 1, Position.Item2].DistToPiece = payloadDiscover.distanceE;
                        Board[Position.Item1 - 1, Position.Item2].DistToPiece = payloadDiscover.distanceW;
                        Board[Position.Item1, Position.Item2 + 1].DistToPiece = payloadDiscover.distanceS;
                        Board[Position.Item1, Position.Item2 - 1].DistToPiece = payloadDiscover.distanceN;
                        Board[Position.Item1 + 1, Position.Item2 + 1].DistToPiece = payloadDiscover.distanceSE;
                        Board[Position.Item1 - 1, Position.Item2 - 1].DistToPiece = payloadDiscover.distanceNW;
                        Board[Position.Item1 + 1, Position.Item2 - 1].DistToPiece = payloadDiscover.distanceNE;
                        Board[Position.Item1 - 1, Position.Item2 + 1].DistToPiece = payloadDiscover.distanceSW;
                        break;
                    case (int)MessageID.EndGame:
                        EndGamePayload payloadEnd = JsonConvert.DeserializeObject<EndGamePayload>(message.Payload);
                        winner = payloadEnd.winner;
                        Stop();
                        break;
                    case (int)MessageID.StartGame:
                        StartGamePayload payloadStart = JsonConvert.DeserializeObject<StartGamePayload>(message.Payload);
                        id = payloadStart.agentID;
                        TeamMatesIds = payloadStart.alliesIDs;
                        if (id == payloadStart.leaderID) IsLeader = true;
                        else IsLeader = false;
                        team = (Team)Enum.Parse(typeof(Team), payloadStart.teamId);
                        Board = new Field[payloadStart.boardSize.x, payloadStart.boardSize.y];
                        for (int i = 0; i < payloadStart.boardSize.x; i++)
                        {
                            for (int j = 0; j < payloadStart.boardSize.y; j++)
                            {
                                Board[i, j] = new Field();
                            }
                        }
                        penaltiesTimes = payloadStart.penalties;
                        Position = new Tuple<int, int>(payloadStart.position.x, payloadStart.position.y);
                        enemiesIDs = payloadStart.enemiesIDs;
                        goalAreaSize = payloadStart.goalAreaSize;
                        numberOfPlayers = payloadStart.numberOfPlayers;
                        numberOfPieces = payloadStart.numberOfPieces;
                        numberOfGoals = payloadStart.numberOfGoals;
                        shamPieceProbability = payloadStart.shamPieceProbability;
                        WaitingPlayers = new List<int>();
                        break;
                    case (int)MessageID.BegForInfoForwarded:
                        BegForInfoForwardedPayload payloadBeg = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.Payload);
                        if (team == (Team)Enum.Parse(typeof(Team), payloadBeg.teamId))
                        {
                            if (payloadBeg.leader)
                            {
                                GiveInfo(true);
                            }
                            else
                            {
                                WaitingPlayers.Add(payloadBeg.askingID);
                            }
                        }
                        break;
                    case (int)MessageID.JoinTheGameAnswer:
                        JoinAnswerPayload payloadJoin = JsonConvert.DeserializeObject<JoinAnswerPayload>(message.Payload);
                        if (id != payloadJoin.agentID) id = payloadJoin.agentID;
                        if (!payloadJoin.accepted) Stop();
                        break;
                    case (int)MessageID.MoveAnswer:
                        MoveAnswerPayload payloadMove = JsonConvert.DeserializeObject<MoveAnswerPayload>(message.Payload);
                        if (payloadMove.madeMove)
                        {
                            Position = new Tuple<int, int>(payloadMove.currentPosition.x, payloadMove.currentPosition.y);
                            Board[Position.Item1, Position.Item2].DistToPiece = payloadMove.closestPiece;
                        }
                        break;
                    case (int)MessageID.PickAnswer:
                        HavePiece = true;
                        break;
                    case (int)MessageID.PutAnswer:
                        HavePiece = false;
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
