using System.Threading;
using System.Threading.Tasks;

namespace Player.Models.Strategies
{
    public interface IStrategy
    {
        Task MakeDecision(CancellationToken cancellationToken);
    }
}
