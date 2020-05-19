using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shared.Payloads;

namespace Shared.Messages
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class Message
    {
        private int? agentID;

        public int AgentID
        {
            get => agentID.Value;
            set
            {
                this.agentID = value;
            }
        }

        public Payload Payload { get; set; }

        public T DeserializePayload<T>()
            where T : Payload
        {
            return (T)Payload;
        }
    }
}
