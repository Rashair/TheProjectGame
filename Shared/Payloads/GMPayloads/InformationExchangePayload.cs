namespace Shared.Payloads.GMPayloads
{
    public class InformationExchangePayload : Payload
    {
        public bool WasSent { get; set; }

        public override string ToString()
        {
            return $"Was the message send: {WasSent}";
        }
    }
}
