using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Managers
{
    public abstract class SocketManager<TSocket, TMessage> : ISocketManager<TSocket, TMessage>
    {
        private readonly ConcurrentDictionary<string, TSocket> sockets = new ConcurrentDictionary<string, TSocket>();

        protected abstract bool IsSame(TSocket a, TSocket b);

        protected abstract Task CloseSocketAsync(TSocket socket, CancellationToken cancellationToken);

        protected abstract Task SendMessageAsync(TSocket socket, TMessage message, CancellationToken cancellationToken);

        public string GetId(TSocket socket)
        {
            return sockets.FirstOrDefault(p => IsSame(p.Value, socket)).Key;
        }

        public TSocket GetSocketById(string id)
        {
            return sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public bool AddSocket(TSocket socket)
        {
            return sockets.TryAdd(CreateSocketId(), socket);
        }

        public async Task<bool> RemoveSocketAsync(string id, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;
            bool removed = sockets.TryRemove(id, out TSocket socket);
            if (removed)
                await CloseSocketAsync(socket, cancellationToken);
            return removed;
        }

        public async Task SendMessageAsync(string id, TMessage message, CancellationToken cancellationToken)
        {
            await SendMessageAsync(GetSocketById(id), message, cancellationToken);
        }

        public async Task SendMessageToAllAsync(TMessage message, CancellationToken cancellationToken)
        {
            await Task.WhenAll(from p in sockets select SendMessageAsync(p.Value, message, cancellationToken));
        }

        private string CreateSocketId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
