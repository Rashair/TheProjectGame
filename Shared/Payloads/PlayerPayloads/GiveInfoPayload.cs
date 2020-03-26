using Shared.Enums;

namespace Shared.Payloads
{
    public class GiveInfoPayload : Payload
    {
        public int[,] Distances { get; set; }

        public int RespondToID { get; set; }

        public GoalInfo[,] RedTeamGoalAreaInformations { get; set; }

        public GoalInfo[,] BlueTeamGoalAreaInformations { get; set; }
    }
}
