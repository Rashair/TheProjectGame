using Shared.Enums;

namespace Shared.Payloads
{
    public class JoinGamePayload : Payload
    {
        public Team TeamID { get; set; }
    }
}
