using Newtonsoft.Json;
using Shared.Enums;
using Shared.Payloads.PlayerPayloads;

namespace Shared.Messages
{
    public class PlayerMessage : Message
    {
        public new PlayerMessageId MessageID { get; set; }
    }
}
