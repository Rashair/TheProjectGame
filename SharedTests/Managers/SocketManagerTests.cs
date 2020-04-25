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
            bool result = socketManager.AddSocket(client.Object);

            // Assert
            Assert.True(result, "Socket should be added");
        }

        [Fact]
        public void GetId_Test()
        {
            // Arrange
            int startId = socketManager.GetValue<SocketManager<ISocketClient<PlayerMessage, GMMessage>, GMMessage>, 
                int>("guid");
            var client = new TcpSocketClient<PlayerMessage, GMMessage>(logger);
            socketManager.AddSocket(client);

            // Act
            int id = socketManager.GetId(client);

            // Assert
            Assert.Equal(startId + 1, id);
        }
    }
}
