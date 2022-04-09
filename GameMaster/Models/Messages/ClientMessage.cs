using GameMaster.Models.Payloads;

namespace GameMaster.Models.Messages;

public class ClientMessage
{
    public string Type { get; set; }

    public string Info { get; set; }

    public ClientPayload Payload { get; set; }

    public ClientMessage()
    {
    }

    public ClientMessage(string type, string info = default, ClientPayload payload = null)
    {
        this.Type = type;
        this.Info = info;
        this.Payload = payload;
    }
}
