using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Player.Models.Messages;
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
            JoinGameRequestMessage message = new JoinGameRequestMessage()
            {
                messageID = (int)MessageID.JoinTheGame,
                teamID = team.ToString(),
                //payload = 
            };
            Communicate(JsonConvert.SerializeObject(message));
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
            MoveMessage message = new MoveMessage()
            {
                messageID = (int)MessageID.Move,
                agentID = id,
                //payload = ,
                direction = _direction.ToString()
            };
            Communicate(JsonConvert.SerializeObject(message));
        }

        public void Put()
        {
            RegularMessage message = new RegularMessage()
            {
                messageID = (int)MessageID.Put,
                agentID = id
            };
            Communicate(JsonConvert.SerializeObject(message));
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
            Communicate(JsonConvert.SerializeObject(message));
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
            
            Communicate(JsonConvert.SerializeObject(message));
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
            Communicate(JsonConvert.SerializeObject(message));
        }

        public void Discover()
        {
            RegularMessage message = new RegularMessage()
            {
                messageID = (int)MessageID.Discover,
                agentID = id
            };
            Communicate(JsonConvert.SerializeObject(message));
        }

        public void AcceptMessage()
        {
            throw new NotImplementedException();
        }

        public void MakeDecisionFromStrategy()
        {
            strategy.MakeDecision(this);
        }

        private void Communicate(string message)
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