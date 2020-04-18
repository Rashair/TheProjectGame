using Shared.Enums;
using Shared.Payloads;

namespace Shared.Messages
{
    public class GMMessage : Message
    {
        public GMMessageID Id { get; set; }

        public int PlayerId { get; set; }

        public string Payload { get; set; }

        public GMMessage()
        {
        }

        public GMMessage(GMMessageID id, int playerID, Payload payload)
        {
            Id = id;
            PlayerId = playerID;
            Payload = payload.Serialize();
        }
    }
}
