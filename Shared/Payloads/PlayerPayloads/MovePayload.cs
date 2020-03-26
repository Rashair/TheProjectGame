using Shared.Enums;

namespace Shared.Payloads
{
    public class MovePayload : Payload
    {
        public Directions Direction { get; set; }
    }
}
