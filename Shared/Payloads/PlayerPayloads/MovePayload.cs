using Shared.Enums;

namespace Shared.Payloads.PlayerPayloads
{
    public class MovePayload : Payload
    {
        public Direction Direction { get; set; }

        public override string ToString()
        {
            return $"Direction:{Direction}";
        }
    }
}
