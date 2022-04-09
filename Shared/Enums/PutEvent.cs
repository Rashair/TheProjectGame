using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum PutEvent
{
    Unknown,
    NormalOnGoalField,
    NormalOnNonGoalField,
    TaskField,
    ShamOnGoalArea,
}
