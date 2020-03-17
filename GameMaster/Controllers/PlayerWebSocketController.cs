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
    public class PlayerWebSocketController : WebSocketController
    {
        private readonly BufferBlock<AgentMessage> _queue;
        private readonly WebSocketManager<GMMessage> _manager;
        public PlayerWebSocketController(BufferBlock<AgentMessage> queue, WebSocketManager<GMMessage> manager) : base()
        {
            _queue = queue;
            _manager = manager;
        }
        public override void OnConnected(WebSocket socket) => _manager.AddSocket(socket);
        public override async Task OnDisconnectedAsync(WebSocket socket) 
            => await _manager.RemoveSocketAsync(_manager.GetId(socket));
        public override async Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            AgentMessage message = JsonConvert.DeserializeObject<AgentMessage>(json);
            await _queue.SendAsync(message);
        }
    } 
}
