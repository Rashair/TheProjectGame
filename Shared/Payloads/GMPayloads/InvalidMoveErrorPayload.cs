using Shared.Models;

namespace Shared.Payloads.GMPayloads;

public class InvalidMoveErrorPayload : Payload
{
    public Position Position { get; set; }

    public override string ToString()
    {
        return $"X:{Position.X}, Y:{Position.Y}";
    }
}
