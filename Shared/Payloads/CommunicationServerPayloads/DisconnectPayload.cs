using Shared.Enums;

namespace Shared.Payloads.CommunicationServerPayloads;

public class DisconnectPayload : Payload
{
    public int AgentID { get; set; }

    public override string ToString()
    {
        return $"AgentID:{AgentID}";
    }
}
