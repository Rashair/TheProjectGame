using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class PickErrorPayload : Payload
    {
        [JsonProperty("errorSubtype")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PickError ErrorSubtype { get; set; }

        public override string ToString()
        {
            return $"ErrorSubtype:{ErrorSubtype}";
        }
    }
}
