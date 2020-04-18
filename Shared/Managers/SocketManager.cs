using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Managers
{
    public abstract class SocketManager<TSocket, TMessage> : ISocketManager<TSocket, TMessage>
    {
        private readonly ConcurrentDictionary<int, TSocket> sockets = new ConcurrentDictionary<int, TSocket>();
        private int guid = 1;

        protected abstract bool IsOpen(TSocket socket);

        protected abstract bool IsSame(TSocket a, TSocket b);

        protected abstract Task CloseSocketAsync(TSocket socket, CancellationToken cancellationToken);

        protected abstract Task SendMessageAsync(TSocket socket, TMessage message, CancellationToken cancellationToken);

        public int GetId(TSocket socket)
        {
            return sockets.FirstOrDefault(p => IsSame(p.Value, socket)).Key;
        }

        public TSocket GetSocketById(int id)
        {
            return sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public bool AddSocket(TSocket socket)
        {
            return sockets.TryAdd(CreateSocketId(), socket);
        }

        public async Task<bool> RemoveSocketAsync(int id, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            bool removed = sockets.TryRemove(id, out TSocket socket);
            if (removed)
                await CloseSocketAsync(socket, cancellationToken);
            return removed;
        }

        public async Task SendMessageAsync(int id, TMessage message, CancellationToken cancellationToken)
        {
            await SendMessageAsync(GetSocketById(id), message, cancellationToken);
        }

        public async Task SendMessageToAllAsync(TMessage message, CancellationToken cancellationToken)
        {
            await Task.WhenAll(from p in sockets select SendMessageAsync(p.Value, message, cancellationToken));
        }

        public bool IsAnyOpen()
        {
           return sockets.Any(s => IsOpen(s.Value));
        }

        private int CreateSocketId()
        {
            return Interlocked.Increment(ref guid);
        }
    }
}
