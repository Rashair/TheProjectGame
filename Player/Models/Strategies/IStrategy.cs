using System.Threading;
using System.Threading.Tasks;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public interface IStrategy
    {
        Task MakeDecision(Player player, CancellationToken cancellationToken);
    }
}
