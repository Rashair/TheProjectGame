﻿using System;
using System.Collections.Generic;

using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Models
{
    public class GM
    {
        private static readonly int[] legalKnowledgeReplies = new int[2]; // unique from documentation considered as static

        private Dictionary<int, GMPlayer> players;
        private AbstractField[][] board;
        private Configuration conf;

        internal int redTeamPoints;
        internal int blueTeamPoints;

        public bool WasGameStarted { get; set; }

        public GM(Configuration conf)
        {
            this.conf = conf;
        }

        public async Task AcceptMessage(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (WasGameStarted)
                {
                    Console.WriteLine("Do sth");
                }
                else
                {
                    // For debugging purposes
                    Console.WriteLine("Do nothing");
                }
            }
        }

        internal Dictionary<Direction, int> Discover(AbstractField field)
        {
            throw new NotImplementedException();
        }

        internal void StartGame()
        {
            board = new AbstractField[conf.Height][];
            for (int i = 0; i < board.Length; ++i)
            {
                board[i] = new AbstractField[conf.Width];
            }

            Func<int, int, AbstractField> nonGoalFieldGenerator = (int x, int y) => new NonGoalField(x, y);
            for (int rowIt = 0; rowIt < conf.GoalAreaHeight; ++rowIt)
            {
                FillBoardRow(rowIt, nonGoalFieldGenerator);
            }

            Func<int, int, AbstractField> taskFieldGenerator = (int x, int y) => new TaskField(x, y);
            int secondGoalAreaStart = conf.Height - conf.GoalAreaHeight;
            for (int rowIt = conf.GoalAreaHeight; rowIt < secondGoalAreaStart; ++rowIt)
            {
                FillBoardRow(rowIt, taskFieldGenerator);
            }

            for (int rowIt = secondGoalAreaStart; rowIt < conf.Height; ++rowIt)
            {
                FillBoardRow(rowIt, nonGoalFieldGenerator);
            }

            // TODO : initialize rest
            players = new Dictionary<int, GMPlayer>();

            WasGameStarted = true;
        }

        private void FillBoardRow(int row, Func<AbstractField> getField)
        {
            for (int col = 0; col < board[row].Length; ++col)
            {
                board[row][col] = getField();
            }
        private void FillBoardRow(int row, Func<int, int, AbstractField> getField)
        {
            for (int col = 0; col < board[row].Length; ++col)
            {
                board[row][col] = getField(row, col);
            }
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


        internal void Work()
        {

        }

        internal void EndGame()
        {
            throw new NotImplementedException();
        }

        private void GeneratePiece()
        {
            var rand = new Random();
            bool isSham = rand.Next(0, 100) <= conf.ShamPieceProbability;
            AbstractPiece piece;
            if (isSham)
            {
                piece = new ShamPiece();
            }
            else
            {
                piece = new NormalPiece();
            }

            int taskAreaStart = conf.GoalAreaHeight;
            int taskAreaEnd = conf.Height - conf.GoalAreaHeight;
            int xCoord = rand.Next(taskAreaStart, taskAreaEnd);
            int yCoord = rand.Next(0, conf.Width);

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
