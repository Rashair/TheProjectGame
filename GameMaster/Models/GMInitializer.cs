using System;
using System.Collections.Generic;

using GameMaster.Models.Fields;
using Shared.Enums;

namespace GameMaster.Models
{
    public class GMInitializer
    {
        private readonly GameConfiguration conf;
        private readonly AbstractField[][] board;
        private readonly Random rand;

        public int SecondGoalAreaStart { get => conf.Height - conf.GoalAreaHeight; }

        public GMInitializer(GameConfiguration conf, AbstractField[][] board)
        {
            this.conf = conf;
            this.board = board;
            this.rand = new Random();
        }

        public void InitializeBoard()
        {
            for (int i = 0; i < board.Length; ++i)
            {
                board[i] = new AbstractField[conf.Width];
            }

            GenerateGoalFields(0, conf.GoalAreaHeight);
            AbstractField NonGoalFieldGenerator(int y, int x) => new NonGoalField(y, x);
            for (int rowIt = 0; rowIt < conf.GoalAreaHeight; ++rowIt)
            {
                FillBoardRow(rowIt, NonGoalFieldGenerator);
            }

            AbstractField TaskFieldGenerator(int y, int x) => new TaskField(y, x);
            for (int rowIt = conf.GoalAreaHeight; rowIt < SecondGoalAreaStart; ++rowIt)
            {
                FillBoardRow(rowIt, TaskFieldGenerator);
            }

            GenerateGoalFields(SecondGoalAreaStart, conf.Height);
            for (int rowIt = SecondGoalAreaStart; rowIt < conf.Height; ++rowIt)
            {
                FillBoardRow(rowIt, NonGoalFieldGenerator);
            }
        }

        private void GenerateGoalFields(int beg, int end)
        {
            for (int i = 0; i < conf.NumberOfGoals; ++i)
            {
                int row = rand.Next(beg, end);
                int col = rand.Next(conf.Width);
                while (board[row][col] != null)
                {
                    ++col;
                    if (col == conf.Width)
                    {
                        col = 0;
                        ++row;
                        if (row == end)
                        {
                            row = beg;
                        }
                    }
                }
                board[row][col] = new GoalField(row, col);
            }
        }

        private void FillBoardRow(int row, Func<int, int, AbstractField> getField)
        {
            for (int col = 0; col < board[row].Length; ++col)
            {
                // Goal-field generation
                if (board[row][col] == null)
                {
                    board[row][col] = getField(row, col);
                }
            }
        }

        public void GenerateAllPieces(Action generator)
        {
            for (int i = 0; i < conf.NumberOfPiecesOnBoard; ++i)
            {
                generator();
            }
        }

        public void InitializePlayersPoisitions(IEnumerable<KeyValuePair<int, GMPlayer>> players)
        {
            foreach (var p in players)
            {
                GMPlayer player = p.Value;
                (int y1, int y2) = GetBoundaries(player.Team);
                int y = rand.Next(y1, y2);
                int x = rand.Next(0, conf.Width);

                AbstractField pos = board[y][x];
                while (!pos.MoveHere(player))
                {
                    ++x;
                    if (x == conf.Width)
                    {
                        x = 0;
                        ++y;
                        if (y == y2)
                        {
                            y = y1;
                        }
                    }

                    pos = board[y][x];
                }
            }
        }

        private (int y1, int y2) GetBoundaries(Team team)
        {
            if (team == Team.Red)
            {
                return (0, SecondGoalAreaStart);
            }

            return (conf.GoalAreaHeight, conf.Height);
        }
    }
}
