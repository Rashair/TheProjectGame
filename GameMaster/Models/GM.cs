using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Shared;
using System;
using System.Collections.Generic;

namespace GameMaster.Models
{
    public class GM
    {
        private readonly Dictionary<int, GMPlayer> players;
        private readonly AbstractField[][] board;
        private static int[] legalKnowledgeReplies = new int[2]; // unique from documentation considered as static
        private Configuration conf;
        internal int redTeamPoints;
        internal int blueTeamPoints;

        public GM()
        {
            // TODO : Initialize everything
        }

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
            var rand = new Random();
            bool isSham = rand.Next(0, 100) <= conf.shamPieceProbability;
            AbstractPiece piece;
            if (isSham)
            {
                piece = new ShamPiece();
            }
            else
            {
                piece = new NormalPiece();
            }

            int taskAreaStart = conf.goalAreaHeight;
            int taskAreaEnd = conf.height - conf.goalAreaHeight;
            int xCoord = rand.Next(taskAreaStart, taskAreaEnd);
            int yCoord = rand.Next(0, conf.width);

            board[xCoord][yCoord].Put(piece);
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