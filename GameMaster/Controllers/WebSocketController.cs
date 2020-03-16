using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace GameMaster.Controllers 
{
    public abstract class WebSocketController : Controller
    {
        private const int _BUFFER_SIZE = 1024 * 4;
        public abstract Task OnConnectedAsync(WebSocket socket);
        public abstract Task OnDisconnectedAsync(WebSocket socket);
        public abstract Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            HttpContext context = ControllerContext.HttpContext;
            if (!context.WebSockets.IsWebSocketRequest)
                return BadRequest();
            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await OnConnectedAsync(socket);
            byte[] buffer = new byte[_BUFFER_SIZE];
            WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), 
                CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await OnMessageAsync(socket, result, buffer);
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await OnDisconnectedAsync(socket);
            return Ok();
        }
    }
}
