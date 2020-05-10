namespace Shared.Payloads.GMPayloads
{
    public class CheckAnswerPayload : Payload
    {
        public bool Sham { get; set; }

        public override string ToString()
        {
            return $"Sham:{Sham}";
        }
    }
}
