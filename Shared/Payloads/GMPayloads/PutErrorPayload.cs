using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Shared.Enums;

namespace Shared.Payloads
{
    public class PutErrorPayload : Payload
    {
        [JsonProperty("errorSubtype")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PutError ErrorSubtype { get; set; }
    }
}
