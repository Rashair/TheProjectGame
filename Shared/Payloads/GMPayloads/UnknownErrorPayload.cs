using Shared.Models;

namespace Shared.Payloads
{
    public class UnknownErrorPayload : Payload
    {
        public Position Position { get; set; }

        public bool HoldingPiece { get; set; }
    }
}
