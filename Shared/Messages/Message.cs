using System;

namespace Shared.Messages
{
    public abstract class Message
    {
        public int AgentID { get; set; }

        public string Payload { get; set; }
    }
}
