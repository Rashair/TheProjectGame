using System;

namespace GameMaster.Models
{
    public class Configuration
    {
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

        public int ShamPieceProbability { get; set; } // percentage

        public int MaximumNumberOfPiecesOnBoard { get; set; }
        public int GeneratePieceInterval { get; set; }

        public int NumberOfPlayersPerTeam { get; set; }
    }
}
