using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Clients
{
    public interface ISocketClient<R, S>
    {
        bool IsOpen { get; }

        object GetSocket();

        Task ConnectAsync(string host, int port, CancellationToken cancellationToken);

        Task CloseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// (bool wasReceived, R value)
        /// If wasReceived is false then critical error was encountered and message is set to default(R),
        /// otherwise the message is obtained from deserialization of the socket message.
        /// </summary>
        Task<(bool wasReceived, R message)> ReceiveAsync(CancellationToken cancellationToken);

        Task SendAsync(S message, CancellationToken cancellationToken);

        Task SendToAllAsync(List<S> messages, CancellationToken cancellationToken);
    }
}
