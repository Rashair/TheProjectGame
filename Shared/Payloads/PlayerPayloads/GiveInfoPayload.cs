using System.Text;

using Shared.Enums;

namespace Shared.Payloads.PlayerPayloads
{
    public class GiveInfoPayload : Payload
    {
        public int[,] Distances { get; set; }

        public int RespondToID { get; set; }

        public GoalInfo[,] RedTeamGoalAreaInformations { get; set; }

        public GoalInfo[,] BlueTeamGoalAreaInformations { get; set; }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder($" RespondToId:{RespondToID})");
            message.AppendLine();
            message.Append("Distances:\n");
            for (int i = 0; i < Distances.GetLength(0); i++)
            {
                for (int j = 0; j < Distances.GetLength(1); j++)
                {
                    message.Append($"{Distances[i, j]}, ");
                }
                message.AppendLine();
            }
            message.AppendLine();
            message.Append("RedTeamGoalAreaInformations:\n");
            for (int i = 0; i < RedTeamGoalAreaInformations.GetLength(0); i++)
            {
                for (int j = 0; j < RedTeamGoalAreaInformations.GetLength(1); j++)
                {
                    message.Append($"{RedTeamGoalAreaInformations[i, j]}, ");
                }
                message.AppendLine();
            }
            message.AppendLine();
            message.Append("BlueTeamGoalAreaInformations:\n");
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
