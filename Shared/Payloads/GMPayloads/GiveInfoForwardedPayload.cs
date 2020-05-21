using System.Text;

using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class GiveInfoForwardedPayload : Payload
    {
        public int RespondingID { get; set; }

        public int[,] Distances { get; set; }

        public GoalInfo[,] RedTeamGoalAreaInformations { get; set; }

        public GoalInfo[,] BlueTeamGoalAreaInformations { get; set; }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder($"respondingID:{RespondingID}, ");
            message.AppendLine("Distances:\n");
            for (int i = 0; i < Distances.GetLength(0); i++)
            {
                for (int j = 0; j < Distances.GetLength(1); j++)
                {
                    message.Append($"{Distances[i, j]}, ");
                }
                message.AppendLine();
            }
            message.AppendLine();
            message.Append("RedTeamGoalAreaInformation:\n");
            for (int i = 0; i < RedTeamGoalAreaInformations.GetLength(0); i++)
            {
                for (int j = 0; j < RedTeamGoalAreaInformations.GetLength(1); j++)
                {
                    message.Append($"{RedTeamGoalAreaInformations[i, j]}, ");
                }
                message.AppendLine();
            }
            message.AppendLine();
            message.Append("BlueTeamGoalAreaInformation:\n");
            for (int i = 0; i < BlueTeamGoalAreaInformations.GetLength(0); i++)
            {
                for (int j = 0; j < BlueTeamGoalAreaInformations.GetLength(1); j++)
                {
                    message.Append($"{BlueTeamGoalAreaInformations[i, j]}, ");
                }
                message.AppendLine();
            }

            return message.ToString();
        }
    }
}
