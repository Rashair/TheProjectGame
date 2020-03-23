using Newtonsoft.Json;
using Player.Clients;
using Player.Models.Strategies;
using Shared;
using Shared.Enums;
using Shared.Models;
using Shared.Models.Messages;
using Shared.Models.Payloads;
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
        public readonly Team team;
        public bool IsLeader { get; private set; }
        public bool HavePiece { get; private set; }
        public Field[,] Board { get; private set; }
        public Tuple<int, int> Position { get; private set; }
        public List<int> WaitingPlayers { get; private set; }
        public int[] TeamMatesIds { get; private set; }
        public int LeaderId { get; private set; }
        public (int x, int y) BoardSize { get; private set; }

        private Penalties penaltiesTimes;
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
            
            waitingPlayers.RemoveAt(0);

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
                        if (payload1.sham) havePiece = false;
                        break;
                    case (int)MessageID.DestructionAnswer:
                        EmptyAnswerPayload payload2 = JsonConvert.DeserializeObject<EmptyAnswerPayload>(message.payload);
                        havePiece = false;
                        break;
                    case (int)MessageID.DiscoverAnswer:
                        DiscoveryAnswerPayload payload3 = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.payload);
                        board[position.Item1, position.Item2].distToPiece = payload3.distanceFromCurrent;
                        board[position.Item1 + 1, position.Item2].distToPiece = payload3.distanceE;
                        board[position.Item1 - 1, position.Item2].distToPiece = payload3.distanceW;
                        board[position.Item1, position.Item2 + 1].distToPiece = payload3.distanceS;
                        board[position.Item1, position.Item2 - 1].distToPiece = payload3.distanceN;
                        board[position.Item1 + 1, position.Item2 + 1].distToPiece = payload3.distanceSE;
                        board[position.Item1 - 1, position.Item2 - 1].distToPiece = payload3.distanceNW;
                        board[position.Item1 + 1, position.Item2 - 1].distToPiece = payload3.distanceNE;
                        board[position.Item1 - 1, position.Item2 + 1].distToPiece = payload3.distanceSW;
                        break;
                    case (int)MessageID.EndGame:
                        EndGamePayload payload4 = JsonConvert.DeserializeObject<EndGamePayload>(message.payload);
                        //winner not used
                        Stop();
                        break;
                    case (int)MessageID.StartGame:
                        StartGamePayload payload5 = JsonConvert.DeserializeObject<StartGamePayload>(message.payload);
                        id = payload5.agentID;
                        teamMatesIds = payload5.alliesIDs;
                        if (id == payload5.leaderID) isLeader = true;
                        else isLeader = false;
                        team = (Team)Enum.Parse(typeof(Team), payload5.teamId);
                        board = new Field[payload5.boardSize.x, payload5.boardSize.y];
                        for (int i = 0; i < payload5.boardSize.x; i++)
                        {
                            for (int j = 0; j < payload5.boardSize.y; j++)
                            {
                                board[i, j] = new Field();
                            }
                        }
                        penaltiesTimes = payload5.penalties;
                        position = new Tuple<int, int>(payload5.position.x, payload5.position.y);
                        //enemiesIDs, goalAreaSize, numberOfPlayers, numberOfPieces, numberOfGoals, shamPieceProbability not used
                        Start();
                        break;
                    case (int)MessageID.BegForInfoForwarded:
                        BegForInfoForwardedPayload payload6 = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.payload);
                        if (team == (Team)Enum.Parse(typeof(Team), payload6.teamId))
                        {
                            if (payload6.leader)
                            {
                                GiveInfo(true);
                            }
                            else
                            {
                                waitingPlayers.Add(payload6.askingID);
                            }
                        }
                        break;
                    case (int)MessageID.JoinTheGameAnswer:
                        JoinAnswerPayload payload7 = JsonConvert.DeserializeObject<JoinAnswerPayload>(message.payload);
                        if (id != payload7.agentID) id = payload7.agentID;
                        if (!payload7.accepted) Stop();
                        break;
                    case (int)MessageID.MoveAnswer:
                        MoveAnswerPayload payload8 = JsonConvert.DeserializeObject<MoveAnswerPayload>(message.payload);
                        if (payload8.madeMove)
                        {
                            position = new Tuple<int, int>(payload8.currentPosition.x, payload8.currentPosition.y);
                            board[position.Item1, position.Item2].distToPiece = payload8.closestPiece;
                        }
                        break;
                    case (int)MessageID.PickAnswer:
                        EmptyAnswerPayload payload9 = JsonConvert.DeserializeObject<EmptyAnswerPayload>(message.payload);
                        havePiece = true;
                        break;
                    case (int)MessageID.PutAnswer:
                        EmptyAnswerPayload payload10 = JsonConvert.DeserializeObject<EmptyAnswerPayload>(message.payload);
                        havePiece = false;
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
            await client.SendAsync(message);
        }

        private void Penalty()
        {
            Thread.Sleep(penaltyTime);
            penaltyTime = 0;
        }
    }
}