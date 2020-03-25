using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Managers
{
    public interface ISocketManager<TSocket, TMessage>
    {
        string GetId(TSocket socket);

        TSocket GetSocketById(string id);

        bool AddSocket(TSocket socket);

        Task<bool> RemoveSocketAsync(string id, CancellationToken cancellationToken);

        Task SendMessageAsync(string id, TMessage message, CancellationToken cancellationToken);

        Task SendMessageToAllAsync(TMessage message, CancellationToken cancellationToken);
    }
}
