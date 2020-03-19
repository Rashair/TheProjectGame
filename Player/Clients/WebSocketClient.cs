using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Player.Clients
{
    public class WebSocketClient<R, S> : ISocketClient<R, S>
    {
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

        /// <returns>
        /// (bool notClosed, R value)
        /// If notClosed is false then the socket is closed and value is the default value of a R type, otherwise value
        /// is deserialized from socket message.
        /// </returns>
        public async Task<(bool, R)> ReceiveAsync()
        {
            byte[] buffer = new byte[_BUFFER_SIZE];
            WebSocketReceiveResult result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer),
                CancellationToken.None);
            if (result.CloseStatus.HasValue)
                return (false, default(R));
            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            R message = JsonConvert.DeserializeObject<R>(json);
            return (true, message);
        }

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
        }
    }
}
