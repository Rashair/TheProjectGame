using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Serilog;

namespace Shared.Clients
{
    public class TcpSocketClient<R, S> : ISocketClient<R, S>
    {
        private readonly ILogger logger;
        private readonly TcpClient client;
        private NetworkStream stream;
        private bool isOpen;

        public TcpSocketClient(ILogger log)
        {
            this.logger = log.ForContext<TcpSocketClient<R, S>>();
            this.client = new TcpClient();
        }

        public TcpSocketClient(TcpClient tcpClient, ILogger log)
        {
            logger = log.ForContext<TcpSocketClient<R, S>>();
            client = tcpClient;
            stream = tcpClient.GetStream();
            isOpen = tcpClient.Connected;
        }

        public bool IsOpen => isOpen && client.Connected;

        public object GetSocket()
        {
            return client;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            isOpen = false;
            stream.Close();
            client.Close();
            return Task.CompletedTask;
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            await client.ConnectAsync(host, port);
            stream = client.GetStream();
            isOpen = true;
            logger.Information($"Connected to {host}:{port}");
        }

        public async Task<(bool, R)> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || !client.Connected)
            {
                return (false, default);
            }

            byte[] lengthEndian = new byte[2];
            int countRead = await stream.ReadAsync(lengthEndian, 0, 2, cancellationToken);
            if (countRead == 0)
            {
                await CloseAsync(cancellationToken);
                return (false, default);
            }
            else if (countRead == 1)
            {
                logger.Warning("Message length should be wrote on 2 bytes.\nAborting message...");
                return (false, default);
            }

            int length = TryConvertToInt(lengthEndian);
            if (length == 0)
            {
                logger.Warning("Bad message length or not in little-endian convention.\nAborting message...");
                return (false, default);
            }

            byte[] buffer = new byte[length];
            countRead = await stream.ReadAsync(buffer, 0, length, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return (false, default);
            }
            else if (countRead == 0)
            {
                await CloseAsync(cancellationToken);
                return (false, default);
            }
            else if (countRead != length)
            {
                logger.Warning("Unexpected message - wrong length provided.\nMessage will be aborted");
                return (false, default);
            }

            string json = Encoding.UTF8.GetString(buffer, 0, length);
            R message = JsonConvert.DeserializeObject<R>(json);
            return (true, message);
        }

        private int TryConvertToInt(byte[] lengthEndian)
        {
            int length;
            try
            {
                length = lengthEndian.ToInt16();
            }
            catch (Exception e)
            {
                logger.Warning($"Cannot convert to little-endian. Message will be aborted. \n{e}");
                return 0;
            }

            return length;
        }

        public async Task SendAsync(S message, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                string serialized = JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(serialized);
                byte[] length = buffer.Length.ToLittleEndian();
                await stream.WriteAsync(length, 0, 2, cancellationToken);
                await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            }
        }
    }
}
