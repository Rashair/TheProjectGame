using Newtonsoft.Json;
using Shared.Enums;

namespace Shared.Payloads.PlayerPayloads
{
    public class JoinGamePayload : Payload
    {
        [JsonProperty("teamId")]
        public Team TeamId { get; set; }

        public override string ToString()
        {
            return $"TeamId:{TeamId}";
        }
    }
}
