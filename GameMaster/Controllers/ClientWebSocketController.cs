using System.Net.WebSockets;
using System.Threading.Tasks;

using GameMaster.Managers;
using GameMaster.Models.Messages;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Controllers
{
    [Route("/client")]
    public class ClientWebSocketController : WebSocketController<BackendMessage>
    {
        public ClientWebSocketController(WebSocketManager<BackendMessage> manager)
            : base(manager)
        {
        }

        protected override Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            // Ignoring client messages
            return Task.CompletedTask;
        }
    }
}
