using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Messages
{
    public class PlayerMessage
    {
        [JsonProperty("messageID")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PlayerMessageID MessageID { get; set; }

        public int PlayerID { get; set; }

        public string Payload { get; set; }
    }
}
