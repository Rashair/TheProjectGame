using GameMaster.Managers;
using GameMaster.Models.Messages;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace GameMaster.Controllers
{
    [Route("ws/client")]
    public class ClientWebSocketController : WebSocketController<BackendMessage>
    {
        public ClientWebSocketController(WebSocketManager<BackendMessage> manager) : base(manager)
        {
        }

        public override Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            // Ignoring client messages
            return Task.CompletedTask;
        }
    }
}
