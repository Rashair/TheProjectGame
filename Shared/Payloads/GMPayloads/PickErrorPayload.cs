using Shared.Enums;

namespace Shared.Payloads
{
    public class PickErrorPayload : Payload
    {
        public PickError ErrorSubtype { get; set; }
    }
}
