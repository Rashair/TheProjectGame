namespace Shared.Payloads
{
    public class JoinAnswerPayload : Payload
    {
        public bool Accepted { get; set; }

        public int PlayerId { get; set; }
    }
}
