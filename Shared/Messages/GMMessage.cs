using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;
using Shared.Payloads;

namespace Shared.Messages
{
    public class GMMessage
    {
        [JsonProperty("id")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GMMessageID Id { get; set; }

        public string Payload { get; set; }

        public GMMessage()
        {
        }

        public GMMessage(GMMessageID id, Payload payload)
        {
            Id = id;
            Payload = payload.Serialize();
        }
    }
}
