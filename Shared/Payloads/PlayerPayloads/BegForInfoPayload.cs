namespace Shared.Payloads.PlayerPayloads;

public class BegForInfoPayload : Payload
{
    public int AskedAgentID { get; set; }

    public override string ToString()
    {
        return $"AskedAgentID:{AskedAgentID}";
    }
}
