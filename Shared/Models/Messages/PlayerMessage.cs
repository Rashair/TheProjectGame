namespace Shared.Models.Messages
{
    public class PlayerMessage
    {
        public int MessageID { get; set; }

        public int AgentID { get; set; }

        public string Payload { get; set; }
    }
}
