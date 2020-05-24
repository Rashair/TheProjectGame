using System.Text;

using Shared.Enums;

namespace Shared.Payloads.GMPayloads
{
    public class GiveInfoForwardedPayload : Payload
    {
        private readonly int boardWidth;

        public GiveInfoForwardedPayload(int width)
        {
            this.boardWidth = width;
        }

        public int RespondingID { get; set; }

        public int[] Distances { get; set; }

        public GoalInfo[] RedTeamGoalAreaInformations { get; set; }

        public GoalInfo[] BlueTeamGoalAreaInformations { get; set; }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder($"respondingID:{RespondingID}, ");
            message.AppendLine("Distances:\n");
            for (int i = 0; i < Distances.Length; i += boardWidth)
            {
                for (int j = 0; j < boardWidth; j++)
                {
                    message.Append($"{Distances[i + j]}, ");
                }
                message.AppendLine();
            }
            message.AppendLine();

            message.Append("RedTeamGoalAreaInformation:\n");
            for (int i = 0; i < RedTeamGoalAreaInformations.Length; i += boardWidth)
            {
                for (int j = 0; j < boardWidth; ++j)
                {
                    message.Append($"{RedTeamGoalAreaInformations[i + j]}, ");
                }
                message.AppendLine();
            }
            message.AppendLine();

            message.Append("BlueTeamGoalAreaInformation:\n");
            for (int i = 0; i < BlueTeamGoalAreaInformations.Length; i += boardWidth)
            {
                for (int j = 0; j < boardWidth; j++)
                {
                    message.Append($"{BlueTeamGoalAreaInformations[i + j]}, ");
                }
                message.AppendLine();
            }

            return message.ToString();
        }
    }
}
