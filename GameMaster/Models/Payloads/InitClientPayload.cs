using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameMaster.Models.Payloads
{
    public class InitClientPayload : ClientPayload
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int FirstGoalLevel { get; set; }

        public int SecondGoalLevel { get; set; }

        public List<int[]> Goals { get; set; }
    }
}
