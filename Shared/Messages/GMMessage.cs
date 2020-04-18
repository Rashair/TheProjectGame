using Shared.Enums;
using Shared.Payloads;

namespace Shared.Messages
{
    public class GMMessage : Message
    {
        public GMMessageId Id { get; set; }

        public int PlayerId { get; set; }

        public string Payload { get; set; }

        public GMMessage()
        {
        }

        public GMMessage(GMMessageId id, int playerId, Payload payload)
        {
            Id = id;
            PlayerId = playerId;
            Payload = payload.Serialize();
        }
    }
}
