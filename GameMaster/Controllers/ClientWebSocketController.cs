using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Threading.Tasks;

using GameMaster.Managers;
using GameMaster.Models.Messages;

namespace GameMaster.Controllers
{
    [Route("ws/client")]
    public class ClientWebSocketController : WebSocketController
    {
        private readonly WebSocketManager<ClientMessage> _manager;
        public ClientWebSocketController(WebSocketManager<ClientMessage> manager)
        {
            _manager = manager;
        }
        public override void OnConnected(WebSocket socket) => _manager.AddSocket(socket);
        public override async Task OnDisconnectedAsync(WebSocket socket) 
            => await _manager.RemoveSocketAsync(_manager.GetId(socket));
        public override async Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            // Ignoring client messages
        }
    } 
}
