namespace Player.Models.Strategies.Utils
{
    public static class StrategyFactory
    {
        public static IStrategy Create(StrategyEnum strategy, Player player)
        {
            switch (strategy)
            {
                case StrategyEnum.SimpleStrategy:
                    return new SimpleStrategy(player);
                case StrategyEnum.AdvancedStrategy:
                    return new AdvancedStrategy(player);
                default:
                    return null;
            }
        }
    }
}
