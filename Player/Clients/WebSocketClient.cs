using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Player.Clients
{
    public class WebSocketClient<R, S> : ISocketClient<R, S>
    {
<<<<<<< HEAD
        private const int _BUFFER_SIZE = 1024 * 4;
        private readonly ClientWebSocket _client;

        public bool IsOpen => _client.State == WebSocketState.Open;

        public WebSocketClient()
        {
            _client = new ClientWebSocket();
        }

        public async Task ConnectAsync(Uri uri)
        {
            await _client.ConnectAsync(uri, CancellationToken.None);
        }

        public async Task CloseAsync()
        {
            await _client.CloseAsync(
                closeStatus: WebSocketCloseStatus.NormalClosure,
                statusDescription: "Closed by the WebSocketClient",
                cancellationToken: CancellationToken.None
            );
        }

        public async Task<(bool, R)> ReceiveAsync()
        {
            byte[] buffer = new byte[_BUFFER_SIZE];
            WebSocketReceiveResult result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer),
                CancellationToken.None);
            if (result.CloseStatus.HasValue)
=======
        private const int BUFFER_SIZE = 1024 * 4;
        private readonly ClientWebSocket client;

        public bool IsOpen => client.State == WebSocketState.Open;

        public WebSocketClient()
        {
            client = new ClientWebSocket();
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await client.ConnectAsync(uri, cancellationToken);
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            await client.CloseAsync(
                closeStatus: WebSocketCloseStatus.NormalClosure,
                statusDescription: "Closed by the WebSocketClient",
                cancellationToken: cancellationToken
            );
        }

        public async Task<(bool, R)> ReceiveAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer),
                cancellationToken);
            if (cancellationToken.IsCancellationRequested || result.CloseStatus.HasValue)
>>>>>>> 2bf0805ab06bf8ebcfc306f753508ddec701f5a1
                return (false, default(R));
            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            R message = JsonConvert.DeserializeObject<R>(json);
            return (true, message);
        }

<<<<<<< HEAD
        public async Task SendAsync(S message)
        {
            string serialized = JsonConvert.SerializeObject(message);
            byte[] buffer = Encoding.UTF8.GetBytes(serialized);
            await _client.SendAsync(
                buffer: new ArraySegment<byte>(buffer, 0, buffer.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None
            );
=======
        public async Task SendAsync(S message, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                string serialized = JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(serialized);
                await client.SendAsync(
                    buffer: new ArraySegment<byte>(buffer, 0, buffer.Length),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: cancellationToken
                );
            }
>>>>>>> 2bf0805ab06bf8ebcfc306f753508ddec701f5a1
        }
    }
}
