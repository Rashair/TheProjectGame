using System;
using System.Collections.Generic;

using GameMaster.Models.Fields;
using GameMaster.Models.Pieces;
using Shared.Models.Enums;
using Shared.Models.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;

namespace GameMaster.Models
{
    public class GM
    {
        private Configuration conf;
        private BufferBlock<PlayerMessage> queue;

        private readonly int[] legalKnowledgeReplies;
        private Dictionary<int, GMPlayer> players;
        private AbstractField[][] board;
        private int piecesOnBoard;

        private int redTeamPoints;
        private int blueTeamPoints;

        public bool WasGameStarted { get; set; }

        public GM(Configuration conf, BufferBlock<PlayerMessage> queue)
        {
            this.conf = conf;
            this.queue = queue;
            legalKnowledgeReplies = new int[2];
        }

        public async Task AcceptMessage(PlayerMessage message, CancellationToken cancellationToken)
        {
            // TODO decrement `piecesOnBoard` on Put Message
        }

        internal Dictionary<Direction, int> Discover(AbstractField field)
        {
            throw new NotImplementedException();
        }

        internal void StartGame()
        {
            InitializeBoard();

            // TODO : initialize rest
            players = new Dictionary<int, GMPlayer>();

            WasGameStarted = true;
        }

        private void InitializeBoard()
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

        internal async Task Work(CancellationToken cancellationToken)
        {
            bool shouldGeneratePiece = true;
            var timer = PrepareGeneratePieceTimer((sender, e) =>
            {
                if (piecesOnBoard < conf.MaximumNumberOfPiecesOnBoard)
                {
                    shouldGeneratePiece = true;
                }
            });
            TimeSpan cancellationTimespan = TimeSpan.FromMilliseconds(50);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (queue.Count > 0)
                {
                    int maxMessagesToRead = Math.Min(conf.NumberOfPlayersPerTeam, queue.Count);
                    for (int i = 0; i < maxMessagesToRead; ++i)
                    {
                        var message = await queue.ReceiveAsync(cancellationTimespan, cancellationToken);
                        await AcceptMessage(message, cancellationToken);
                        if (conf.NumberOfGoals == blueTeamPoints || conf.NumberOfGoals == redTeamPoints)
                        {
                            EndGame();
                            break;
                        }
                    }
                }

                if (shouldGeneratePiece)
                {
                    timer.Stop();
                    GeneratePiece();
                    shouldGeneratePiece = false;
                    timer.Start();
                }
            }
        }

        private System.Timers.Timer PrepareGeneratePieceTimer(ElapsedEventHandler elapsed)
        {
            var timer = new System.Timers.Timer()
            {
                Interval = conf.GeneratePieceInterval,
                AutoReset = true,
            };
            timer.Elapsed += elapsed;

            return timer;
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
            piecesOnBoard += 1;
        }

        private void ForwardKnowledgeQuestion()
        {
            throw new NotImplementedException();
        }

        private void ForwardKnowledgeReply()
        {
            throw new NotImplementedException();
        }

        internal void EndGame()
        {
            throw new NotImplementedException();
        }
    }
}
