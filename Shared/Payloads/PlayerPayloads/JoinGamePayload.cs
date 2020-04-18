using Newtonsoft.Json;
using Shared.Enums;

namespace Shared.Payloads.PlayerPayloads
{
    public class JoinGamePayload : Payload
    {
        [JsonProperty("teamID")]
        public Team TeamID { get; set; }
    }
}
