using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Payloads
{
    public class GiveInfoPayload : Payload
    {
        public int respondToID;
        public int[,] distances;
        public GoalInfo[,] redTeamGoalAreaInformations;
        public GoalInfo[,] blueTeamGoalAreaInformations;
    }
}
