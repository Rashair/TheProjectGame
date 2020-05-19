using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Shared.Payloads
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class Payload
    {
        public Payload Serialize()
        {
            return this;
        }
    }
}
