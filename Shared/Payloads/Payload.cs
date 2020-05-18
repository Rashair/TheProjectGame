using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Shared.Payloads
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class Payload
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
