using System.Net.WebSockets;
using System.Threading.Tasks;

using GameMaster.Managers;
using GameMaster.Models.Messages;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Controllers
{
    [Route("/client")]
    public class ClientWebSocketController : WebSocketController<ClientMessage>
    {
        public ClientWebSocketController(WebSocketManager<ClientMessage> manager)
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
