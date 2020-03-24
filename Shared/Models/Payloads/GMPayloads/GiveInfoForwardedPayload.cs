namespace Shared.Models.Payloads
{
    public class GiveInfoForwardedPayload : Payload
    {
        public int AnsweringID { get; set; }

        public int[,] Distances { get; set; }

        public GoalInfo[,] RedTeamGoalAreaInformations { get; set; }

        public GoalInfo[,] BlueTeamGoalAreaInformations { get; set; }
    } // added for compatibility
}
