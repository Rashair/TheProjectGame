using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Player.Models.Strategies.Utils
{
    public enum LastAction
    {
        None,
        Move,
        Discover,
        Check,
        Pick,
        Put,
        Destroy,
    }
}
