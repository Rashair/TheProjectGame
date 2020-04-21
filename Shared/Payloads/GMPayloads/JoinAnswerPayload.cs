namespace Shared.Payloads.GMPayloads
{
    public class JoinAnswerPayload : Payload
    {
        public bool Accepted { get; set; }

        public int PlayerId { get; set; }
    }
}
