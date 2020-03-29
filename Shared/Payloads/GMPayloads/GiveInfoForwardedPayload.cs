using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads
{
    public class GiveInfoForwardedPayload : Payload
    {
        public int AnsweringID { get; set; }

        public int[,] Distances { get; set; }

        [JsonProperty("redTeamGoalAreaInformations", ItemConverterType = typeof(StringEnumConverter))]
        public GoalInfo[,] RedTeamGoalAreaInformations { get; set; }

        [JsonProperty("blueTeamGoalAreaInformations", ItemConverterType = typeof(StringEnumConverter))]
        public GoalInfo[,] BlueTeamGoalAreaInformations { get; set; }
    } // added for compatibility
}
