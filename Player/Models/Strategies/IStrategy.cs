using System.Threading;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public interface IStrategy
    {
        void MakeDecision(Player player, Team team, int goalAreaSize, CancellationToken cancellationToken);
    }
}
