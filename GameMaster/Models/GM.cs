using GameMaster.Models.Fields;
using Shared;
using System;
using System.Collections.Generic;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly Dictionary<int, GMPlayer> players;
        private readonly AbstractField[][] map;
        private static int[] legalKnowledgeReplies = new int[2]; // unique from documentation considered as static
        private Configuration conf;
        internal int redTeamPoints;
        internal int blueTeamPoints;

        public void AcceptMessage()
        {
            throw new NotImplementedException();
        }

        public void GenerateGUI()
        {
            throw new NotImplementedException();
        }

        internal Dictionary<Direction, int> Discover(AbstractField field)
        {
            throw new NotImplementedException();
        }

        internal void EndGame()
        {
            throw new NotImplementedException();
        }

        private void GeneratePiece()
        {
            throw new NotImplementedException();
        }

        private void ForwardKnowledgeQuestion()
        {
            throw new NotImplementedException();
        }

        private void ForwardKnowledgeReply()
        {
            throw new NotImplementedException();
        }
    }
}