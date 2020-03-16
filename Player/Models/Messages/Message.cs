using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Player.Models.Messages
{
    public class Message
    {
        public int messageID;
    }

    public class JoinGameRequestMessage: Message
    {
        public string teamID;
    }

    public class RegularMessage: Message
    {
        public int agentID;
        public int payload;
    }

    public class MoveMessage: RegularMessage
    {
        public string direction;
    }

    public class BegForInfoMessage: RegularMessage
    {
        public int askedAgentID;
    }

    public class GiveInfoMessage: RegularMessage
    {
        public int respondToID;
        public int[,] distances;
        public GoalAreaInformations[,] redTeamGoalAreaInformations;
        public GoalAreaInformations[,] blueTeamGoalAreaInformations;
    }
}
