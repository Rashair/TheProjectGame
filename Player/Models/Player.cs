using Newtonsoft.Json;
using Player.Clients;
using Player.Models.Payloads;
using Player.Models.Strategies;
using Shared;
using Shared.Models.Messages;
using Shared.Senders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;

using System.Threading.Tasks;
using Shared.Enums;

namespace Player.Models
{
    public class Player
    {
        private int id;
        private ISender sender;
        public int penaltyTime;
        public Team team;
        public bool isLeader;
        public bool havePiece;
        public Field[,] board;
        public Tuple<int, int> position;
        public List<int> waitingPlayers;
        private IStrategy strategy;
        public int[] teamMates;
        public (int x, int y) boardSize;
        private bool working;
        private int leaderId;

        private Penalties penaltiesTimes;
        private BufferBlock<GMMessage> queue;
        private WebSocketClient<GMMessage, AgentMessage> client;

        public Player(Team _team, BufferBlock<GMMessage> _queue, WebSocketClient<GMMessage, AgentMessage> _client)
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
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageType.JoinTheGame,
                payload = payload.Serialize()
            };
            Communicate(message);
        }

        public async void Start()
        {
            working = true;
            while (working)
            {
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
            AgentMessage message = new AgentMessage()
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
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageType.Put,
                agentID = id,
                payload = payload.Serialize()
            };
            Communicate(message);
        }

        public void BegForInfo()
        {
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageType.BegForInfo,
                agentID = id
            };

            Random rnd = new Random();
            int index = rnd.Next(0, teamMates.Length - 1);
            while (teamMates[index] == id)
            {
                index = rnd.Next(0, teamMates.Length - 1);
            }

            BegForInfoPayload payload = new BegForInfoPayload()
            {
                askedAgentID = teamMates[index]
            };
            message.payload = payload.Serialize();

            Communicate(message);
        }

        public void GiveInfo(bool toLeader = false)
        {
            if (waitingPlayers.Count < 1)
                return;

            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageType.GiveInfo,
                agentID = id

            };

            GiveInfoPayload response = new GiveInfoPayload(); //TODO
            if (toLeader)
                response.respondToID = leaderId;
            else
            {
                response.respondToID = waitingPlayers[0];
                waitingPlayers.RemoveAt(0);
            }

            response.distances = new int[boardSize.x, boardSize.y];
            response.redTeamGoalAreaInformations = new GoalInfo[boardSize.x, boardSize.y];
            response.blueTeamGoalAreaInformations = new GoalInfo[boardSize.x, boardSize.y];

            for (int i = 0; i < board.Length; ++i)
            {
                response.distances[i / boardSize.y, i % boardSize.y] = board[i / boardSize.y, i % boardSize.y].distToPiece;
                if (team == Team.Red)
                {
                    response.redTeamGoalAreaInformations[i / boardSize.y, i % boardSize.y] = board[i / boardSize.y, i % boardSize.y].goalInfo;
                    response.blueTeamGoalAreaInformations[i / boardSize.y, i % boardSize.y] = GoalInfo.IDK;

                }
                else
                {
                    response.blueTeamGoalAreaInformations[i / boardSize.y, i % boardSize.y] = board[i / boardSize.y, i % boardSize.y].goalInfo;
                    response.redTeamGoalAreaInformations[i / boardSize.y, i % boardSize.y] = GoalInfo.IDK;
                }
            }
<<<<<<< HEAD
            
            waitingPlayers.RemoveAt(0);

            message.payload = JsonConvert.SerializeObject(response);

=======
            message.payload = response.Serialize();
>>>>>>> Code refactor
            Communicate(message);
        }

        public void RequestsResponse(int respondToID, bool isFromLeader = false)
        {
            if (isFromLeader)
            {
                GiveInfo(isFromLeader);
            }
            else
                waitingPlayers.Add(respondToID);
        }

        private AgentMessage CreateMessage(MessageType type, Payload payload)
        {
            return new AgentMessage()
            {
                messageID = (int)type,
                agentID = id,
                payload = payload.Serialize()
            };
        }

        public void CheckPiece()
        {
            EmptyPayload payload = new EmptyPayload();
            AgentMessage message = CreateMessage(MessageType.CheckPiece, payload);
            Communicate(message);
        }

        public void Discover()
        {
            EmptyPayload payload = new EmptyPayload();
            AgentMessage message = CreateMessage(MessageType.Discover, payload);
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
                        teamMates = payload5.alliesIDs;
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
                        JoinTheGame();
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
                        if (id == payload7.agentID)
                        {
                            if (payload7.accepted) Start();
                            else Stop();
                        }
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

        private async void Communicate(AgentMessage message)
        {
            //await client.SendAsync(message);
        }

        private void Penalty()
        {
            Thread.Sleep(penaltyTime);
            penaltyTime = 0;
        }
    }
}