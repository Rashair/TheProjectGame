using Shared.Enums;

namespace Shared.Payloads.PlayerPayloads
{
    public class JoinGamePayload : Payload
    {
        public Team TeamId { get; set; }

        public override string ToString()
        {
            return $"TeamId:{TeamId}";
        }
    }
}
