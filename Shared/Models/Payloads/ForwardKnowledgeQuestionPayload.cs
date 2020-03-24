namespace Shared.Models.Payloads
{
    public class ForwardKnowledgeQuestionPayload : Payload
    {
        public int AskingID { get; set; }

        public bool Leader { get; set; }

        public Team TeamId { get; set; }
    }
}
