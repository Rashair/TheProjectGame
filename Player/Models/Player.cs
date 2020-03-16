﻿using System;
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

        public void Move()
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

        public void GiveInfo()
        {
            throw new NotImplementedException();
        }

        public void RequestsResponse()
        {
            throw new NotImplementedException();
        }

        public void CheckPiece()
        {
            throw new NotImplementedException();
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