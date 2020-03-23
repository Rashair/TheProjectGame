using System;

namespace GameMaster.Models
{
    public class Configuration
    {
        public TimeSpan MovePenalty { get; protected set; }

        public TimeSpan AskPenalty { get; protected set; }

        public TimeSpan DiscoverPenalty { get; protected set; }

        public TimeSpan PutPenalty { get; protected set; }

        public TimeSpan CheckPenalty { get; protected set; }

        public TimeSpan ResponsePenalty { get; protected set; }

        public int Width { get; protected set; }

        public int Height { get; protected set; }

        public int NumberOfGoals { get; protected set; }

        public int GoalAreaHeight { get; protected set; }

        public int ShamPieceProbability { get; protected set; } // percentage
    }
}
