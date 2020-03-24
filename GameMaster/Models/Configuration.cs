using System;

namespace GameMaster.Models
{
    public class Configuration
    {
        private int generatePieceInterval;

        public TimeSpan MovePenalty { get; set; }

        public TimeSpan AskPenalty { get; set; }

        public TimeSpan DiscoverPenalty { get; set; }

        public TimeSpan PutPenalty { get; set; }

        public TimeSpan CheckPenalty { get; set; }

        public TimeSpan ResponsePenalty { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int NumberOfGoals { get; set; }

        public int GoalAreaHeight { get; set; }

        /// <summary>
        /// Percentage
        /// </summary>
        public int ShamPieceProbability { get; set; }

        public int MaximumNumberOfPiecesOnBoard { get; set; }

        /// <summary>
        /// Number of handled requests before generation of piece, minimum 2
        /// </summary>
        public int GeneratePieceInterval
        {
            get => generatePieceInterval;
            set => generatePieceInterval = Math.Max(value, 2);
        }

        public int NumberOfPlayersPerTeam { get; set; }
    }
}
