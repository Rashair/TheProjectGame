using System;

namespace Shared.Messages
{
    public class Message
    {
        public Enum MessageID { get; set; }

        public int AgentID { get; set; }

        public string Payload { get; set; }
    }
}
