using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads
{
    public class GiveInfoPayload : Payload
    {
        public int[,] Distances { get; set; }

        public int RespondToID { get; set; }

        [JsonProperty("redTeamGoalAreaInformations")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GoalInfo[,] RedTeamGoalAreaInformations { get; set; }

        [JsonProperty("blueTeamGoalAreaInformations")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GoalInfo[,] BlueTeamGoalAreaInformations { get; set; }
    }
}
