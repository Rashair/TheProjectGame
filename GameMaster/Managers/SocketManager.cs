using Newtonsoft.Json;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Managers 
{
    public abstract class SocketManager<T, M>
    {
        private ConcurrentDictionary<string, T> _sockets = new ConcurrentDictionary<string, T>();
        public abstract bool IsSame(T a, T b);
        public abstract Task CloseSocketAsync(T socket);
        public abstract Task SendMessageAsync(T socket, M message);
        public string GetId(T socket) => _sockets.FirstOrDefault(p => IsSame(p.Value, socket)).Key;
        public T GetSocketById(string id) => _sockets.FirstOrDefault(p => p.Key == id).Value;
        public ConcurrentDictionary<string, T> GetAll() => _sockets;
        public bool AddSocket(T socket) => _sockets.TryAdd(CreateSocketId(), socket);
        public async Task<bool> RemoveSocket(string id)
        {
            bool removed = _sockets.TryRemove(id, out T socket);
            if (removed)
            {
                await CloseSocketAsync(socket);
            }
            return removed;
        }
        public async Task SendMessageAsync(string id, M message) => await SendMessageAsync(GetSocketById(id), message);
        public async Task SendMessageToAllAsync(M message) 
            => await Task.WhenAll(from p in _sockets select SendMessageAsync(p.Value, message));
        private string CreateSocketId() => Guid.NewGuid().ToString();
    }
}
