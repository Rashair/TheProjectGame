using Serilog;

namespace Player.Models.Strategies.Utils;

public static class StrategyFactory
{
    public static IStrategy Create(StrategyEnum strategy, Player player, ILogger log)
    {
        switch (strategy)
        {
            case StrategyEnum.SimpleStrategy:
                return new SimpleStrategy(player, log);
            case StrategyEnum.AdvancedStrategy:
                return new AdvancedStrategy(player, log);
            default:
                return null;
        }
    }
}
