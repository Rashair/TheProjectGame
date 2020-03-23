using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace GameMaster.Managers
{
    public abstract class SocketManager<T, M>
    {
        private ConcurrentDictionary<string, T> _sockets = new ConcurrentDictionary<string, T>();

        protected abstract bool IsSame(T a, T b);

        protected abstract Task CloseSocketAsync(T socket);

        protected abstract Task SendMessageAsync(T socket, M message);

        public string GetId(T socket)
        {
            return _sockets.FirstOrDefault(p => IsSame(p.Value, socket)).Key;
        }

        public T GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public bool AddSocket(T socket)
        {
            return _sockets.TryAdd(CreateSocketId(), socket);
        }

        public async Task<bool> RemoveSocketAsync(string id)
        {
            bool removed = _sockets.TryRemove(id, out T socket);
            if (removed)
            {
                await CloseSocketAsync(socket);
            }
            return removed;
        }

        public async Task SendMessageAsync(string id, M message)
        {
            await SendMessageAsync(GetSocketById(id), message);
        }

        public async Task SendMessageToAllAsync(M message)
        {
            await Task.WhenAll(from p in _sockets select SendMessageAsync(p.Value, message));
        }

        private string CreateSocketId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
