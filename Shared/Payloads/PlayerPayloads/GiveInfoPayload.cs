using System.Text;

using Shared.Enums;

namespace Shared.Payloads.PlayerPayloads
{
    public class GiveInfoPayload : Payload
    {
        public int[] Distances { get; set; }

        public int RespondToID { get; set; }

        public GoalInfo[] RedTeamGoalAreaInformations { get; set; }

        public GoalInfo[] BlueTeamGoalAreaInformations { get; set; }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder($" RespondToId:{RespondToID})");
            message.AppendLine();

            message.Append("Distances:\n");
            for (int i = 0; i < Distances.Length; ++i)
            {
                message.Append($"{Distances[i]}, ");
            }
            message.AppendLine();

            message.Append("RedTeamGoalAreaInformations:\n");
            for (int i = 0; i < RedTeamGoalAreaInformations.Length; ++i)
            {
                message.Append($"{RedTeamGoalAreaInformations[i]}, ");
            }
            message.AppendLine();

            message.Append("BlueTeamGoalAreaInformations:\n");
            for (int i = 0; i < BlueTeamGoalAreaInformations.Length; ++i)
            {
                message.Append($"{BlueTeamGoalAreaInformations[i]}, ");
            }

            return message.ToString();
        }
    }
}
