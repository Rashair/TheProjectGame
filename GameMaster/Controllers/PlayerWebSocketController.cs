using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using Shared.Models.Messages;

namespace GameMaster.Controllers
{
    [Route("ws/player")]
    public class PlayerWebSocketController : WebSocketController<GMMessage>
    {
        private readonly BufferBlock<AgentMessage> _queue;
        public PlayerWebSocketController(BufferBlock<AgentMessage> queue, WebSocketManager<GMMessage> manager) 
            : base(manager)
        {
            _queue = queue;
        }
        public override async Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            AgentMessage message = JsonConvert.DeserializeObject<AgentMessage>(json);
            await _queue.SendAsync(message);
        }
    } 
}
