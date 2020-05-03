using System;

using GameMaster.Models.Fields;
using Shared.Enums;

namespace GameMaster.Models
{
    public class GMInitializer
    {
        private readonly GM gm;

        public GMInitializer(GM gm)
        {
            this.gm = gm;
        }

        public void InitializePlayersPoisitions()
        {
            var rand = new Random();
            foreach (var p in gm.Players)
            {
                GMPlayer player = p.Value;
                (int y1, int y2) = GetBoundaries(player.Team);
                int y = rand.Next(y1, y2);
                int x = rand.Next(0, gm.Conf.Width);

                AbstractField pos = gm.Board[y][x];
                while (!pos.MoveHere(player))
                {
                    ++x;
                    if (x == gm.Conf.Width)
                    {
                        x = 0;
                        ++y;
                        if (y == y2)
                        {
                            y = y1;
                        }
                    }

                    pos = gm.Board[y][x];
                }
            }
        }

        private (int y1, int y2) GetBoundaries(Team team)
        {
            if (team == Team.Red)
            {
                return (0, gm.SecondGoalAreaStart);
            }

            return (gm.Conf.GoalAreaHeight, gm.Conf.Height);
        }

        public void InitializeBoard()
        {
            gm.Board = new AbstractField[gm.Conf.Height][];
            for (int i = 0; i < gm.Board.Length; ++i)
            {
                gm.Board[i] = new AbstractField[gm.Conf.Width];
            }

            GenerateGoalFields(0, gm.Conf.GoalAreaHeight);
            AbstractField NonGoalFieldGenerator(int y, int x) => new NonGoalField(y, x);
            for (int rowIt = 0; rowIt < gm.Conf.GoalAreaHeight; ++rowIt)
            {
                FillBoardRow(rowIt, NonGoalFieldGenerator);
            }

            AbstractField TaskFieldGenerator(int y, int x) => new TaskField(y, x);
            for (int rowIt = gm.Conf.GoalAreaHeight; rowIt < gm.SecondGoalAreaStart; ++rowIt)
            {
                FillBoardRow(rowIt, TaskFieldGenerator);
            }

            GenerateGoalFields(gm.SecondGoalAreaStart, gm.Conf.Height);
            for (int rowIt = gm.SecondGoalAreaStart; rowIt < gm.Conf.Height; ++rowIt)
            {
                FillBoardRow(rowIt, NonGoalFieldGenerator);
            }
        }

        private void GenerateGoalFields(int beg, int end)
        {
            for (int i = 0; i < gm.Conf.NumberOfGoals; ++i)
            {
                int row = gm.Rand.Next(beg, end);
                int col = gm.Rand.Next(gm.Conf.Width);
                while (gm.Board[row][col] != null)
                {
                    ++col;
                    if (col == gm.Conf.Width)
                    {
                        col = 0;
                        ++row;
                        if (row == end)
                        {
                            row = beg;
                        }
                    }
                }
                gm.Board[row][col] = new GoalField(row, col);
            }
        }

        private void FillBoardRow(int row, Func<int, int, AbstractField> getField)
        {
            for (int col = 0; col < gm.Board[row].Length; ++col)
            {
                // Goal-field generation
                if (gm.Board[row][col] == null)
                {
                    gm.Board[row][col] = getField(row, col);
                }
            }
        }

        public void GenerateAllPieces()
        {
            for (int i = 0; i < gm.Conf.NumberOfPiecesOnBoard; ++i)
            {
                gm.GeneratePiece();
            }
        }
    }
}
