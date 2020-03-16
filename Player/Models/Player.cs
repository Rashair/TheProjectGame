using System;
using System.Collections.Generic;
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

        public Player()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
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
        }

        public void Put()
        {
            RegularMessage message = new RegularMessage()
            {
                messageID = (int)MessageID.Put,
                agentID = id
            };
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

        public void GiveInfo()
        {
            if (waitingPlayers.Count < 1)
                return;

            GiveInfoMessage message = new GiveInfoMessage()
            {
                messageID = (int)MessageID.GiveInfo,
                agentID = id,
                //payload = ,
                respondToID = waitingPlayers[0],
                distances = new int[boardSize.x, boardSize.y],
                redTeamGoalAreaInformations = new GoalInfo[boardSize.x, boardSize.y],
                blueTeamGoalAreaInformations = new GoalInfo[boardSize.x, boardSize.y],
            };

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
            Communicate(JsonConvert.SerializeObject(message));
        }

        public void RequestsResponse()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        private void Communicate(string message)
        {
            throw new NotImplementedException();
        }

        private void Penalty()
        {
            throw new NotImplementedException();
        }
    }
}