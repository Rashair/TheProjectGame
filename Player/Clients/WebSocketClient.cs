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
                return (false, default(R));
            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            R message = JsonConvert.DeserializeObject<R>(json);
            return (true, message);
        }

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
        }
    }
}
