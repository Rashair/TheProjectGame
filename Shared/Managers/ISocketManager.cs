using System.Threading;
using System.Threading.Tasks;

namespace Shared.Managers
{
    public interface ISocketManager<TSocket, TMessage>
    {
        int GetId(TSocket socket);

        TSocket GetSocketById(int id);

        /// <summary>On succes returns id (id > 0) of the socket, on failure -1.</summary>
        int AddSocket(TSocket socket);

        Task<bool> RemoveSocketAsync(int id, CancellationToken cancellationToken);

        Task SendMessageAsync(int id, TMessage message, CancellationToken cancellationToken);

        Task SendMessageToAllAsync(TMessage message, CancellationToken cancellationToken);

        bool IsAnyOpen();
    }
}
