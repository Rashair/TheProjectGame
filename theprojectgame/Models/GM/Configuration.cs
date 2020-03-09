using System;

namespace TheProjectGame.Models.GM
{
    public class Configuration
    {
        public readonly TimeSpan movePenalty;
        public readonly TimeSpan askPenalty;
        public readonly TimeSpan discoverPenalty;
        public readonly TimeSpan putPenalty;
        public readonly TimeSpan checkPenalty;
        public readonly TimeSpan responsePenalty;
        public readonly int x;
        public readonly int y;
        public readonly int numberOfGoals;
    }
}