using Newtonsoft.Json;
using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class BegForInfoForwardedPayload : Payload
    {
        public int AskingId { get; set; }

        public bool Leader { get; set; }

        [JsonProperty("teamId")]
        public Team TeamId { get; set; }
    }
}
