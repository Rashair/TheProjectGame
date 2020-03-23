namespace Shared.Models.Payloads
{
    public class GiveInfoPayload
    {
        public int respondToID;
        public int[,] distances;
        public GoalInfo[,] redTeamGoalAreaInformations;
        public GoalInfo[,] blueTeamGoalAreaInformations;
    }
}
