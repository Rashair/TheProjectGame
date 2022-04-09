using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Shared.Payloads;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class Payload
{
    public override bool Equals(object obj)
    {
        return this.GetType() == obj.GetType() && this.AreAllPropertiesTheSame(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
