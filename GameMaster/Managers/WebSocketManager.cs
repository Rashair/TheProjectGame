using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Managers
{
    public class WebSocketManager<T> : SocketManager<WebSocket, T>
    {
        protected override bool IsSame(WebSocket a, WebSocket b)
        {
            return a == b;
        }

        protected override async Task CloseSocketAsync(WebSocket socket)
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(
                    closeStatus: WebSocketCloseStatus.NormalClosure,
                    statusDescription: "Closed by the WebSocketManager",
                    cancellationToken: CancellationToken.None
                );
            }
        }

        protected override async Task SendMessageAsync(WebSocket socket, T message)
        {
            if (socket.State == WebSocketState.Open)
            {
                string serialized = JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(serialized);
                await socket.SendAsync(
                    buffer: new ArraySegment<byte>(buffer, 0, buffer.Length),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: CancellationToken.None
                );
            }
        }
    }
}