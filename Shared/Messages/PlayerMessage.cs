using Newtonsoft.Json;
using Shared.Enums;
using Shared.Payloads.PlayerPayloads;

namespace Shared.Messages
{
    public class PlayerMessage : Message
    {
        public PlayerMessageId MessageId { get; set; }

        public int PlayerId { get; set; }

        public string Payload { get; set; }

        public override string Get()
        {
            string message = $"MessageId:{MessageId}, PlayerId:{PlayerId}";
            if (Payload == null)
            {
                message += " Payload:null\n";
                return message;
            }
            message += ", Payload:{";
            message += Payload.ToString();
            message += "}";
            return message;
        }
    }
}
