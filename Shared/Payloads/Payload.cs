using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Shared.Payloads
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public abstract class Payload
    {
        public static implicit operator string(Payload p) => p.Serialize();

        public override string ToString()
        {
            return Serialize();
        }

        private string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
