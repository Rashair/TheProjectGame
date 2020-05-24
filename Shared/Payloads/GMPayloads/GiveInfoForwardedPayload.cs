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
            int height = Distances.Length / boardWidth;
            for (int i = 0; i < height; i += boardWidth)
            {
                for (int j = 0; j < boardWidth; j++)
                {
                    message.Append($"{Distances[i + j]}, ");
                }
                message.AppendLine();
            }
            message.AppendLine();

            message.Append("RedTeamGoalAreaInformation:\n");
            height = RedTeamGoalAreaInformations.Length / boardWidth;
            for (int i = 0; i < height; i += boardWidth)
            {
                for (int j = 0; j < boardWidth; ++j)
                {
                    message.Append($"{RedTeamGoalAreaInformations[i + j]}, ");
                }
                message.AppendLine();
            }
            message.AppendLine();

            message.Append("BlueTeamGoalAreaInformation:\n");
            height = BlueTeamGoalAreaInformations.Length / boardWidth;
            for (int i = 0; i < height; i += boardWidth)
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
