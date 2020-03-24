using Shared.Enums;

namespace Shared.Models.Messages
{
    public class PlayerMessage
    {
        public PlayerMessageID MessageID { get; set; }

        public int PlayerID { get; set; }

        public string Payload { get; set; }
    }
}
