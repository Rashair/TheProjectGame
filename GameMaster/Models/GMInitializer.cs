using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using GameMaster.Models.Fields;
using Microsoft.Extensions.Configuration;
using Serilog;
using Shared.Enums;
using Shared.Models;

using static System.Environment;

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

        public ILogger GetLogger(bool verbose)
        {
            var s = System.Environment.CurrentDirectory;
            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

            LoggerLevel level = new LoggerLevel();
            configuration.Bind("Serilog:MinimumLevel", level);
            string loggerTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {SourceContext}{NewLine}[{Level}] {Message}{NewLine}{Exception}";
            
            string folderName = Path.Combine("TheProjectGameLogs", DateTime.Today.ToString("yyyy-MM-dd"), "GameMaster");
            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            string fileName = $"gm-{DateTime.Now:HH-mm-ss}-{processId:000000}.log";
            string path = Path.Combine(GetFolderPath(SpecialFolder.MyDocuments), folderName, fileName);
            var logConfig = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.File(
               path: path,
               rollOnFileSizeLimit: true,
               outputTemplate: loggerTemplate)
               .WriteTo.Console(outputTemplate: loggerTemplate)
                .MinimumLevel.Override("Microsoft", level.Microsoft)
                .MinimumLevel.Override("System", level.System);
            if (verbose)
            {
                logConfig.MinimumLevel.Verbose();
            }
            else
            {
                level.SetMinimumLevel(logConfig);
            }
            return logConfig.CreateLogger();
        }
    }
}
