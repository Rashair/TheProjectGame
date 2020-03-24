using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameMaster.Controllers
{
    public abstract class WebSocketController<T> : Controller
    {
        private const int BufferSize = 1024 * 4;

        public WebSocketManager<T> Manager { get; }

        public WebSocketController(WebSocketManager<T> manager)
        {
            Manager = manager;
        }

        public abstract Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);

        public virtual void OnConnected(WebSocket socket)
        {
            Manager.AddSocket(socket);
        }

        public virtual async Task OnDisconnectedAsync(WebSocket socket)
        {
            await Manager.RemoveSocketAsync(Manager.GetId(socket));
        }

        public virtual bool AcceptConnection()
        {
            return true;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            HttpContext context = ControllerContext.HttpContext;
            if (!context.WebSockets.IsWebSocketRequest || !AcceptConnection())
                return BadRequest();
            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            OnConnected(socket);
            byte[] buffer = new byte[BufferSize];
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
