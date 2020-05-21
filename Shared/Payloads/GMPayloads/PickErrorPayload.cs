using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class PickErrorPayload : Payload
    {
        public PickError ErrorSubtype { get; set; }

        public override string ToString()
        {
            return $"ErrorSubtype:{ErrorSubtype}";
        }
    }
}
