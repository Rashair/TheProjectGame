using Newtonsoft.Json;
using Player.Clients;
using Player.Models.Strategies;
using Shared;
using Shared.Models;
using Shared.Models.Messages;
using Shared.Models.Payloads;
using Shared.Senders;
using System;
using System.Collections.Generic;
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

        private Penalties penaltiesTimes;
        private string winner;
        private int[] enemiesIDs;
        private int goalAreaSize;
        private NumberOfPlayers numberOfPlayers;
        private int numberOfPieces;
        private int numberOfGoals;
        private float shamPieceProbability;
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
            throw new NotImplementedException();
        }

        public async void Start()
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
                        havePiece = false;
                        break;
                    case (int)MessageID.DiscoverAnswer:
                        DiscoveryAnswerPayload payload2 = JsonConvert.DeserializeObject<DiscoveryAnswerPayload>(message.payload);
                        board[position.Item1, position.Item2].distToPiece = payload2.distanceFromCurrent;
                        board[position.Item1 + 1, position.Item2].distToPiece = payload2.distanceE;
                        board[position.Item1 - 1, position.Item2].distToPiece = payload2.distanceW;
                        board[position.Item1, position.Item2 + 1].distToPiece = payload2.distanceS;
                        board[position.Item1, position.Item2 - 1].distToPiece = payload2.distanceN;
                        board[position.Item1 + 1, position.Item2 + 1].distToPiece = payload2.distanceSE;
                        board[position.Item1 - 1, position.Item2 - 1].distToPiece = payload2.distanceNW;
                        board[position.Item1 + 1, position.Item2 - 1].distToPiece = payload2.distanceNE;
                        board[position.Item1 - 1, position.Item2 + 1].distToPiece = payload2.distanceSW;
                        break;
                    case (int)MessageID.EndGame:
                        EndGamePayload payload3 = JsonConvert.DeserializeObject<EndGamePayload>(message.payload);
                        winner = payload3.winner;
                        Stop();
                        break;
                    case (int)MessageID.StartGame:
                        StartGamePayload payload4 = JsonConvert.DeserializeObject<StartGamePayload>(message.payload);
                        id = payload4.agentID;
                        teamMates = payload4.alliesIDs;
                        if (id == payload4.leaderID) isLeader = true;
                        else isLeader = false;
                        team = (Team)Enum.Parse(typeof(Team), payload4.teamId);
                        board = new Field[payload4.boardSize.x, payload4.boardSize.y];
                        for (int i = 0; i < payload4.boardSize.x; i++)
                        {
                            for (int j = 0; j < payload4.boardSize.y; j++)
                            {
                                board[i, j] = new Field();
                            }
                        }
                        penaltiesTimes = payload4.penalties;
                        position = new Tuple<int, int>(payload4.position.x, payload4.position.y);
                        enemiesIDs = payload4.enemiesIDs;
                        goalAreaSize = payload4.goalAreaSize;
                        numberOfPlayers = payload4.numberOfPlayers;
                        numberOfPieces = payload4.numberOfPieces;
                        numberOfGoals = payload4.numberOfGoals;
                        shamPieceProbability = payload4.shamPieceProbability;
                        Start();
                        break;
                    case (int)MessageID.BegForInfoForwarded:
                        BegForInfoForwardedPayload payload5 = JsonConvert.DeserializeObject<BegForInfoForwardedPayload>(message.payload);
                        if (team == (Team)Enum.Parse(typeof(Team), payload5.teamId))
                        {
                            if (payload5.leader)
                            {
                                GiveInfo(true);
                            }
                            else
                            {
                                waitingPlayers.Add(payload5.askingID);
                            }
                        }
                        break;
                    case (int)MessageID.JoinTheGameAnswer:
                        JoinAnswerPayload payload6 = JsonConvert.DeserializeObject<JoinAnswerPayload>(message.payload);
                        if (id != payload6.agentID) id = payload6.agentID;
                        if (!payload6.accepted) Stop();
                        break;
                    case (int)MessageID.MoveAnswer:
                        MoveAnswerPayload payload7 = JsonConvert.DeserializeObject<MoveAnswerPayload>(message.payload);
                        if (payload7.madeMove)
                        {
                            position = new Tuple<int, int>(payload7.currentPosition.x, payload7.currentPosition.y);
                            board[position.Item1, position.Item2].distToPiece = payload7.closestPiece;
                        }
                        break;
                    case (int)MessageID.PickAnswer:
                        havePiece = true;
                        break;
                    case (int)MessageID.PutAnswer:
                        havePiece = false;
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

        private async void Communicate(AgentMessage message)
        {
            await client.SendAsync(message);
        }

        private void Penalty()
        {
            throw new NotImplementedException();
        }
    }
}