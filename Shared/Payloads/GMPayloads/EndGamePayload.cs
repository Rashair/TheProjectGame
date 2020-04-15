using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads
{
    public class EndGamePayload : Payload
    {
        [JsonProperty("winner")]
        public Team Winner { get; set; }
    }
}
