namespace Shared.Models.Payloads
{
    public class GiveInfoForwardedPayload
    {
        public int answeringID;
        public int[,] distances;
        public GoalInfo[,] redTeamGoalAreaInformations;
        public GoalInfo[,] blueTeamGoalAreaInformations;
    } //added for compatibility
}
