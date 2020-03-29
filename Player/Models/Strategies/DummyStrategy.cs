using System.Threading;
using System.Threading.Tasks;

using Shared.Enums;

namespace Player.Models.Strategies
{
    public class DummyStrategy : IStrategy
    {
        public async Task MakeDecision(Player player, CancellationToken cancellationToken)
        {
        }
    }
}
