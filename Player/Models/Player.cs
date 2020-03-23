using Newtonsoft.Json;
using Player.Clients;
using Player.Models.Strategies;
using Shared;
using Shared.Enums;
using Shared.Models.Messages;
using Shared.Models.Payloads;
using Shared.Payloads;
using Shared.Senders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Player.Models
{
    public class Player
    {
        private int id;
        private ISender sender;
        private int penaltyTime;
        private IStrategy strategy;
        private bool working;
        public Team team;
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

        public Player(Team _team, BufferBlock<GMMessage> _queue, WebSocketClient<GMMessage, PlayerMessage> _client)
        {
            team = _team;
            queue = _queue;
            client = _client;
        }

        public void JoinTheGame()
        {
            JoinGamePayload payload = new JoinGamePayload()
            {
                teamID = team.ToString()
            };
            PlayerMessage message = new PlayerMessage()
            {
                messageID = (int)MessageType.JoinTheGame,
                payload = payload.Serialize()
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
                direction = direction.ToString()
            };
            PlayerMessage message = new PlayerMessage()
            {
                messageID = (int)MessageType.Move,
                agentID = id,
                payload = payload.Serialize()
            };
            Communicate(message);
        }

        public void Put()
        {
            EmptyPayload payload = new EmptyPayload();
            PlayerMessage message = new PlayerMessage()
            {
                messageID = (int)MessageType.Put,
                agentID = id,
                payload = payload.Serialize()
            };
            Communicate(message);
        }

        public void BegForInfo()
        {
            PlayerMessage message = new PlayerMessage()
            {
                messageID = (int)MessageType.BegForInfo,
                agentID = id
            };

            Random rnd = new Random();
            int index = rnd.Next(0, TeamMatesIds.Length - 1);
            if (TeamMatesIds[index] == id)
            {
                index = (index + 1) % TeamMatesIds.Length;
            }

            BegForInfoPayload payload = new BegForInfoPayload()
            {
                askedAgentID = TeamMatesIds[index]
            };
            message.payload = payload.Serialize();

            Communicate(message);
        }

        public void GiveInfo(bool toLeader = false)
        {
            if (WaitingPlayers.Count < 1 && !toLeader)
                return;

            PlayerMessage message = new PlayerMessage()
            {
                messageID = (int)MessageType.GiveInfo,
                agentID = id

            };

            GiveInfoPayload response = new GiveInfoPayload();
            if (toLeader)
            {
                response.respondToID = LeaderId;
            }
            else
            {
                response.respondToID = WaitingPlayers[0];
                WaitingPlayers.RemoveAt(0);
            }

            response.distances = new int[BoardSize.x, BoardSize.y];
            response.redTeamGoalAreaInformations = new GoalInfo[BoardSize.x, BoardSize.y];
            response.blueTeamGoalAreaInformations = new GoalInfo[BoardSize.x, BoardSize.y];

            for (int i = 0; i < Board.Length; ++i)
            {
                int row = i / BoardSize.y;
                int col = i % BoardSize.y;
                response.distances[row, col] = Board[row, col].distToPiece;
                if (team == Team.Red)
                {
                    response.redTeamGoalAreaInformations[row, col] = Board[row, col].goalInfo;
                    response.blueTeamGoalAreaInformations[row, col] = GoalInfo.IDK;

                }
                else
                {
                    response.blueTeamGoalAreaInformations[row, col] = Board[row, col].goalInfo;
                    response.redTeamGoalAreaInformations[row, col] = GoalInfo.IDK;
                }
            }
            
            WaitingPlayers.RemoveAt(0);

            message.payload = JsonConvert.SerializeObject(response);

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
                messageID = (int)type,
                agentID = id,
                payload = payload.Serialize()
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
                switch(message.id)
                {
                    case (int)MessageID.CheckAnswer:
                        CheckAnswerPayload payload1 = JsonConvert.DeserializeObject<CheckAnswerPayload>(message.payload);
                        if (payload1.sham) HavePiece = false;
                        break;
                    case (int)MessageID.DestructionAnswer:
                        HavePiece = false;
                        break;
                    case (int)MessageID.DiscoverAnswer:
                        DiscoveryAnswerPayload payload2 = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.payload);
                        Board[Position.Item1, Position.Item2].distToPiece = payload2.distanceFromCurrent;
                        Board[Position.Item1 + 1, Position.Item2].distToPiece = payload2.distanceE;
                        Board[Position.Item1 - 1, Position.Item2].distToPiece = payload2.distanceW;
                        Board[Position.Item1, Position.Item2 + 1].distToPiece = payload2.distanceS;
                        Board[Position.Item1, Position.Item2 - 1].distToPiece = payload2.distanceN;
                        Board[Position.Item1 + 1, Position.Item2 + 1].distToPiece = payload2.distanceSE;
                        Board[Position.Item1 - 1, Position.Item2 - 1].distToPiece = payload2.distanceNW;
                        Board[Position.Item1 + 1, Position.Item2 - 1].distToPiece = payload2.distanceNE;
                        Board[Position.Item1 - 1, Position.Item2 + 1].distToPiece = payload2.distanceSW;
                        break;
                    case (int)MessageID.EndGame:
                        EndGamePayload payload3 = JsonConvert.DeserializeObject<EndGamePayload>(message.payload);
                        winner = payload3.winner;
                        Stop();
                        break;
                    case (int)MessageID.StartGame:
                        StartGamePayload payload4 = JsonConvert.DeserializeObject<StartGamePayload>(message.payload);
                        id = payload4.agentID;
                        TeamMatesIds = payload4.alliesIDs;
                        if (id == payload4.leaderID) IsLeader = true;
                        else IsLeader = false;
                        team = (Team)Enum.Parse(typeof(Team), payload4.teamId);
                        Board = new Field[payload4.boardSize.x, payload4.boardSize.y];
                        for (int i = 0; i < payload4.boardSize.x; i++)
                        {
                            for (int j = 0; j < payload4.boardSize.y; j++)
                            {
                                Board[i, j] = new Field();
                            }
                        }
                        penaltiesTimes = payload4.penalties;
                        Position = new Tuple<int, int>(payload4.position.x, payload4.position.y);
                        enemiesIDs = payload4.enemiesIDs;
                        goalAreaSize = payload4.goalAreaSize;
                        numberOfPlayers = payload4.numberOfPlayers;
                        numberOfPieces = payload4.numberOfPieces;
                        numberOfGoals = payload4.numberOfGoals;
                        shamPieceProbability = payload4.shamPieceProbability;
                        Start();
                        break;
                    case (int)MessageID.BegForInfoForwarded:
                        BegForInfoForwardedPayload payload5 = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.payload);
                        if (team == (Team)Enum.Parse(typeof(Team), payload5.teamId))
                        {
                            if (payload5.leader)
                            {
                                GiveInfo(true);
                            }
                            else
                            {
                                WaitingPlayers.Add(payload5.askingID);
                            }
                        }
                        break;
                    case (int)MessageID.JoinTheGameAnswer:
                        JoinAnswerPayload payload6 = JsonConvert.DeserializeObject<JoinAnswerPayload>(message.payload);
                        if (id != payload6.agentID) id = payload6.agentID;
                        if (!payload6.accepted) Stop();
                        break;
                    case (int)MessageID.MoveAnswer:
                        MoveAnswerPayload payload7 = JsonConvert.DeserializeObject<MoveAnswerPayload>(message.payload);
                        if (payload7.madeMove)
                        {
                            Position = new Tuple<int, int>(payload7.currentPosition.x, payload7.currentPosition.y);
                            Board[Position.Item1, Position.Item2].distToPiece = payload7.closestPiece;
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
            CancellationToken ct = new CancellationToken();
            await client.SendAsync(message, ct);
        }

        private void Penalty()
        {
            Thread.Sleep(penaltyTime);
            penaltyTime = 0;
        }
    }
}
