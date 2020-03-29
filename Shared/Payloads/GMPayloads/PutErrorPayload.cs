using Shared.Enums;

namespace Shared.Payloads
{
    public class PutErrorPayload : Payload
    {
        public PutError ErrorSubtype { get; set; }
    }
}
