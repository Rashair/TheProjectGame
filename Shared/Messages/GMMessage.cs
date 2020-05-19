using Shared.Enums;
using Shared.Payloads;

namespace Shared.Messages
{
    public class GMMessage : Message
    {
        public GMMessageId MessageID { get; set; }

        public GMMessage()
        {
        }

        public GMMessage(GMMessageId id, int agentID, Payload payload)
        {
            MessageID = id;
            AgentID = agentID;
            Payload = payload;
        }
    }
}
