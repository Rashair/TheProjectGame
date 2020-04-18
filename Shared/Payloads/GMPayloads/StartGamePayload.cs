using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;
using Shared.Models;

namespace Shared.Payloads
{
    public class StartGamePayload : Payload
    {
        public int PlayerId { get; set; }

        public int[] AlliesIds { get; set; }

        public int LeaderId { get; set; }

        public int[] EnemiesIds { get; set; }

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
