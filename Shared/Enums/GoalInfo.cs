﻿using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum GoalInfo
{
    IDK,
    [EnumMember(Value = "N")]
    DiscoveredNotGoal,
    [EnumMember(Value = "G")]
    DiscoveredGoal,
}
