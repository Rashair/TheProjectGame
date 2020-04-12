using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Serilog;
using Shared;

namespace GameMaster.Managers
{
    public class TcpSocketManager<TMessage> : SocketManager<TcpClient, TMessage>
    {
        private readonly ILogger logger;

        public TcpSocketManager()
        {
            this.logger = Log.ForContext<TcpSocketManager<TMessage>>();
        }

        protected override bool IsOpen(TcpClient socket)
        {
            return socket.Connected;
        }

        protected override bool IsSame(TcpClient a, TcpClient b)
        {
            return a == b;
        }

        protected override Task CloseSocketAsync(TcpClient socket, CancellationToken cancellationToken)
        {
            socket.Close();
            logger.Information("Closing socket");
            return Task.CompletedTask;
        }

        protected override async Task SendMessageAsync(TcpClient socket, TMessage message, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && socket.Connected)
            {
                try
                {
                    var stream = socket.GetStream();

                    string serialized = JsonConvert.SerializeObject(message);
                    byte[] buffer = Encoding.UTF8.GetBytes(serialized);
                    var length = buffer.Length.ToLittleEndian();
                    await stream.WriteAsync(length, cancellationToken);
                    await stream.WriteAsync(buffer, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.Error($"Failed to send message: {e}");
                }
            }
            else
            {
                logger.Information($"Connection state: {socket.Connected}");
            }
        }
    }
}
