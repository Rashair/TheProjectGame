using Shared.Models;

namespace Shared.Payloads.GMPayloads
{
    public class UnknownErrorPayload : Payload
    {
        public Position Position { get; set; }

        public bool HoldingPiece { get; set; }

        public override string ToString()
        {
            return $"HoldingPiece:{HoldingPiece}, Position:({Position.Y}, {Position.X})";
        }
    }
}
