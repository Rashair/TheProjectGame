using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Serilog;

namespace GameMaster.Managers
{
    public class TcpSocketManager<TMessage> : SocketManager<TcpClient, TMessage>
    {
        private readonly ILogger logger;

        public TcpSocketManager()
        {
            this.logger = Log.ForContext<WebSocketManager<TMessage>>();
        }

        protected override bool IsSame(TcpClient a, TcpClient b)
        {
            return a == b;
        }

        protected override Task CloseSocketAsync(TcpClient socket, CancellationToken cancellationToken)
        {
            // Can close after some time
            socket.Close();
            return Task.CompletedTask;
        }

        protected override async Task SendMessageAsync(TcpClient socket, TMessage message, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && socket.Connected)
            {
                var stream = socket.GetStream();

                string serialized = JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(serialized);

                await stream.WriteAsync(buffer, cancellationToken);

                stream.Close();
            }
        }
    }
}
