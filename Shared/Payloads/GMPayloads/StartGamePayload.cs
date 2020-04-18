using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;
using Shared.Models;

namespace Shared.Payloads.GMPayloads
{
    public class StartGamePayload : Payload
    {
        public int PlayerID { get; set; }

        public int[] AlliesIDs { get; set; }

        public int LeaderID { get; set; }

        public int[] EnemiesIDs { get; set; }

        [JsonProperty("teamId")]
        public Team TeamId { get; set; }

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
