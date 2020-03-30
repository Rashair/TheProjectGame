using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads
{
    public class PickErrorPayload : Payload
    {
        [JsonProperty("errorSubtype")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PickError ErrorSubtype { get; set; }
    }
}
