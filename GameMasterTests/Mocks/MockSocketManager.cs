using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using GameMaster.Managers;
using Shared.Messages;

namespace GameMaster.Tests.Mocks
{
    public class MockSocketManager : ISocketManager<WebSocket, GMMessage>
    {
        private readonly Send send;

        public delegate void Send(GMMessage message);

        public MockSocketManager(Send send)
        {
            this.send = send;
        }

        public bool AddSocket(WebSocket socket) => default;

        public int GetId(WebSocket socket) => default;

        public WebSocket GetSocketById(int id) => default;

        public Task<bool> RemoveSocketAsync(int id, CancellationToken cancellationToken)
            => default;

        public async Task SendMessageAsync(int id, GMMessage message, CancellationToken cancellationToken)
        {
            send(message);
            await Task.CompletedTask;
        }

        public Task SendMessageToAllAsync(GMMessage message, CancellationToken cancellationToken)
            => default;
    }
}
