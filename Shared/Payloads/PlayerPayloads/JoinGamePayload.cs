using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads
{
    public class JoinGamePayload : Payload
    {
        [JsonProperty("teamID")]
        public Team TeamID { get; set; }
    }
}
