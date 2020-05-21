using System.Collections.Generic;
using System.Threading;

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
        private readonly SocketManager<ISocketClient<Message, Message>, Message> socketManager;

        public SocketManagerTests()
        {
            logger = MockGenerator.Get<ILogger>();
            socketManager = new TcpSocketManager<Message, Message>(logger);
        }

        [Fact]
        public void AddSocket_Test()
        {
            // Arrange
            var client = new Mock<ISocketClient<Message, Message>>() { DefaultValue = DefaultValue.Mock };

            // Act
            int result = socketManager.AddSocket(client.Object);

            // Assert
            Assert.True(result > 0, "Socket should be added");
        }

        [Fact]
        public void GetId_Test()
        {
            // Arrange
            var client = new TcpSocketClient<Message, Message>(logger);
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
            var client = new TcpSocketClient<Message, Message>(logger);
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
            var client = new TcpSocketClient<Message, Message>(logger);
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
            var clientMock = new Mock<ISocketClient<Message, Message>>();
            clientMock.Setup(c => c.IsOpen).Returns(true);
            var token = CancellationToken.None;
            var message = new Message();
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
            var clientMock = new Mock<ISocketClient<Message, Message>>();
            clientMock.Setup(c => c.IsOpen).Returns(false);
            var token = CancellationToken.None;
            var message = new Message();
            int id = socketManager.AddSocket(clientMock.Object);

            // Act
            await socketManager.SendMessageAsync(id, message, token);

            // Assert
            clientMock.Verify(c => c.SendAsync(message, token), Times.Never(), "Client sendAsync should not be invoked");
        }

        [Fact]
        public async void SendMessageToAllAsync_Test()
        {
            // Arrange
            int clientsNum = 10;
            var clientMocks = new List<Mock<ISocketClient<Message, Message>>>();
            for (int i = 0; i < clientsNum; ++i)
            {
                var mock = new Mock<ISocketClient<Message, Message>>();
                mock.Setup(c => c.IsOpen).Returns(true);
                clientMocks.Add(mock);
                int id = socketManager.AddSocket(mock.Object);
                Assert.True(id > 0, "Socket should be added");
            }
            var token = CancellationToken.None;
            var message = new Message();

            // Act
            await socketManager.SendMessageToAllAsync(message, token);

            // Assert
            for (int i = 0; i < clientsNum; ++i)
            {
                clientMocks[i].Verify(c => c.SendAsync(message, token), Times.Once(), "Client sendAsync should be invoked");
            }
        }
    }
}
