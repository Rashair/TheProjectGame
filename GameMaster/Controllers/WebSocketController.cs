using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace GameMaster.Controllers
{
    public abstract class WebSocketController<T> : ControllerBase
    {
        private const int BufferSize = 1024 * 4;
        private readonly ILogger logger;

        public WebSocketManager<T> Manager { get; }

        public WebSocketController(WebSocketManager<T> manager)
        {
            logger = Log.ForContext<WebSocketController<T>>();
            Manager = manager;
        }

        protected abstract Task OnMessageAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);

        protected virtual void OnConnected(WebSocket socket)
        {
            bool result = Manager.AddSocket(socket);
            logger.Information($"Socked added: {result}");
        }

        protected virtual async Task OnDisconnectedAsync(WebSocket socket)
        {
            await Manager.RemoveSocketAsync(Manager.GetId(socket), CancellationToken.None);
        }

        protected virtual bool AcceptConnection()
        {
            return true;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            logger.Information("Get request!");
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
