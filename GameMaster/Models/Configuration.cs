using System;

namespace GameMaster.Models
{
    public class Configuration
    {
        public readonly TimeSpan movePenalty;
        public readonly TimeSpan askPenalty;
        public readonly TimeSpan discoverPenalty;
        public readonly TimeSpan putPenalty;
        public readonly TimeSpan checkPenalty;
        public readonly TimeSpan responsePenalty;
        public readonly int width;
        public readonly int height;
        public readonly int numberOfGoals;
        public readonly int goalAreaHeight;
        public readonly int shamPieceProbability; // percentage
    }
}