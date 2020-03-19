using System;
using System.Threading.Tasks;

namespace Player.Clients
{
    public interface ISocketClient<R, S>
    {
        bool IsOpen { get; }

        Task ConnectAsync(Uri uri);
        Task CloseAsync();
        Task<(bool, R)> ReceiveAsync();
        Task SendAsync(S message);
    }
}
