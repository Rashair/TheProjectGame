using Shared.Enums;

namespace Shared.Payloads.GMPayloads;

public class PutErrorPayload : Payload
{
    public PutError ErrorSubtype { get; set; }

    public override string ToString()
    {
        return $"ErrorSubtype:{ErrorSubtype}";
    }
}
