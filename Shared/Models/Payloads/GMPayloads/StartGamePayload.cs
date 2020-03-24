namespace Shared.Models.Payloads
{
    public class StartGamePayload : Payload
    {
        public int AgentID { get; set; }

        public int[] AlliesIDs { get; set; }

        public int LeaderID { get; set; }

        public int[] EnemiesIDs { get; set; }

        public string TeamId { get; set; }

        public BoardSize BoardSize { get; set; }

        public int GoalAreaSize { get; set; }

        public NumberOfPlayers NumberOfPlayers { get; set; }

        public int NumberOfPieces { get; set; }

        public int NumberOfGoals { get; set; }

        public Penalties Penalties { get; set; }

        public float ShamPieceProbability { get; set; }

        public Position Position { get; set; }
    }
}
