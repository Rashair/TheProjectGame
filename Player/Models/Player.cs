using System;
using System.Collections.Generic;
using System.Threading;
using CommunicationServer.Models.Messages;
using Newtonsoft.Json;
using Player.Models.Payloads;
using Player.Models.Strategies;
using Shared;
using Shared.Senders;
using System.Threading.Tasks;
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

        public Player(Team _team)
        {
            team = _team;
        }

        public void JoinTheGame()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public void Put()
        {
            throw new NotImplementedException();
        }

        public void BegForInfo()
        {
            throw new NotImplementedException();
        }

        public void GiveInfo(bool toLeader = false)
        {
            throw new NotImplementedException();
        }

        public void RequestsResponse(int respondToID, bool _isLeader = false)
        {
            throw new NotImplementedException();
        }

        public void CheckPiece()
        {
            throw new NotImplementedException();
        }

        public BufferBlock<GMMessage> bufferBlock = new BufferBlock<GMMessage>(); //temporary data abstraction

        public bool? isPieceSham = null;
        public Penalties penaltiesTimes = null;

        public void AcceptMessage()
        {
            GMMessage message;
            if (bufferBlock.TryReceive(null, out message))
            {
                switch(message.id)
                {
                    case (int)MessageID.CheckAnswer:
                        CheckAnswerPayload payload1 = JsonConvert.DeserializeObject<CheckAnswerPayload>(message.payload);
                        if (payload1.sham) isPieceSham = true;
                        else isPieceSham = false;
                        break;
                    case (int)MessageID.DestructionAnswer:
                        EmptyAnswerPayload payload2 = JsonConvert.DeserializeObject<EmptyAnswerPayload>(message.payload);
                        isPieceSham = null;
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
                        isPieceSham = null;
                        break;
                    case (int)MessageID.PutAnswer:
                        EmptyAnswerPayload payload10 = JsonConvert.DeserializeObject<EmptyAnswerPayload>(message.payload);
                        havePiece = false;
                        isPieceSham = null;
                        break;
                    default:
                        break;
                }
            }
        }
        
        public void MakeDecisionFromStrategy()
        {
            throw new NotImplementedException();
        }

        public class ClientWebSocket
        {
            public async Task SendAsync(AgentMessage message)
            {
                throw new NotImplementedException();
            }
        } //temporary data abstraction
        public ClientWebSocket client = new ClientWebSocket(); //temporary data abstraction

        private void Communicate(AgentMessage message)
        {
            Task result = client.SendAsync(message);
        }

        private void Penalty()
        {
            throw new NotImplementedException();
        }
    }
}