namespace Shared.Payloads.PlayerPayloads
{
    public class BegForInfoPayload : Payload
    {
        public int AskedPlayerId { get; set; }

        public override string ToString()
        {
            return $"AskedPlayerId:{AskedPlayerId}";
        }
    }
}
