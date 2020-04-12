using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Player.Models;
using Serilog;
using Shared;

namespace Player.Clients
{
    public class TcpSocketClient<R, S> : ISocketClient<R, S>
    {
        private const int BufferSize = 1024 * 4;
        private readonly ILogger logger;
        private readonly TcpClient client;
        private NetworkStream stream;

        public TcpSocketClient()
        {
            this.logger = Log.ForContext<TcpSocketClient<R, S>>();
            this.client = new TcpClient();
        }

        public bool IsOpen => client.Connected;

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            stream.Close();
            client.Close();
            return Task.CompletedTask;
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await client.ConnectAsync(uri.Host, uri.Port);
            logger.Information($"Connected to {uri.Host}:{uri.Port}");
            this.stream = client.GetStream();
        }

        public async Task<(bool, R)> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && stream.DataAvailable)
            {
                byte[] lengthEndian = new byte[2];
                await stream.ReadAsync(lengthEndian, 0, 2, cancellationToken);
                int length;
                try
                {
                    length = lengthEndian.ToInt16();
                }
                catch (Exception e)
                {
                    logger.Warning($"Cannot convert to little-endian. Message will be aborted. \n{e}");
                    await stream.FlushAsync();
                    return (false, default);
                }

                byte[] buffer = new byte[length];
                int count = await stream.ReadAsync(buffer, 0, length, cancellationToken);
                if (count != length)
                {
                    logger.Warning("Unexpected message - wrong length provided.\n Message will be aborted");
                    await stream.FlushAsync();
                    return (false, default);
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    return (false, default(R));
                }

                string json = Encoding.UTF8.GetString(buffer, 0, length);
                R message = JsonConvert.DeserializeObject<R>(json);
                return (true, message);
            }

            return (false, default(R));
        }

        public async Task SendAsync(S message, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && IsOpen)
            {
                string serialized = JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(serialized);
                byte[] length = buffer.Length.ToLittleEndian();
                await stream.WriteAsync(length, 0, 2, cancellationToken);
                await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            }
            else if (!IsOpen)
            {
                logger.Warning("Tried to send message with closed socket");
            }
        }
    }
}
