using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads
{
    public class JoinGamePayload : Payload
    {
        [JsonProperty("teamId")]
        public Team TeamId { get; set; }
    }
}
