using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PutError
    {
        Other,
        AgentNotHolding,
        CannotPutThere,
    }
}
