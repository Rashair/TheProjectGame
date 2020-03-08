using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace theprojectgame.Models.Player
{
    public enum Team { Red, Blue };

    public enum GoalInfo { IDK, DiscoveredNotGoal, DiscoveredGoal };

    public interface ISender
    {
        void SendMessage();
    }

    public class Field
    {
        public GoalInfo goalInfo;
        public bool playerInfo;
        public int distToPiece;
    }

    public class Strategy: IStrategy
    {
        public void MakeDecision()
        {
            throw new NotImplementedException();
        }
    }

    public interface IStrategy
    {
        void MakeDecision();
    }

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
        private void Communicate()
        {
            throw new NotImplementedException();
        }
        private void Penalty()
        {
            throw new NotImplementedException();
        }
    }
}