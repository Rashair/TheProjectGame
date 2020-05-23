using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Serilog;
using Shared.Managers;

namespace GameMaster.Managers
{
    public class WebSocketManager<TMessage> : SocketManager<WebSocket, TMessage>
    {
        private readonly ILogger logger;

        public WebSocketManager()
        {
            this.logger = Log.ForContext<WebSocketManager<TMessage>>();
        }

        protected override bool IsOpen(WebSocket socket)
        {
            return socket.State == WebSocketState.Open;
        }

        protected override bool IsSame(WebSocket a, WebSocket b)
        {
            return a == b;
        }

        protected override async Task CloseSocketAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && (socket.State == WebSocketState.Open))
            {
                await socket.CloseOutputAsync(
                    closeStatus: WebSocketCloseStatus.NormalClosure,
                    statusDescription: "Closed by the WebSocketManager",
                    cancellationToken: cancellationToken);
            }
        }

        protected override async Task SendMessageAsync(WebSocket socket, TMessage message,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && (socket.State == WebSocketState.Open))
            {
                string serialized = JsonConvert.SerializeObject(message);
                byte[] buffer = Encoding.UTF8.GetBytes(serialized);
                await socket.SendAsync(
                    buffer: new ArraySegment<byte>(buffer, 0, buffer.Length),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: cancellationToken);
            }
        }
    }
}
