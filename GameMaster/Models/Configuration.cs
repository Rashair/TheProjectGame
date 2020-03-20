using System;

namespace GameMaster.Models
{
    public class Configuration
    {
        public string CsIP { get; set; }

        public int CsPort { get; set; }

        public int MovePenalty { get; set; }

        public int AskPenalty { get; set; }

        public int DiscoverPenalty { get; set; }

        public int PutPenalty { get; set; }

        public int CheckPenalty { get; set; }

        public int ResponsePenalty { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int GoalAreaHeight { get; set; }

        public int NumberOfGoals { get; set; }

        public int MaximumNumberOfPiecesOnBoard { get; set; }

        public double ShamPieceProbability { get; set; } // percentage
        public int NumberOfPlayersPerTeam { get; set; }
    }
}
