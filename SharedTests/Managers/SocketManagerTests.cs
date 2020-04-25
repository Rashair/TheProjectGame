using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using Serilog;
using Shared.Clients;
using Shared.Managers;
using Shared.Messages;
using TestsShared;
using Xunit;

namespace Shared.Tests.Managers
{
    public class SocketManagerTests
    {
        private readonly ILogger logger;
        private readonly SocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage> socketManager;

        public SocketManagerTests()
        {
            logger = MockGenerator.Get<ILogger>();
            socketManager = new TcpSocketManager<PlayerMessage, GMMessage>(logger);
        }

        [Fact]
        public void AddSocket_Test()
        {
            // Arrange
            var client = new Mock<ISocketClient<PlayerMessage, GMMessage>>() { DefaultValue = DefaultValue.Mock };

            // Act
            int result = socketManager.AddSocket(client.Object);

            // Assert
            Assert.True(result > 0, "Socket should be added");
        }

        [Fact]
        public void GetId_Test()
        {
            // Arrange
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            int createdId = socketManager.AddSocket(client);

            // Act
            int id = socketManager.GetId(client);

            // Assert
            Assert.Equal(createdId, id);
        }

        [Fact]
        public void GetSocketById_Test()
        {
            // Arrange
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            int id = socketManager.AddSocket(client);

            // Act
            var socket = socketManager.GetSocketById(id);

            // Assert
            Assert.True(client == socket, "Returned socket should be the same object which was added");
        }

        [Fact]
        public async void RemoveSocketAsync_Test()
        {
            // Arrange
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            var token = CancellationToken.None;
            int id = socketManager.AddSocket(client);

            // Act
            bool removed = await socketManager.RemoveSocketAsync(id, token);

            // Assert
            Assert.True(removed, "Socket should be removed");
            var socket = socketManager.GetSocketById(id);
            Assert.Null(socket);
        }

        [Fact]
        public async void SendMessageAsync_WithOpenClient_ShouldSendMessage_Test()
        {
            // Arrange
            var clientMock = new Mock<ISocketClient<PlayerMessage, GMMessage>>();
            clientMock.Setup(c => c.IsOpen).Returns(true);
            var token = CancellationToken.None;
            var message = new GMMessage();
            int id = socketManager.AddSocket(clientMock.Object);

            // Act
            await socketManager.SendMessageAsync(id, message, token);

            // Assert
            clientMock.Verify(c => c.SendAsync(message, token), Times.Once(), "Client sendAsync should be invoked");
        }

        [Fact]
        public async void SendMessageAsync_WithClosedClient_ShouldNotSendMessage_Test()
        {
            // Arrange
            var clientMock = new Mock<ISocketClient<PlayerMessage, GMMessage>>();
            clientMock.Setup(c => c.IsOpen).Returns(false);
            var token = CancellationToken.None;
            var message = new GMMessage();
            int id = socketManager.AddSocket(clientMock.Object);

            // Act
            await socketManager.SendMessageAsync(id, message, token);

            // Assert
            clientMock.Verify(c => c.SendAsync(message, token), Times.Never(), "Client sendAsync should not be invoked");
        }
    }
}
