using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Managers 
{
    public class WebSocketManager<M> : SocketManager<WebSocket, M>
    {
        public override bool IsSame(WebSocket a, WebSocket b) => a == b;
        public override async Task CloseSocketAsync(WebSocket socket) 
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
        public override async Task SendMessageAsync(WebSocket socket, M message)
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
