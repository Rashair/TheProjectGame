using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class GiveInfoForwardedPayload : Payload
    {
        public int AnsweringId { get; set; }

        public int[,] Distances { get; set; }

        [JsonProperty("redTeamGoalAreaInformations", ItemConverterType = typeof(StringEnumConverter))]
        public GoalInfo[,] RedTeamGoalAreaInformations { get; set; }

        [JsonProperty("blueTeamGoalAreaInformations", ItemConverterType = typeof(StringEnumConverter))]
        public GoalInfo[,] BlueTeamGoalAreaInformations { get; set; }

        public override string ToString()
        {
            string message = $" AnsweringId:{AnsweringId}\n";
            message += "Distances:\n";
            for (int i = 0; i < Distances.GetLength(0); i++)
            {
                for (int j = 0; j < Distances.GetLength(1); j++)
                {
                    message += $"{Distances[i, j]}, ";
                }
                message += "\n";
            }
            message += "RedTeamGoalAreaInformations:\n";
            for (int i = 0; i < RedTeamGoalAreaInformations.GetLength(0); i++)
            {
                for (int j = 0; j < RedTeamGoalAreaInformations.GetLength(1); j++)
                {
                    message += $"{RedTeamGoalAreaInformations[i, j]}, ";
                }
                message += "\n";
            }
            message += "BlueTeamGoalAreaInformations:\n";
            for (int i = 0; i < BlueTeamGoalAreaInformations.GetLength(0); i++)
            {
                for (int j = 0; j < BlueTeamGoalAreaInformations.GetLength(1); j++)
                {
                    message += $"{BlueTeamGoalAreaInformations[i, j]}, ";
                }
                message += "\n";
            }
            return message;
        }
    } 
}
