using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shared.Payloads;

namespace Shared.Messages
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class Message
    {
        public int AgentID { get; set; }

        public Payload Payload { get; set; }

        public T DeserializePayload<T>()
            where T : Payload
        {
            return (T)Payload;
        }
    }
}
