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
using Shared.Models;
using Shared.Models.Messages;
using Shared.Models.Payloads;
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
        private Penalties penaltiesTimes;
        private string winner;
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

        private BufferBlock<GMMessage> queue;
        private WebSocketClient<GMMessage, PlayerMessage> client;

        public Player(Team team, IStrategy strategy, BufferBlock<GMMessage> queue, WebSocketClient<GMMessage, PlayerMessage> client)
        {
            this.team = team;
            this.strategy = strategy;
            this.queue = queue;
            this.client = client;
        }

        public void JoinTheGame()
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

        public async Task Start()
        {
            working = true;
            while (working)
            {
                await Task.Run(() => AcceptMessage());
                await Task.Run(() => MakeDecisionFromStrategy());
                await Task.Run(() => Penalty());
            }
        }

        public void Stop()
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
                        CheckAnswerPayload payload1 = JsonConvert.DeserializeObject<CheckAnswerPayload>(message.Payload);
                        if (payload1.Sham) HavePiece = false;
                        break;
                    case (int)MessageID.DestructionAnswer:
                        HavePiece = false;
                        break;
                    case (int)MessageID.DiscoverAnswer:
                        DiscoveryAnswerPayload payload2 = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.Payload);
                        Board[Position.Item1, Position.Item2].DistToPiece = payload2.DistanceFromCurrent;
                        Board[Position.Item1 + 1, Position.Item2].DistToPiece = payload2.DistanceE;
                        Board[Position.Item1 - 1, Position.Item2].DistToPiece = payload2.DistanceW;
                        Board[Position.Item1, Position.Item2 + 1].DistToPiece = payload2.DistanceS;
                        Board[Position.Item1, Position.Item2 - 1].DistToPiece = payload2.DistanceN;
                        Board[Position.Item1 + 1, Position.Item2 + 1].DistToPiece = payload2.DistanceSE;
                        Board[Position.Item1 - 1, Position.Item2 - 1].DistToPiece = payload2.DistanceNW;
                        Board[Position.Item1 + 1, Position.Item2 - 1].DistToPiece = payload2.DistanceNE;
                        Board[Position.Item1 - 1, Position.Item2 + 1].DistToPiece = payload2.DistanceSW;
                        break;
                    case (int)MessageID.EndGame:
                        EndGamePayload payload3 = JsonConvert.DeserializeObject<EndGamePayload>(message.Payload);
                        winner = payload3.Winner;
                        Stop();
                        break;
                    case (int)MessageID.StartGame:
                        StartGamePayload payload4 = JsonConvert.DeserializeObject<StartGamePayload>(message.Payload);
                        id = payload4.AgentID;
                        TeamMatesIds = payload4.AlliesIDs;
                        if (id == payload4.LeaderID) IsLeader = true;
                        else IsLeader = false;
                        team = (Team)Enum.Parse(typeof(Team), payload4.TeamId);
                        Board = new Field[payload4.BoardSize.X, payload4.BoardSize.Y];
                        for (int i = 0; i < payload4.BoardSize.X; i++)
                        {
                            for (int j = 0; j < payload4.BoardSize.Y; j++)
                            {
                                Board[i, j] = new Field();
                            }
                        }
                        penaltiesTimes = payload4.Penalties;
                        Position = new Tuple<int, int>(payload4.Position.X, payload4.Position.Y);
                        enemiesIDs = payload4.EnemiesIDs;
                        goalAreaSize = payload4.GoalAreaSize;
                        numberOfPlayers = payload4.NumberOfPlayers;
                        numberOfPieces = payload4.NumberOfPieces;
                        numberOfGoals = payload4.NumberOfGoals;
                        shamPieceProbability = payload4.ShamPieceProbability;
                        WaitingPlayers = new List<int>();
                        break;
                    case (int)MessageID.BegForInfoForwarded:
                        BegForInfoForwardedPayload payload5 = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.Payload);
                        if (team == (Team)Enum.Parse(typeof(Team), payload5.TeamId))
                        {
                            if (payload5.Leader)
                            {
                                GiveInfo(true);
                            }
                            else
                            {
                                WaitingPlayers.Add(payload5.AskingID);
                            }
                        }
                        break;
                    case (int)MessageID.JoinTheGameAnswer:
                        JoinAnswerPayload payload6 = JsonConvert.DeserializeObject<JoinAnswerPayload>(message.Payload);
                        if (id != payload6.AgentID) id = payload6.AgentID;
                        if (!payload6.Accepted) Stop();
                        break;
                    case (int)MessageID.MoveAnswer:
                        MoveAnswerPayload payload7 = JsonConvert.DeserializeObject<MoveAnswerPayload>(message.Payload);
                        if (payload7.MadeMove)
                        {
                            Position = new Tuple<int, int>(payload7.CurrentPosition.X, payload7.CurrentPosition.Y);
                            Board[Position.Item1, Position.Item2].DistToPiece = payload7.ClosestPiece;
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
