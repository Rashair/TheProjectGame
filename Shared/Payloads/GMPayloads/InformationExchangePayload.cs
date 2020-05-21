namespace Shared.Payloads.GMPayloads
{
    public class InformationExchangePayload : Payload
    {
        public bool Succeeded { get; set; }

        public override string ToString()
        {
            return $"Was the message send: {Succeeded}";
        }
    }
}
