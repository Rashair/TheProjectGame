using System.Text;

using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class GiveInfoForwardedPayload : Payload
    {
        public int RespondingID { get; set; }

        public int[] Distances { get; set; }

        public GoalInfo[] RedTeamGoalAreaInformations { get; set; }

        public GoalInfo[] BlueTeamGoalAreaInformations { get; set; }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder($"respondingID:{RespondingID}, ");
            message.AppendLine("Distances:\n");
            for (int i = 0; i < Distances.Length; ++i)
            {
                message.Append($"{Distances[i]}, ");
            }
            message.AppendLine();

            message.Append("RedTeamGoalAreaInformation:\n");
            for (int i = 0; i < RedTeamGoalAreaInformations.Length; ++i)
            {
                message.Append($"{RedTeamGoalAreaInformations[i]}, ");
            }
            message.AppendLine();

            message.Append("BlueTeamGoalAreaInformation:\n");
            for (int i = 0; i < BlueTeamGoalAreaInformations.Length; ++i)
            {
                message.Append($"{BlueTeamGoalAreaInformations[i]}, ");
            }

            return message.ToString();
        }
    }
}
