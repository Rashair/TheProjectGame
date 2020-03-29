using System.Threading;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class DummyStrategy : IStrategy
    {
        public void MakeDecision(Player player, Team team, int g, CancellationToken cancellationToken)
        {
        }
    }
}
