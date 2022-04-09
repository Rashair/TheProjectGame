using System.Text;

using Shared.Models;

namespace Shared.Payloads.GMPayloads;

public class UnknownErrorPayload : Payload
{
    public Position? Position { get; set; }

    public bool? HoldingPiece { get; set; }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        if (HoldingPiece != null)
        {
            stringBuilder.Append($"HoldingPiece:{HoldingPiece}, ");
        }
        if (Position != null)
        {
            stringBuilder.Append($"Position: ({Position.Value.Y}, {Position.Value.X})");
        }

        return stringBuilder.ToString();
    }
}
