namespace Shared.Models.Payloads
{
    public class StartGamePayload
    {
        public int agentID;
        public int[] alliesIDs;
        public int leaderID;
        public int[] enemiesIDs;
        public string teamId;
        public BoardSize boardSize;
        public int goalAreaSize;
        public NumberOfPlayers numberOfPlayers;
        public int numberOfPieces;
        public int numberOfGoals;
        public Penalties penalties;
        public float shamPieceProbability;
        public Position position;
    }
}
