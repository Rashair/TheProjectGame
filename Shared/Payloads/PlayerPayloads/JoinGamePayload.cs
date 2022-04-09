using Shared.Enums;

namespace Shared.Payloads.PlayerPayloads;

public class JoinGamePayload : Payload
{
    public Team TeamID { get; set; }

    public override string ToString()
    {
        return $"TeamID:{TeamID}";
    }
}
