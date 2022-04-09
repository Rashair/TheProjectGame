using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shared.Converters;
using Shared.Enums;
using Shared.Payloads;

namespace Shared.Messages;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
[JsonConverter(typeof(MessageConverter))]
public class Message
{
    public MessageID MessageID { get; set; }

    public int? AgentID { get; set; }

    public Payload Payload { get; set; }

    public Message()
    {
    }

    public Message(MessageID messageID, int agentID, Payload payload = null)
    {
        this.MessageID = messageID;
        this.AgentID = agentID;
        this.Payload = payload;
    }

    public Message(MessageID messageID, int agentID, string payload)
        : this(messageID, agentID)
    {
        var type = messageID.GetPayloadType();
        this.Payload = (Payload)JsonConvert.DeserializeObject(payload, type);
    }

    public bool IsMessageToGM()
    {
        return (int)MessageID < (int)Enums.MessageID.CheckAnswer ||
            (int)MessageID == (int)Enums.MessageID.PlayerDisconnected;
    }
}
