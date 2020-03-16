using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Player.Models.Payloads
{   
    public class EmptyPayload
    {     
    }

    public class JoinGamePayload
    {
        public string teamID;
    }

    public class MovePayload
    {
        public string direction;
    }

    public class BegForInfoPayload
    {
        public int askedAgentID;
    }

    public class GiveInfoPayload
    {
        public int respondToID;
        public int[,] distances;
        public GoalInfo[,] redTeamGoalAreaInformations;
        public GoalInfo[,] blueTeamGoalAreaInformations;
    }
}
