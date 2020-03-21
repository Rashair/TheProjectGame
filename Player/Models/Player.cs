using System;
using System.Collections.Generic;
using System.Threading;
using Player.Models.Payloads;
using Player.Models.Strategies;
using Shared;
using Shared.Senders;
using System.Threading.Tasks;
using Shared.Enums;
using Shared.Models.Messages;

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
            message.payload = response.Serialize();
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
        }
    }
}