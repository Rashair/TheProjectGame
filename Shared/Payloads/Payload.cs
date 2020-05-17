using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Shared.Payloads
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class Payload
    {
        public static implicit operator string(Payload p) => p;

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
