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

        public TcpSocketClient()
        {
            this.logger = Log.ForContext<TcpSocketClient<R, S>>();
            this.client = new TcpClient();
        }

        public TcpSocketClient(TcpClient tcpClient)
        {
            logger = Log.ForContext<TcpSocketClient<R, S>>();
            client = tcpClient;
            stream = tcpClient.GetStream();
            isOpen = tcpClient.Connected;
        }

        public bool IsOpen => isOpen && client.Connected;

        public int ReceiveTimeout => 50;

        public object GetSocket()
        {
            return client;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
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
            int countRead = await TryGetMessageLengthAsync(lengthEndian, cancellationToken);
            if (countRead == 0)
            {
                return (false, default);
            }

            int length = TryConvertToInt(lengthEndian);
            if (length == 0)
            {
                await ClearStream(cancellationToken);
                return (false, default);
            }

            byte[] buffer = new byte[length];
            countRead = await stream.ReadAsync(buffer, 0, length, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return (false, default);
            }
            if (countRead != length)
            {
                logger.Warning("Unexpected message - wrong length provided.\n Message will be aborted");
                await ClearStream(cancellationToken);
                return (false, default);
            }

            string json = Encoding.UTF8.GetString(buffer, 0, length);
            R message = JsonConvert.DeserializeObject<R>(json);
            return (true, message);
        }

        private async Task<int> TryGetMessageLengthAsync(byte[] lengthEndian, CancellationToken cancellationToken)
        {
            int count = await stream.ReadAsync(lengthEndian, 0, 2, cancellationToken);
            if (count == 0)
            {
                await Task.Delay(ReceiveTimeout);
                count = await stream.ReadAsync(lengthEndian, 0, 2, cancellationToken);
            }

            return count;
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

        private async Task ClearStream(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            int read;
            do
            {
                read = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            while (read > 0 && !cancellationToken.IsCancellationRequested);
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
