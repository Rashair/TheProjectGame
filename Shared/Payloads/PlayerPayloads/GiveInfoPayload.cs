using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads
{
    public class GiveInfoPayload : Payload
    {
        public int[,] Distances { get; set; }

        public int RespondToId { get; set; }

        [JsonProperty("redTeamGoalAreaInformations", ItemConverterType = typeof(StringEnumConverter))]
        public GoalInfo[,] RedTeamGoalAreaInformations { get; set; }

        [JsonProperty("blueTeamGoalAreaInformations", ItemConverterType = typeof(StringEnumConverter))]
        public GoalInfo[,] BlueTeamGoalAreaInformations { get; set; }
    }
}
