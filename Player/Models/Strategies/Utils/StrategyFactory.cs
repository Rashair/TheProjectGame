using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Player.Models.Strategies.Utils
{
    public static class StrategyFactory
    {
        public static IStrategy Create(StrategyEnum strategy)
        {
            switch (strategy)
            {
                case StrategyEnum.SimpleStrategy:
                    return new SimpleStrategy();
                default:
                    return new DummyStrategy();
            }
        }
    }
}
