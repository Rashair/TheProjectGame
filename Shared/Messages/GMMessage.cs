using Newtonsoft.Json;
using Shared.Enums;
using Shared.Payloads;
using Shared.Payloads.GMPayloads;

namespace Shared.Messages
{
    public class GMMessage : Message
    {
        public GMMessageId MessageID { get; set; }

        public GMMessage()
        {
        }

        public GMMessage(GMMessageId id, int playerId, Payload payload)
        {
            MessageID = id;
            AgentID = playerId;
            Payload = payload.Serialize();
        }
    }
}
