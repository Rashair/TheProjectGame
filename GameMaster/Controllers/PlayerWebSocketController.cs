using GameMaster.Managers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared.Models.Messages;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GameMaster.Controllers
{
    [Route("ws/player")]
    public class PlayerWebSocketController : WebSocketController<GMMessage>
    {
        private readonly BufferBlock<PlayerMessage> queue;

        public PlayerWebSocketController(BufferBlock<PlayerMessage> queue, WebSocketManager<GMMessage> manager)
            : base(manager)
        {
            this.queue = queue;
        }

        public override async Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            PlayerMessage message = JsonConvert.DeserializeObject<PlayerMessage>(json);
            await queue.SendAsync(message);
        }
    }
}
