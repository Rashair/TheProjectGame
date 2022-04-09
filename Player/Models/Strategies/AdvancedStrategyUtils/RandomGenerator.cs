using System;

namespace Player.Models.Strategies.AdvancedStrategyUtils;

public class RandomGenerator
{
    private readonly Random random;

    public RandomGenerator()
    {
        this.random = new Random();
    }

    public bool IsLucky(int percentage)
    {
        return random.Next(100) < percentage;
    }

    public int this[int i] => random.Next(i);
}
