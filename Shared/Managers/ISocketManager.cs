using System.Threading;
using System.Threading.Tasks;

namespace Shared.Managers
{
    public interface ISocketManager<TSocket, TMessage>
    {
        int GetId(TSocket socket);

        TSocket GetSocketById(int id);

        bool AddSocket(TSocket socket);

        Task<bool> RemoveSocketAsync(int id, CancellationToken cancellationToken);

        Task SendMessageAsync(int id, TMessage message, CancellationToken cancellationToken);

        Task SendMessageToAllAsync(TMessage message, CancellationToken cancellationToken);

        bool IsAnyOpen();
    }
}
