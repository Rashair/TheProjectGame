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
                agentID = id,
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