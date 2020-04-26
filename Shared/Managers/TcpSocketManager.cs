using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using Shared.Clients;

namespace Shared.Managers
{
    public class TcpSocketManager<R, S> : SocketManager<ISocketClient<R, S>, S>
    {
        private readonly ILogger logger;

        public TcpSocketManager(ILogger log)
        {
            this.logger = log.ForContext<TcpSocketManager<R, S>>();
        }

        protected override bool IsOpen(ISocketClient<R, S> socket)
        {
            return socket.IsOpen;
        }

        protected override bool IsSame(ISocketClient<R, S> a, ISocketClient<R, S> b)
        {
            return ((TcpClient)a.GetSocket()) == ((TcpClient)b.GetSocket());
        }

        protected override async Task CloseSocketAsync(ISocketClient<R, S> socket,
            CancellationToken cancellationToken)
        {
            await socket.CloseAsync(cancellationToken);
        }

        protected override async Task SendMessageAsync(ISocketClient<R, S> socket, S message,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && socket.IsOpen)
            {
                try
                {
                    await socket.SendAsync(message, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to send message: {e}");
                    throw;
                }
            }
            else
            {
                logger.Information($"Connection state: {socket.IsOpen}");
            }
        }
    }
}
