using Shared.Enums;

namespace Shared.Messages
{
    public class PlayerMessage : Message
    {
        public PlayerMessageId MessageId { get; set; }

        public int PlayerId { get; set; }

        public string Payload { get; set; }
    }
}
