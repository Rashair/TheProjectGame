using Newtonsoft.Json;
using Player.Clients;
using Player.Models.Messages;
using Player.Models.Payloads;
using Player.Models.Strategies;
using Shared;
using Shared.Models.Messages;
using Shared.Senders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;

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
            JoinGameRequestMessage message = new JoinGameRequestMessage()
            {
                messageID = (int)MessageID.JoinTheGame,
                teamID = team.ToString(),
                //payload = 
            };
            Communicate(message);
        }

        public async void Start()
        {
            working = true;
            while(working)
            {
                Penalty();
            }
        }

        public void Stop()
        {
            working = false;
        }

        public void Move(Directions _direction)
        {
            MoveMessage message = new MoveMessage()
            {
                messageID = (int)MessageID.Move,
                agentID = id,
                //payload = ,
                direction = _direction.ToString()
            };
            Communicate(message);
        }

        public void Put()
        {
            RegularMessage message = new RegularMessage()
            {
                messageID = (int)MessageID.Put,
                agentID = id
            };
            Communicate(message);
        }

        public void BegForInfo()
        {
            BegForInfoMessage message = new BegForInfoMessage()
            {
                messageID = (int)MessageID.BegForInfo,
                agentID = id,
                //payload = ,
            };
            
            Random rnd = new Random();
            int index = rnd.Next(0, teamMates.Length - 1);
            while(teamMates[index] == id)
            {
                index = rnd.Next(0, teamMates.Length - 1);
            }
            message.askedAgentID = teamMates[index];
            Communicate(message);
        }

        public void GiveInfo(bool toLeader = false)
        {
            if (waitingPlayers.Count < 1)
                return;

            GiveInfoMessage message = new GiveInfoMessage()
            {
                messageID = (int)MessageID.GiveInfo,
                agentID = id,
                //payload = ,              
                distances = new int[boardSize.x, boardSize.y],
                redTeamGoalAreaInformations = new GoalInfo[boardSize.x, boardSize.y],
                blueTeamGoalAreaInformations = new GoalInfo[boardSize.x, boardSize.y],
            };
            if (toLeader)
                message.respondToID = leaderId;
            else
            {
                message.respondToID = waitingPlayers[0];
                waitingPlayers.RemoveAt(0);
            }

            for (int i = 0; i <board.Length; ++i)
            {
                message.distances[i / boardSize.y, i % boardSize.y] = board[i / boardSize.y, i % boardSize.y].distToPiece;
                if(team == Team.Red)
                {
                    message.redTeamGoalAreaInformations[i / boardSize.y, i % boardSize.y] = board[i / boardSize.y, i % boardSize.y].goalInfo;
                    message.blueTeamGoalAreaInformations[i / boardSize.y, i % boardSize.y] = GoalInfo.IDK;

                }
                else
                {
                    message.blueTeamGoalAreaInformations[i / boardSize.y, i % boardSize.y] = board[i / boardSize.y, i % boardSize.y].goalInfo;
                    message.redTeamGoalAreaInformations[i / boardSize.y, i % boardSize.y] = GoalInfo.IDK;
                }
            }
            
            waitingPlayers.RemoveAt(0);
            Communicate(message);
        }

        public void RequestsResponse(int respondToID, bool _isLeader = false)
        {
            if (_isLeader)
            {
                GiveInfo(_isLeader);
            }
            else
                waitingPlayers.Add(respondToID);
        }

        public void CheckPiece()
        {
            RegularMessage message = new RegularMessage()
            {
                messageID = (int)MessageID.CheckPiece,
                agentID = id
            };
            Communicate(message);
        }

        public void Discover()
        {
            RegularMessage message = new RegularMessage()
            {
                messageID = (int)MessageID.Discover,
                agentID = id
            };
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

        private async void Communicate(Message message)
        {
            //await client.SendAsync(message);
        }

        private void Penalty()
        {
            Thread.Sleep(penaltyTime);
            penaltyTime = 0;
            MakeDecisionFromStrategy();
        }
    }
}