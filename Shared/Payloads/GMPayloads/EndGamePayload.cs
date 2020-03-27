using Shared.Enums;

namespace Shared.Payloads
{
    public class EndGamePayload : Payload
    {
        public Team Winner { get; set; }
    }
}
