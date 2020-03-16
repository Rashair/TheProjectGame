using System;
using System.Collections.Generic;
using System.Threading;
using CommunicationServer.Models.Messages;
using Newtonsoft.Json;
using Player.Models.Payloads;
using Player.Models.Strategies;
using Shared;
using Shared.Senders;

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

        public Player(Team _team)
        {
            team = _team;
        }

        public void JoinTheGame()
        {
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageID.JoinTheGame,
                payload = JsonConvert.SerializeObject(new JoinGamePayload()
                {
                    teamID = team.ToString()
                })
            };
            Communicate(message);
        }

        public void Start()
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
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageID.Move,
                agentID = id,
                payload = JsonConvert.SerializeObject(new MovePayload() 
                {
                    direction = _direction.ToString()
                })                
            };
            Communicate(message);
        }

        public void Put()
        {
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageID.Put,
                agentID = id,
                payload = JsonConvert.SerializeObject(new EmptyPayload())
            };
            Communicate(message);
        }

        public void BegForInfo()
        {
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageID.BegForInfo,
                agentID = id
            };
            
            Random rnd = new Random();
            int index = rnd.Next(0, teamMates.Length - 1);
            while(teamMates[index] == id)
            {
                index = rnd.Next(0, teamMates.Length - 1);
            }
            message.payload = JsonConvert.SerializeObject(new BegForInfoPayload()
            {
                askedAgentID = teamMates[index]
            });
            Communicate(message);
        }

        public void GiveInfo(bool toLeader = false)
        {
            if (waitingPlayers.Count < 1)
                return;

            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageID.GiveInfo,
                agentID = id           
                
            };

            var response = new GiveInfoPayload();
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

            for (int i = 0; i <board.Length; ++i)
            {
                response.distances[i / boardSize.y, i % boardSize.y] = board[i / boardSize.y, i % boardSize.y].distToPiece;
                if(team == Team.Red)
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
            message.payload = JsonConvert.SerializeObject(response);
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
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageID.CheckPiece,
                agentID = id,
                payload = JsonConvert.SerializeObject(new EmptyPayload())
            };
            Communicate(message);
        }

        public void Discover()
        {
            AgentMessage message = new AgentMessage()
            {
                messageID = (int)MessageID.Discover,
                agentID = id,
                payload = JsonConvert.SerializeObject(new EmptyPayload())
            };
            Communicate(message);
        }

        public void AcceptMessage()
        {
            throw new NotImplementedException();
        }

        public void MakeDecisionFromStrategy()
        {
            strategy.MakeDecision(this);
        }

        private void Communicate(AgentMessage message)
        {
            throw new NotImplementedException();
        }

        private void Penalty()
        {
            Thread.Sleep(penaltyTime);
            penaltyTime = 0;
            MakeDecisionFromStrategy();
        }
    }
}