namespace Shared.Models.Payloads
{
    public class JoinAnswerPayload : Payload
    {
        public bool Accepted { get; set; }

        public int AgentID { get; set; }
    }
}
