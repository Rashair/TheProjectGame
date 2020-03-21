using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Player.Models.Payloads
{
    public class GiveInfoPayload : Payload
    {
        public int respondToID;
        public int[,] distances;
        public GoalInfo[,] redTeamGoalAreaInformations;
        public GoalInfo[,] blueTeamGoalAreaInformations;
    }
}
