using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Shared.Messages
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class Message
    {
        public int AgentID { get; set; }

        public string Payload { get; set; }

        public T DeserializePayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(Payload);
        }
    }
}
