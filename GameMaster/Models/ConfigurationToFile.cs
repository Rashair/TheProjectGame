namespace GameMaster.Models
{
    public class ConfigurationToFile
    {
        public string CsIP { get; set; }

        public int CsPort { get; set; }
        
        public int movePenalty { get; set; }

        public int askPenalty { get; set; }

        public int discoverPenalty { get; set; }

        public int putPenalty { get; set; }

        public int checkForShamPenalty { get; set; }

        public int responsePenalty { get; set; }

        public int boardX { get; set; }

        public int boardY { get; set; }

        public int goalAreaHeight { get; set; }

        public int numberOfGoals { get; set; }

        public int numberOfPieces { get; set; }

        public double shamPieceProbability { get; set; }
    }
}
