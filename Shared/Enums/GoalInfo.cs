using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum GoalInfo
    {
        IDK,
        DiscoveredNotGoal,
        DiscoveredGoal,
    }
}
