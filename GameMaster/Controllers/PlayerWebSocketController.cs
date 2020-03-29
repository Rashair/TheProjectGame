using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Managers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using Shared.Messages;

namespace GameMaster.Controllers
{
    [Route("/player")]
    public class PlayerWebSocketController : WebSocketController<GMMessage>
    {
        private readonly ILogger logger;
        private readonly BufferBlock<PlayerMessage> queue;

        public PlayerWebSocketController(BufferBlock<PlayerMessage> queue, WebSocketManager<GMMessage> manager)
            : base(manager)
        {
            this.queue = queue;
            logger = Log.ForContext<PlayerWebSocketController>();
        }

        protected override async Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            PlayerMessage message = JsonConvert.DeserializeObject<PlayerMessage>(json);

            // TODO: To be changed later.
            logger.Information(json);
            message.PlayerID = Manager.GetId(socket);
            await queue.SendAsync(message);
        }
    }
}
