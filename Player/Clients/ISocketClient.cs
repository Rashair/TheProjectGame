using System;
using System.Threading;
using System.Threading.Tasks;

namespace Player.Clients
{
    public interface ISocketClient<R, S>
    {
        bool IsOpen { get; }
        Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

        Task CloseAsync(CancellationToken cancellationToken);

        /// <returns>
        /// (bool notClosed, R value)
        /// If notClosed is false then the socket is closed and value is set to default(R) otherwise,
        /// the value is obtained from deserialization of the socket message.
        /// </returns>
        Task<(bool, R)> ReceiveAsync(CancellationToken cancellationToken);

        Task SendAsync(S message, CancellationToken cancellationToken);
    }
}
