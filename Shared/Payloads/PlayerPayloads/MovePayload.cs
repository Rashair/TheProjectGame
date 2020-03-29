using Shared.Enums;

namespace Shared.Payloads
{
    public class MovePayload : Payload
    {
        public Direction Direction { get; set; }
    }
}
