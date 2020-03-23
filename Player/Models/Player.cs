using Player.Models.Payloads;
using Player.Models.Strategies;
using Shared;
using Shared.Enums;
using Shared.Models.Messages;
using Shared.Senders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        public int[] teamMatesIds;
        public (int x, int y) boardSize;
        private bool working;
        private int leaderId;

        public Player(Team team)
        {
            this.team = team;
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
            int index = rnd.Next(0, teamMatesIds.Length - 1);
            if (teamMatesIds[index] == id)
            {
                index = (index + 1) % teamMatesIds.Length;
            }
            BegForInfoPayload payload = new BegForInfoPayload()
            {
                askedAgentID = teamMatesIds[index]
            };
            message.payload = payload.Serialize();
            Communicate(message);
        }

        public void GiveInfo(bool toLeader = false)
        {
            if (waitingPlayers.Count < 1 && !toLeader)
                return;

            PlayerMessage message = new PlayerMessage()
            {
                messageID = (int)MessageType.GiveInfo,
                agentID = id

            };

            GiveInfoPayload response = new GiveInfoPayload();
            if (toLeader)
            {
                response.respondToID = leaderId;
            }
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
                int row = i / boardSize.y;
                int col = i % boardSize.y;
                response.distances[row, col] = board[row, col].distToPiece;
                if (team == Team.Red)
                {
                    response.redTeamGoalAreaInformations[row, col] = board[row, col].goalInfo;
                    response.blueTeamGoalAreaInformations[row, col] = GoalInfo.IDK;

                }
                else
                {
                    response.blueTeamGoalAreaInformations[row, col] = board[row, col].goalInfo;
                    response.redTeamGoalAreaInformations[row, col] = GoalInfo.IDK;
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
            {
                waitingPlayers.Add(respondToID);
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
            throw new NotImplementedException();
        }

        public void MakeDecisionFromStrategy()
        {
            strategy.MakeDecision(this);
        }

        private void Communicate(PlayerMessage message)
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