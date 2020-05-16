using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads.PlayerPayloads
{
    public class MovePayload : Payload
    {
        [JsonProperty("direction")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Direction Direction { get; set; }

        public override string ToString()
        {
            return $"Direction:{Direction}";
        }
    }
}
