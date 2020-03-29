using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Shared.Enums;

namespace Player.Models
{
    public class PlayerConfiguration
    {
        public string CsIP { get; set; }

        public int CsPort { get; set; }

        public string TeamID { get; set; }

        public int Strategy { get; set; }
    }
}
